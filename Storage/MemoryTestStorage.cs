using CardStorageRestAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.Storage
{
    public class MemoryTestStorage : ICardStorage, IPhotoStorage
    {
        public Task<bool> SetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID, PetCard card)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            throw new NotImplementedException();
        }

        public Task<PetCard> GetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            // only serves single card
            if (ns.ToString() == "pet911ru" && localID.ToString() == "rf123")
            {
                var res = new PetCard
                {
                    CardType = "found",
                    ContactInfo = new ContactInfo() { Comment = "This is comment", Tel = new string[] { "911" } },
                    EventTime = new DateTime(2010, 1, 1),
                    Location = new Location() { Address = "Moscow", Lat = 55.3, Lon = 37.5 },
                    Animal = "cat",
                    ProvenanceURL = "http://fake.ru/rf12332123"
                };
                return Task.FromResult(res);
            }
            else
                return Task.FromResult<PetCard>(null);
        }

        public async IAsyncEnumerable<PetOriginalPhoto> ListOriginalPhotosAsync(AsciiIdentifier ns, AsciiIdentifier localID)
        {
            if (ns.ToString() == "pet911ru" && localID.ToString() == "rf123")
            {
                yield return
                    new PetOriginalPhoto(Guid.NewGuid(), null, null, 2);
                yield return new PetOriginalPhoto(Guid.NewGuid(), null, null, 1);                
            }
            else
                yield return null;
        }        

        public Task<PetPhotoWithGuid?> GetOriginalPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum)
        {
            throw new NotImplementedException();
        }

        public Task<PetPhoto?> GetProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteOriginalPetPhoto(AsciiIdentifier ns, AsciiIdentifier localID, int photoNum = -1)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteProcessedPhoto(Guid imageUuid, AsciiIdentifier processingIdent)
        {
            throw new NotImplementedException();
        }
        
        public Task<bool> AddProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent, PetPhoto photo)
        {
            throw new NotImplementedException();
        }

        public Task<(Guid,bool)> AddOriginalPetPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum, PetPhoto photo)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetCardFeatureVectorAsync(AsciiIdentifier ns, AsciiIdentifier localID, AsciiIdentifier featuresIdent, double[] features)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPhotoFeatureVectorAsync(Guid imageUuid, AsciiIdentifier featuresIdent, double[] features)
        {
            throw new NotImplementedException();
        }

        public Task<double[]?> GetPhotoFeatures(Guid imageUuid, AsciiIdentifier featuresIdent)
        {
            throw new NotImplementedException();
        }
    }
}
