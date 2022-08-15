using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using ISession = Cassandra.ISession;

namespace CassandraAPI.Storage
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
            "create_images_by_card_id.cql"
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

        private PreparedStatement insertPetCardStatement;
        private PreparedStatement deletePetCardStatement;
        private PreparedStatement getPetCardStatement;

        private PreparedStatement deleteSpecificPetImageStatement;
        private PreparedStatement deleteAllPetImagesStatement;
        private PreparedStatement getAllPetImagesStatementIncBin;
        private PreparedStatement getAllPetImagesStatement;
        private PreparedStatement getParticularPetImageStatement;
        private PreparedStatement addPetImageStatement;

        private bool connected = false;
        private SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

        public CassandraStorage(string keyspace, params string[] contactPoints)
        {
            this.keyspace = keyspace;
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
                this.insertPetCardStatement = await session.PrepareAsync("INSERT INTO cards_by_id (namespace, local_id, provenance_url, animal, animal_sex, card_type, event_time,card_creation_time, event_location, contact_info) values (?,?,?,?,?,?,?,?,?,?)");
                this.deletePetCardStatement = await session.PrepareAsync("DELETE FROM cards_by_id WHERE namespace = ? AND local_id = ?");

                this.deleteSpecificPetImageStatement = await session.PrepareAsync("DELETE FROM images_by_card_id WHERE namespace = ? AND local_id = ? AND image_num = ?");
                this.deleteAllPetImagesStatement = await session.PrepareAsync("DELETE FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                this.getAllPetImagesStatementIncBin = await session.PrepareAsync("SELECT * FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                this.getAllPetImagesStatement = await session.PrepareAsync("SELECT namespace,local_id,image_num,detection_confidence,detection_rotation FROM images_by_card_id WHERE namespace = ? AND local_id = ?");
                this.getParticularPetImageStatement = await session.PrepareAsync("SELECT * FROM images_by_card_id WHERE namespace = ? AND local_id = ? AND image_num = ?");
                this.addPetImageStatement = await session.PrepareAsync("INSERT INTO images_by_card_id (namespace, local_id, image_num, annotated_image, annotated_image_type, extracted_pet_image, detection_confidence, detection_rotation) values (?,?,?,?,?,?,?,?)");

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

        public async Task<bool> SetPetCardAsync(string ns, string localID, PetCard card)
        {
            await EnsureConnectionInitialized();
            /*
            namespace text,
            local_id text,
            provenance_url text,
            animal tinyint,
            animal_sex tinyint,
            card_type tinyint,
            event_time tuple<timestamp,text>, -- time moment + provenance
            card_creation_time timestamp,
            event_location location,
            contact_info frozen<contact_info>, -- frozen as contact info contains collections
            features map<text,frozen<list<double>>>

            */

            var statement = this.insertPetCardStatement.Bind(
                ns,
                localID,
                card.ProvenanceURL,
                EncodeAnimal(card.Animal),
                EncodePetSex(card.AnimalSex),
                EncodeCardType(card.CardType),
                Tuple.Create(card.EventTime, card.EventTimeProvenance),
                card.CardCreationTime,
                card.Location,
                card.ContactInfo
                );

            await session.ExecuteAsync(statement);

            return true;
        }

        public async Task<bool> AddPetPhotoAsync(string ns, string localID, int imageNum, PetPhoto photo)
        {
            await EnsureConnectionInitialized();

            //namespace text,
            //local_id text,
            //image_num tinyint,
            //annotated_image blob,
            //annotated_image_type text,
            //extracted_pet_image blob,
            //detection_confidence double,
            //detection_rotation tinyint,
            var statement = this.addPetImageStatement.Bind(
                ns,
                localID,
                (sbyte)imageNum,
                photo.AnnotatedImage,
                photo.AnnotatedImageType,
                photo.ExtractedImage,
                photo.DetectionConfidence,
                (sbyte)photo.DetectionRotation
                );
            var res = await this.session.ExecuteAsync(statement);
            return true;
        }

        public async Task<bool> DeletePetCardAsync(string ns, string localID)
        {
            await EnsureConnectionInitialized();

            var statement = this.deletePetCardStatement.Bind(ns, localID);

            await session.ExecuteAsync(statement);

            return true;
        }

        public async Task<PetCard> GetPetCardAsync(string ns, string localID)
        {
            await EnsureConnectionInitialized();

            var statement = this.getPetCardStatement.Bind(ns, localID);
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

        private static PetPhoto CovertRowToPetPhoto(Row row, bool includeBinData)
        {
            //namespace text,
            //local_id text,
            //image_num tinyint,
            //annotated_image blob,
            //annotated_image_type text,
            //extracted_pet_image blob,
            //detection_confidence double,
            //detection_rotation tinyint,
            return new PetPhoto()
            {
                ImageNum = row.GetValue<sbyte>("image_num"),
                AnnotatedImage = includeBinData ? row.GetValue<byte[]>("annotated_image") : null,
                AnnotatedImageType = includeBinData ? row.GetValue<string>("annotated_image_type") : null,
                ExtractedImage = includeBinData ? row.GetValue<byte[]>("extracted_pet_image") : null,
                DetectionConfidence = row.GetValue<double>("detection_confidence"),
                DetectionRotation = row.GetValue<sbyte>("detection_rotation")
            };
        }

        public async IAsyncEnumerable<PetPhoto> GetPetPhotosAsync(string ns, string localID, bool includeBinData)
        {
            await EnsureConnectionInitialized();

            BoundStatement statement =
                includeBinData ?
                    this.getAllPetImagesStatementIncBin.Bind(ns, localID)
                    : this.getAllPetImagesStatement.Bind(ns, localID);
            var rows = await this.session.ExecuteAsync(statement);
            foreach (var row in rows)
            {
                yield return CovertRowToPetPhoto(row, includeBinData);
            }
        }

        public async Task<bool> DeletePetPhoto(string ns, string localID, int photoNum = -1)
        {
            await EnsureConnectionInitialized();

            BoundStatement statement = (photoNum == -1) ?
                (this.deleteAllPetImagesStatement.Bind(ns, localID)) :
                (this.deleteSpecificPetImageStatement.Bind(ns, localID, photoNum));
            await this.session.ExecuteAsync(statement);
            return true;
        }

        public async Task<PetPhoto> GetPetPhotoAsync(string ns, string localID, int imageNum)
        {
            await EnsureConnectionInitialized();

            var statement = this.getParticularPetImageStatement.Bind(ns, localID, (sbyte)imageNum);
            var row = (await this.session.ExecuteAsync(statement)).FirstOrDefault();
            if (row != null)
            {
                PetPhoto photo = CovertRowToPetPhoto(row, true);
                return photo;
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> SetFeatureVectorAsync(string ns, string localID, string featuredIdent, double[] features)
        {
            await EnsureConnectionInitialized();

            var newDict = new Dictionary<string, IEnumerable<double>>();
            newDict.Add(featuredIdent, features);
            var statement = new SimpleStatement($"UPDATE cards_by_id SET features = features + ? WHERE namespace = ? AND local_id = ?",
                newDict, ns, localID);
            await this.session.ExecuteAsync(statement);
            return true;
        }
    }
}
