using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CassandraAPI.Storage
{
    public interface IPhotoStorage
    {
        IAsyncEnumerable<PetPhoto> GetPetPhotosAsync(string ns, string localID, bool includeBinData);
        Task<PetPhoto> GetPetPhotoAsync(string ns, string localID, int imageNum);

        /// <summary>
        /// photoNum = -1 means: delete all of the photos for the specified pet
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="photoNum"></param>
        /// <returns></returns>
        Task<bool> DeletePetPhoto(string ns, string localID, int photoNum = -1);
        Task<bool> AddPetPhotoAsync(string ns, string localID, int imageNum, PetPhoto photo);
    }
}
