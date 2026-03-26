using System.Collections.Generic;

namespace powerLabel
{
    public class MemoryModule
    {
        public int id { get; set; }
        public ulong capacity { get; set; }
        public uint maxClockspeed { get; set; }
        public uint formFactor { get; set; } // 8 -> DIMM 12 -> SODIMM
        public uint memoryType { get; set; }

        public static Dictionary<uint, string> memoryTypeLookup = new Dictionary<uint, string>()
        {
            [0] = "Unknown",
            [18] = "DDR",
            [19] = "DDR2",
            [20] = "DDR2 FB-DIMM",
            [24] = "DDR3",
            [26] = "DDR4",
            [27] = "LPDDR",
            [28] = "LPDDR2",
            [29] = "LPDDR3",
            [30] = "LPDDR4",
            [34] = "DDR5",
            [35] = "LPDDR5"
        };

        public string partNubmer { get; set; }
        public string serialNubmer { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            MemoryModule memory = (MemoryModule)obj;

            return this.capacity == memory.capacity &&
                   this.maxClockspeed == memory.maxClockspeed &&
                   this.formFactor == memory.formFactor &&
                   this.memoryType == memory.memoryType &&
                   (this.partNubmer ?? "").Trim() == (memory.partNubmer ?? "").Trim() &&
                   (this.serialNubmer ?? "").Trim() == (memory.serialNubmer ?? "").Trim();
        }
    }
}