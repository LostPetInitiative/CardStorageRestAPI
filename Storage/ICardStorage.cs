using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CassandraAPI.Storage
{
    public interface ICardStorage
    {
        Task<PetCard> GetPetCardAsync(string ns, string localID);
        Task<bool> SetFeatureVectorAsync(string ns, string localID, string featuredIdent, double[] features);
        Task<bool> SetPetCardAsync(string ns, string localID,PetCard card);
        Task<bool> DeletePetCardAsync(string ns, string localID);
    }
}
