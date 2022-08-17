using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PatCardStorageAPI
{
    /// <summary>
    /// POCO
    /// </summary>
    public class PetCard
    {
        public string CardType { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public DateTimeOffset EventTime { get; set; }
        public string EventTimeProvenance { get; set; }
        public DateTimeOffset CardCreationTime { get; set; }
        public Location Location { get; set; }
        public string Animal { get; set; }
        public string? AnimalSex { get; set; }
        public string ProvenanceURL { get; set; }
        public SortedDictionary<string, double[]> Features { get; set; }
    }

    public class ContactInfo
    {
        public string? Name { get; set; }
        public string[] Tel { get; set; }
        public string[] Website { get; set; }
        public string[] Email { get; set; }
        public string? Comment { get; set; }
    }

    public class Location
    {
        public string? Address { get; set; }
        public string? CoordsProvenance { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
    }

}
