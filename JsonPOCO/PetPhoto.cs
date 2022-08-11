using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CassandraAPI.JsonPoco
{
    public class PetPhoto
    {
        public int ImageNum { get; set; }
        public string AnnotatedImage { get; set; }
        public string AnnotatedImageType { get; set; }
        public string ExtractedImage { get; set; }
        public double DetectionConfidence { get; set; }
        public int DetectionRotation { get; set; }

        public PetPhoto(CassandraAPI.PetPhoto photo)
        {
            this.ImageNum = photo.ImageNum;
            this.AnnotatedImageType = photo?.AnnotatedImageType ?? null;
            this.DetectionConfidence = photo.DetectionConfidence;
            this.DetectionRotation = photo.DetectionRotation;
            this.AnnotatedImage = photo.AnnotatedImage != null ? Convert.ToBase64String(photo.AnnotatedImage) : null;
            this.ExtractedImage = photo.ExtractedImage != null ? Convert.ToBase64String(photo.ExtractedImage) : null;
        }

        public PetPhoto() { }

        public CassandraAPI.PetPhoto ToPetPhoto() {
            return new CassandraAPI.PetPhoto()
            {
                ImageNum = this.ImageNum,
                AnnotatedImage = this.AnnotatedImage != null ? Convert.FromBase64String(this.AnnotatedImage) : null,
                ExtractedImage = this.ExtractedImage != null ? Convert.FromBase64String(this.ExtractedImage) : null,
                AnnotatedImageType = this?.AnnotatedImageType ?? null,
                DetectionConfidence = this.DetectionConfidence,
                DetectionRotation = this.DetectionRotation
            };

        }
    }
}
