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

    /// <summary>
    /// Original photo is identified by the ImageNum (order num) in the pet card.
    /// </summary>
    public class PhotoDescriptorOut
    {
        public int ImageNum { get; private set; }
        public Guid Uuid { get; private set; }
        public IReadOnlyDictionary<string, double[]> FeatureVectors { get; private set; }

        public PhotoDescriptorOut(int num, Guid uuid, IReadOnlyDictionary<string, double[]> features)
        {
            this.ImageNum = num;
            this.Uuid = uuid;
            this.FeatureVectors = features;

        }
    }
}
