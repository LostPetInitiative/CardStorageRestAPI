using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.Storage
{
    public interface IPhotoStorage
    {
        IAsyncEnumerable<PetOriginalPhoto> ListOriginalPhotosAsync(string ns, string localID);
        Task<PetPhotoWithGuid?> GetOriginalPhotoAsync(string ns, string localID, int imageNum);
        Task<PetPhoto?> GetProcessedPetPhotoAsync(Guid imageUuid, string processingIdent);

        /// <summary>
        /// photoNum = -1 means: delete all of the photos for the specified pet
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="photoNum"></param>
        /// <returns></returns>
        Task<bool> DeleteOriginalPetPhoto(string ns, string localID, int photoNum = -1);
        Task<bool> DeleteProcessedPhoto(Guid imageUuid, string processingIdent);

        /// <summary>
        /// Returns Guid if the photo is successfully stored. If the photo already exists does NOT replace it, returns null.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="imageNum"></param>
        /// <param name="photo"></param>
        /// <returns></returns>
        Task<(Guid uuid, bool created)> AddOriginalPetPhotoAsync(string ns, string localID, int imageNum, PetPhoto photo);

        /// <summary>
        /// Returns whether new processed photo is added.
        /// If the photo already exists does NOT replace it, returns false.
        /// </summary>
        /// <param name="imageUuid"></param>
        /// <param name="processingIdent"></param>
        /// <param name="photo"></param>
        /// <returns></returns>
        Task<bool> AddProcessedPetPhotoAsync(Guid imageUuid, string processingIdent, PetPhoto photo);
    }
}
