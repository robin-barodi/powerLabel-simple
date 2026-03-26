using System;

namespace powerLabel
{
    public class Disk
    {
        public int id { get; set; }
        public string model { get; set; }
        public ulong size { get; set; }
        public string serialNumber { get; set; }
        public string mediaType { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Disk disk = (Disk)obj;

            return (this.model ?? "").Trim() == (disk.model ?? "").Trim() &&
                   this.size == disk.size &&
                   (this.serialNumber ?? "").Trim() == (disk.serialNumber ?? "").Trim() &&
                   (this.mediaType ?? "").Trim() == (disk.mediaType ?? "").Trim();
        }
    }
}