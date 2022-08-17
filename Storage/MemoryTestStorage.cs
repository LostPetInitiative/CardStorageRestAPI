using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.Storage
{
    public class MemoryTestStorage : ICardStorage, IPhotoStorage
    {
        public Task<bool> SetPetCardAsync(string ns, string localID, PetCard card)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePetCardAsync(string ns, string localID)
        {
            throw new NotImplementedException();
        }

        public Task<PetCard> GetPetCardAsync(string ns, string localID)
        {
            // only serves single card
            if (ns == "pet911ru" && localID == "rf123")
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

        public async IAsyncEnumerable<PetOriginalPhoto> ListOriginalPhotosAsync(string ns, string localID)
        {
            if (ns == "pet911ru" && localID == "rf123")
            {
                yield return
                    new PetOriginalPhoto(Guid.NewGuid(), null, null, 2);
                yield return new PetOriginalPhoto(Guid.NewGuid(), null, null, 1);                
            }
            else
                yield return null;
        }        

        public Task<PetPhotoWithGuid?> GetOriginalPhotoAsync(string ns, string localID, int imageNum)
        {
            throw new NotImplementedException();
        }

        public Task<PetPhoto?> GetProcessedPetPhotoAsync(Guid imageUuid, string processingIdent)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteOriginalPetPhoto(string ns, string localID, int photoNum = -1)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteProcessedPhoto(Guid imageUuid, string processingIdent)
        {
            throw new NotImplementedException();
        }
        
        public Task<bool> AddProcessedPetPhotoAsync(Guid imageUuid, string processingIdent, PetPhoto photo)
        {
            throw new NotImplementedException();
        }

        public Task<(Guid,bool)> AddOriginalPetPhotoAsync(string ns, string localID, int imageNum, PetPhoto photo)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetCardFeatureVectorAsync(string ns, string localID, string featuresIdent, double[] features)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetPhotoFeatureVectorAsync(Guid imageUuid, string featuresIdent, double[] features)
        {
            throw new NotImplementedException();
        }

        public Task<double[]?> GetPhotoFeatures(Guid imageUuid, string featuresIdent)
        {
            throw new NotImplementedException();
        }
    }
}
