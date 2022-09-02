using CardStorageRestAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.Storage
{
    public interface ICardStorage
    {
        Task<PetCard> GetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID);
        Task<bool> SetPetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID,PetCard card);
        Task<bool> DeletePetCardAsync(AsciiIdentifier ns, AsciiIdentifier localID);
        Task<bool> SetCardFeatureVectorAsync(AsciiIdentifier ns, AsciiIdentifier localID, AsciiIdentifier featuresIdent, double[] features);
    }
}
