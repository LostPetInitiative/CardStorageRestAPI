﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CardStorageRestAPI;
using Cassandra;
using Cassandra.Mapping;
using ISession = Cassandra.ISession;

namespace PatCardStorageAPI.Storage
{
    public class CassandraStorage : ICardStorage, IPhotoStorage
    {
        private ICluster cluster;
        private ISession session;

        private readonly string keyspace;
        private readonly string[] contactPoints;

        /// <summary>
        /// To be executed on every connection. The scripts must be idempotent (e.g. have IF NOT EXIST clauses). Reside in Scripts directory. Order is important.
        /// </summary>
        private static readonly string[] deploymentScripts = new string[] {
            "create_kashtanka_keyspace.cql",
            "create_location_type.cql",
            "create_contant_info_type.cql",
            "create_cards_by_id.cql",
            "create_images_by_card_id.cql",
            "create_processed_images_by_uuid.cql",
            "create_image_featrues_by_image_uuid.cql",
        };
        
        public static IEnumerable<IPEndPoint> EndpointFromString(string str)
        {
            var parts = str.Split(":", StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
            int port = 9042; // default port
            string host = parts[0];
            if (parts.Length == 2)
            {
                port = int.Parse(parts[1]);
            }
            Trace.TraceInformation($"Tying to resolve {host}");
            foreach (IPAddress addr in Dns.GetHostEntry(host).AddressList)
            {
                Trace.TraceInformation($"Possible contact point is {addr}:{port}");
                Trace.Flush();
                yield return new IPEndPoint(addr, port);
            }

        }

        private PreparedStatement? insertPetCardStatement;
        private PreparedStatement? deletePetCardStatement;
        private PreparedStatement? getPetCardStatement;

        private PreparedStatement? deleteSpecificPetOriginalImageStatement;
        private PreparedStatement? deletePetProcessedImageStatement;
        private PreparedStatement? deleteAllPetImagesStatement;
        private PreparedStatement? getAllPetImagesStatement;
        private PreparedStatement? getParticularOriginalPetImageUuidStatement;
        private PreparedStatement? getParticularOriginalPetImageStatement;
        private PreparedStatement? getParticularProcessedPetImageStatement;
        private PreparedStatement? getPhotoFeatureVectorStatement;
        private PreparedStatement? addPetOriginalImageStatement;
        private PreparedStatement? addPetProcessedImageStatement;
        private PreparedStatement? addPhotoFeaturesStatement;
        
        private bool connected = false;
        private SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

        public CassandraStorage(string keyspace, params string[] contactPoints)
        {
            if (string.IsNullOrEmpty(keyspace))
                throw new ArgumentException("keyspace must be non-empty string");
            if (contactPoints == null || contactPoints.Length == 0)
                throw new ArgumentException("contactPoints array must contain at lease one element");
            this.keyspace = keyspace!;
            this.contactPoints = contactPoints;
        }

        private async Task EnsureConnectionInitialized()
        {
            await initSemaphore.WaitAsync();
            try
            {
                if (connected)
                    return;

                do
                {
                    try
                    {
                        var builder = Cluster.Builder();                        
                        try {
                            IPEndPoint[] ipEndpoints = this.contactPoints.SelectMany(s => EndpointFromString(s)).ToArray();
                            builder = builder.AddContactPoints(ipEndpoints);                            
                        }
                        catch (Exception ex) {
                            Trace.TraceError($"Failed to construct ip endpoints from strings {this.contactPoints}: {ex}.\nFalling back to plain string endpoint usage");
                            builder = builder.AddContactPoints(this.contactPoints);
                        }
                        
                        this.cluster = builder
                                 .WithApplicationName("RestAPI")
                                 .WithMaxProtocolVersion(ProtocolVersion.V4)
                                 .WithCompression(CompressionType.LZ4)
                                 .Build();
                    }
                    catch (Cassandra.NoHostAvailableException ex)
                    {
                        string errors = string.Join(";", ex.Errors.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                        Trace.TraceError($"Failed to connect to the cluster: {errors}");
                        throw;
                    }

                    this.session = await cluster.ConnectAsync();

                    Trace.TraceInformation($"Binary protocol version: {this.session.BinaryProtocolVersion}");
                    if (this.session.BinaryProtocolVersion != (int)Cassandra.ProtocolVersion.V4)
                    {
                        Trace.TraceError($"Can't establish V4 connection to the cluster. Shutting down the connection...");
                        this.cluster.Shutdown(5000);
                    }
                    else
                        connected = true;
                }
                while (!connected);

                Trace.TraceInformation("Deploying schema...");
                foreach (var deploymentScript in deploymentScripts) {
                    var script = await File.ReadAllTextAsync(Path.Combine("Scripts", deploymentScript));
                    var statement = await this.session.PrepareAsync(script);
                    statement.SetConsistencyLevel(ConsistencyLevel.All);
                    await session.ExecuteAsync(statement.Bind());
                    Trace.TraceInformation($"{deploymentScript} deployed successfully");
                }

                session.ChangeKeyspace(this.keyspace);
                Trace.TraceInformation($"Working with keyspace {this.keyspace} from now");

                this.getPetCardStatement = await session.PrepareAsync("SELECT * FROM cards_by_id WHERE namespace = ? AND local_id = ?");
                this.insertPetCardStatement = await session.PrepareAsync("INSERT INTO cards_by_id (namespace, local_id, provenance_url, animal, animal_sex, card_type, event_time,card_creation_time, event_location, contact_info) values (?,?,?,?,?,?,?,?,?,?) IF NOT EXISTS");
                this.deletePetCardStatement = await session.PrepareAsync("DELETE FROM cards_by_id WHERE namespace = ? AND local_id = ?");

                this.deleteSpecificPetOriginalImageStatement = await session.PrepareAsync("DELETE FROM images_by_card_id WHERE namespace = ? AND local_id = ? AND image_num = ?");
                this.deletePetProcessedImageStatement = await session.PrepareAsync("DELETE FROM processed_images_by_image_uuid WHERE image_uuid = ? AND processing_ident = ?");
                this.deleteAllPetImagesStatement = await session.PrepareAsync("DELETE FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                // this.getAllPetImagesStatementIncBin = await session.PrepareAsync("SELECT * FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                this.getAllPetImagesStatement = await session.PrepareAsync("SELECT namespace,local_id,image_num,image_uuid FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                this.getParticularOriginalPetImageUuidStatement = await session.PrepareAsync("SELECT image_uuid FROM images_by_card_id WHERE namespace = ? AND local_id = ? AND image_num = ?");
                this.getParticularOriginalPetImageStatement = await session.PrepareAsync("SELECT * FROM images_by_card_id WHERE namespace = ? AND local_id = ? AND image_num = ?");
                this.getParticularProcessedPetImageStatement = await session.PrepareAsync("SELECT * FROM processed_images_by_image_uuid WHERE image_uuid = ? AND processing_ident = ?");
                this.addPetOriginalImageStatement = await session.PrepareAsync("INSERT INTO images_by_card_id (namespace, local_id, image_num, image, image_mime_type, image_uuid) values (?,?,?,?,?,?) IF NOT EXISTS");
                this.addPetProcessedImageStatement = await session.PrepareAsync("INSERT INTO processed_images_by_image_uuid (image_uuid, processing_ident, image, image_mime_type) values (?,?,?,?) IF NOT EXISTS");
                this.addPhotoFeaturesStatement = await session.PrepareAsync("INSERT INTO image_features_by_image_uuid (image_uuid, feature_ident, feature_vector) values (?,?,?)");
                this.getPhotoFeatureVectorStatement = await session.PrepareAsync("SELECT feature_vector FROM image_features_by_image_uuid WHERE image_uuid = ? AND feature_ident = ?");

                this.session.UserDefinedTypes.Define(UdtMap.For<Location>("location")
                    .Map(v => v.Address, "address")
                    .Map(v => v.CoordsProvenance, "coords_provenance")
                    .Map(v => v.Lat, "lat")
                    .Map(v => v.Lon, "lon"));
                this.session.UserDefinedTypes.Define(UdtMap.For<ContactInfo>("contact_info")
                    .Map(v => v.Name, "name")
                    .Map(v => v.Comment, "comment")
                    .Map(v => v.Email, "email")
                    .Map(v => v.Tel, "tel")
                    .Map(v => v.Website, "website"));

                this.connected = true;
            }
            finally
            {
                initSemaphore.Release();
            }
        }

        private static bool checkInsertSuccessful(RowSet? rowSet)
        {
            var row = rowSet?.FirstOrDefault();
            if (row == null)
                throw new ArgumentException("insert query (with IF NOT EXIST) result is supposed to return rows");
            return row.GetValue<bool>("[applied]");
        }
        
        public static sbyte EncodeAnimal(string animal)
        {
            return animal switch
            {
                "cat" => 1,
                "dog" => 2,
                _ => 0,
            };
        }

        public static string DecodeAnimal(sbyte code)
        {
            return code switch
            {
                1 => "cat",
                2 => "dog",
                _ => "unknown"
            };
        }

        public static sbyte EncodePetSex(string petSex)
        {
            return petSex switch
            {
                "female" => 1,
                "male" => 2,
                _ => 0,
            };
        }

        public static string DecodePetSex(sbyte code)
        {
            return code switch
            {
                1 => "female",
                2 => "male",
                _ => "unknown"
            };
        }

        public static sbyte EncodeCardType(string cardType)
        {
            return cardType switch
            {
                "found" => 1,
                "lost" => 2,
                _ => 0
            };
        }

        public static string DecodeCardType(sbyte code)
        {
            return code switch
            {
                1 => "found",
                2 => "lost",
                _ => "unknown"
            };
        }

        public async Task<bool> SetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID, PetCard card)
        {
            await EnsureConnectionInitialized();


            // See scripts/create_cards_by_id.cql
            var statement = this.insertPetCardStatement.Bind(
                ns.ToString(),
                localID.ToString(),
                card.ProvenanceURL,
                EncodeAnimal(card.Animal),
                EncodePetSex(card.AnimalSex),
                EncodeCardType(card.CardType),
                Tuple.Create(card.EventTime, card.EventTimeProvenance),
                card.CardCreationTime,
                card.Location,
                card.ContactInfo
                );

            return checkInsertSuccessful(await session.ExecuteAsync(statement));
        }

        public async Task<(Guid uuid, bool created)> AddOriginalPetPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum, PetPhoto photo)
        {
            await EnsureConnectionInitialized();

            // see Scripts/create_images_by_card_id.cql
            var uuid = Guid.NewGuid();
            var statement = this.addPetOriginalImageStatement.Bind(
                ns.ToString(),
                localID.ToString(),
                (sbyte)imageNum,
                photo.Image,
                photo.ImageMimeType,
                uuid
                );
            var res = await this.session.ExecuteAsync(statement);
            if(checkInsertSuccessful(res)) {
                return (uuid, true);
            }
            else {
                // fetching existing UUID
                var statement2 = this.getParticularOriginalPetImageUuidStatement.Bind(ns.ToString(), localID.ToString(), (sbyte)imageNum);
                var res2 = await this.session.ExecuteAsync(statement2);
                var row = res2.FirstOrDefault();
                if (row != null) {
                    return (row.GetValue<Guid>("image_uuid"), false);
                } else {
                    throw new InvalidOperationException($"Failed to create an entry as well as to find existing one for :{ns}/{localID}/{imageNum}");
                }
            }            
        }

        public async Task<bool> AddProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent, PetPhoto photo) {
            // see Scripts/create_processed_images_by_uuid.cql
            var statement = this.addPetProcessedImageStatement.Bind(
                imageUuid,
                processingIdent.ToString(),
                photo.Image,
                photo.ImageMimeType
                );
            return checkInsertSuccessful(await this.session.ExecuteAsync(statement));
        }

        public async Task<bool> DeletePetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            await EnsureConnectionInitialized();

            var statement = this.deletePetCardStatement.Bind(ns.ToString(), localID.ToString());

            await session.ExecuteAsync(statement);

            return true;
        }

        public async Task<PetCard> GetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            await EnsureConnectionInitialized();

            var statement = this.getPetCardStatement.Bind(ns.ToString(), localID.ToString());
            var rows = await session.ExecuteAsync(statement);
            Row extracted = rows.FirstOrDefault();
            if (extracted != null)
            {
                var et = extracted.GetValue<Tuple<DateTimeOffset, string>>("event_time");
                var result = new PetCard()
                {
                    CardType = DecodeCardType(extracted.GetValue<sbyte>("card_type")),
                    ContactInfo = extracted.GetValue<ContactInfo>("contact_info"),
                    EventTime = et.Item1,
                    EventTimeProvenance = et.Item2,
                    CardCreationTime = extracted.GetValue<DateTimeOffset>("card_creation_time"),
                    Location = extracted.GetValue<Location>("event_location"),
                    Animal = DecodeAnimal(extracted.GetValue<sbyte>("animal")),
                    AnimalSex = DecodePetSex(extracted.GetValue<sbyte>("animal_sex")),
                    ProvenanceURL = extracted.GetValue<string>("provenance_url"),
                    Features = extracted.GetValue<SortedDictionary<string, double[]>>("features")
                };

                return result;
            }
            else
                return null;
        }

        private static PetOriginalPhoto ConvertRowToPetOrigPhoto(Row row, bool includeBinData)
        {
            // See Scripts/create_images_by_card_id.cql for column names
            return new PetOriginalPhoto(
                row.GetValue<Guid>("image_uuid"),
                includeBinData ? row.GetValue<byte[]>("image") : null,
                includeBinData ? row.GetValue<string>("image_mime_type") : null,
                row.GetValue<sbyte>("image_num")
                );
        }

        private static PetPhoto ConvertRowToPetProcessedPhoto(Row row, bool includeBinData)
        {
            // See Scripts/create_images_by_card_id.cql for column names
            return new PetPhoto(
                includeBinData ? row.GetValue<byte[]>("image") : null,
                includeBinData ? row.GetValue<string>("image_mime_type") : null
                );
        }

        public async IAsyncEnumerable<PetOriginalPhoto> ListOriginalPhotosAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            await EnsureConnectionInitialized();

            BoundStatement statement = this.getAllPetImagesStatement.Bind(ns.ToString(), localID.ToString());            
            var rows = await this.session.ExecuteAsync(statement);
            foreach (var row in rows)
            {
                yield return ConvertRowToPetOrigPhoto(row, false);
            }
        }

        public async Task<bool> DeleteOriginalPetPhoto(AsciiIdentifier ns, AsciiIdentifier localID, int photoNum = -1)
        {
            await EnsureConnectionInitialized();

            BoundStatement statement = (photoNum == -1) ?
                (this.deleteAllPetImagesStatement.Bind(ns.ToString(), localID.ToString())) :
                (this.deleteSpecificPetOriginalImageStatement.Bind(ns.ToString(), localID.ToString(), photoNum));
            await this.session.ExecuteAsync(statement);
            return true;
        }

        public async Task<bool> DeleteProcessedPhoto(Guid imageUuid, AsciiIdentifier processingIdent) {
            await EnsureConnectionInitialized();

            BoundStatement statement = this.deletePetProcessedImageStatement.Bind(imageUuid, processingIdent.ToString());
            await this.session.ExecuteAsync(statement);
            return true;
        }

        public async Task<PetPhotoWithGuid> GetOriginalPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum)
        {
            await EnsureConnectionInitialized();

            var statement = this.getParticularOriginalPetImageStatement.Bind(ns.ToString(), localID.ToString(), (sbyte)imageNum);
            var row = (await this.session.ExecuteAsync(statement)).FirstOrDefault();
            if (row != null)
            {
                PetOriginalPhoto photo = ConvertRowToPetOrigPhoto(row, true);
                return photo;
            }
            else
            {
                return null;
            }
        }

        public async Task<PetPhoto> GetProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent) {
            await EnsureConnectionInitialized();
            var statement = this.getParticularProcessedPetImageStatement.Bind(imageUuid, processingIdent.ToString());
            var row = (await this.session.ExecuteAsync(statement)).FirstOrDefault();
            if (row != null)
            {
                PetPhoto photo = ConvertRowToPetProcessedPhoto(row, true);
                return photo;
            }
            else
            {
                return null;
            }

        }

        public async Task<bool> SetCardFeatureVectorAsync(AsciiIdentifier ns, AsciiIdentifier localID, AsciiIdentifier featuredIdent, double[] features)
        {
            await EnsureConnectionInitialized();

            var newDict = new Dictionary<string, IEnumerable<double>>();
            newDict.Add(featuredIdent.ToString(), features);
            var statement = new SimpleStatement($"UPDATE cards_by_id SET features = features + ? WHERE namespace = ? AND local_id = ?",
                newDict, ns.ToString(), localID.ToString());
            await this.session.ExecuteAsync(statement);
            return true;
        }

        public async Task<bool> SetPhotoFeatureVectorAsync(Guid imageUuid, AsciiIdentifier featuresIdent, double[] features)
        {
            await EnsureConnectionInitialized();

            var statement = this.addPhotoFeaturesStatement.Bind(imageUuid, featuresIdent.ToString(), features);
            await this.session.ExecuteAsync(statement);

            return true;
        }

        public async Task<double[]?> GetPhotoFeatures(Guid imageUuid, AsciiIdentifier featuresIdent)
        {
            await EnsureConnectionInitialized();

            var statement = this.getPhotoFeatureVectorStatement.Bind(imageUuid, featuresIdent.ToString());
            var res = await this.session.ExecuteAsync(statement);

            return res.FirstOrDefault()?.GetValue<double[]>("feature_vector");

        }
    }
}
