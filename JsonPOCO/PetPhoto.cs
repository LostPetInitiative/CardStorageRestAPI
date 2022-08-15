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

        public Guid Uuid { get; set; }


        public PetPhoto(PatCardStorageAPI.PetPhoto photo) {
            this.Uuid = photo.Uuid;
            this.ImageB64 = photo.Image != null ? Convert.ToBase64String(photo.Image) : null;
            this.ImageMimeType = photo.ImageMimeType;
        }

        public PatCardStorageAPI.PetOriginalPhoto ToPetPhoto()
        {
            return new PatCardStorageAPI.PetOriginalPhoto()
            {                
                Uuid = this.Uuid,
                Image = this.ImageB64 != null ? Convert.FromBase64String(this.ImageB64) : null,
                ImageMimeType = this.ImageMimeType
            };
        }
    }

    /// <summary>
    /// Original photo is identified by the ImageNum (order num) in the pet card.
    /// </summary>
    public class PetOriginalPhoto : PetPhoto {
        public int ImageNum { get; set; }

        public PetOriginalPhoto(PatCardStorageAPI.PetOriginalPhoto photo):base(photo)
        {
            this.ImageNum = photo.ImageNum;            
        }

        public PatCardStorageAPI.PetOriginalPhoto ToPetOriginalPhoto()
        {
            return new PatCardStorageAPI.PetOriginalPhoto()
            {
                Uuid = this.Uuid,
                Image = this.ImageB64 != null ? Convert.FromBase64String(this.ImageB64) : null,
                ImageMimeType = this.ImageMimeType,
                ImageNum = this.ImageNum
            };
        }

    }
}
