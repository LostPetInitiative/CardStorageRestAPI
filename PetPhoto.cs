using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI
{
    public class PetPhoto {
        public byte[]? Image { get; set; }
        public string? ImageMimeType { get; set; }
        public Guid Uuid { get; set; }
    }

    public class PetOriginalPhoto : PetPhoto
    {
        public int ImageNum { get; set; }
    }
}
