using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI.JsonPoco
{
    public class PetPhoto
    {
        public string? ImageB64 { get; set; }
        public string? ImageMimeType { get; set; }

        public PetPhoto() { }

        public PetPhoto(PatCardStorageAPI.PetPhoto photo) {
            this.ImageB64 = photo.Image != null ? Convert.ToBase64String(photo.Image) : null;
            this.ImageMimeType = photo.ImageMimeType;
        }

        public PatCardStorageAPI.PetPhoto ToStoragePetPhoto() {
            return new PatCardStorageAPI.PetPhoto(
                this.ImageB64 != null ? Convert.FromBase64String(this.ImageB64) : null,
                this.ImageMimeType
                );
        }
    }

    public class PetPhotoWithUuid : PetPhoto {

        public Guid Uuid { get; set; }        

        public PetPhotoWithUuid(PatCardStorageAPI.PetPhotoWithGuid photo): base(photo)
        {
            this.Uuid = photo.Uuid;
        }
    }

    /// <summary>
    /// Original photo is identified by the ImageNum (order num) in the pet card.
    /// </summary>
    public class PetOriginalPhoto : PetPhotoWithUuid
    {
        public int ImageNum { get; set; }

        public PetOriginalPhoto(PatCardStorageAPI.PetOriginalPhoto photo):base(photo)
        {
            this.ImageNum = photo.ImageNum;            
        }
    }
}
