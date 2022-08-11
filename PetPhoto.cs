using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CassandraAPI
{
    public class PetPhoto
    {
        public int ImageNum { get; set; }
        public byte[] AnnotatedImage { get; set; }
        public string AnnotatedImageType { get; set; }
        public byte[] ExtractedImage { get; set; }
        public double DetectionConfidence { get; set; }
        public int DetectionRotation { get; set; }
    }
}
