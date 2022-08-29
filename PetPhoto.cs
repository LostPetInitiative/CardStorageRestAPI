using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI
{
    public class PetPhoto {
        public byte[]? Image { get; set; }
        public string? ImageMimeType { get; set; }        

        public PetPhoto(byte[]? image, string? imageMimeType)
        {
            Image = image;
            ImageMimeType = imageMimeType;
            
        }
    }

    public class PetPhotoWithGuid: PetPhoto {
        public Guid Uuid { get; set; }

        public PetPhotoWithGuid(Guid uuid, byte[]? image, string? imageMimeType) : base(image, imageMimeType) {
            Uuid = uuid;
        }
    }

    public class PetOriginalPhoto : PetPhotoWithGuid
    {
        public int ImageNum { get; set; }

        public PetOriginalPhoto(Guid uuid, byte[]? image, string? imageMimeType, int imageNum) :
            base(uuid,image,imageMimeType)
        {
            ImageNum = imageNum;
        }
    }
}
