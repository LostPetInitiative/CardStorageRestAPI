using CardStorageRestAPI;
using Cassandra.DataStax.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.Storage
{
    public interface IPhotoStorage
    {
        IAsyncEnumerable<PetOriginalPhoto> ListOriginalPhotosAsync(AsciiIdentifier ns, AsciiIdentifier localID);
        Task<PetPhotoWithGuid?> GetOriginalPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum);
        Task<PetPhoto?> GetProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent);

        Task<double[]?> GetPhotoFeatures(Guid imageUuid, AsciiIdentifier featuresIdent);

        /// <summary>
        /// photoNum = -1 means: delete all of the photos for the specified pet
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="photoNum"></param>
        /// <returns></returns>
        Task<bool> DeleteOriginalPetPhoto(AsciiIdentifier ns, AsciiIdentifier localID, int photoNum = -1);
        Task<bool> DeleteProcessedPhoto(Guid imageUuid, AsciiIdentifier processingIdent);

        /// <summary>
        /// Returns Guid if the photo is successfully stored. If the photo already exists does NOT replace it, returns null.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="imageNum"></param>
        /// <param name="photo"></param>
        /// <returns></returns>
        Task<(Guid uuid, bool created)> AddOriginalPetPhotoAsync(AsciiIdentifier ns, AsciiIdentifier localID, int imageNum, PetPhoto photo);

        /// <summary>
        /// Returns whether new processed photo is added.
        /// If the photo already exists does NOT replace it, returns false.
        /// </summary>
        /// <param name="imageUuid"></param>
        /// <param name="processingIdent"></param>
        /// <param name="photo"></param>
        /// <returns></returns>
        Task<bool> AddProcessedPetPhotoAsync(Guid imageUuid, AsciiIdentifier processingIdent, PetPhoto photo);

        Task<bool> SetPhotoFeatureVectorAsync(Guid imageUuid, AsciiIdentifier featuresIdent, double[] features);
    }
}
