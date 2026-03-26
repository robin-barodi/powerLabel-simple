using System;
using System.Collections.Generic;
using System.Management;

namespace powerLabel
{
    public class MemoryConfig
    {
        public MemoryConfig()
        {
        }

        public MemoryConfig(ComputerSystem system)
        {
            this.system = system;
            this.module = new MemoryModule();
        }

        public int id { get; set; }
        public MemoryModule module { get; set; }
        public ComputerSystem system { get; set; }
        public uint currentClockspeed { get; set; }

        public static List<MemoryConfig> GetMemory(ComputerSystem system)
        {
            List<MemoryConfig> list = new List<MemoryConfig>();

            ManagementObjectCollection returned = PSInterface.RunObjectQuery("SELECT * FROM Win32_PhysicalMemory");

            foreach (ManagementObject item in returned)
            {
                MemoryConfig memory = new MemoryConfig(system);

                // Always include module (never skip)
                memory.module.capacity = (item["Capacity"] == null)
                    ? 0UL
                    : Convert.ToUInt64(item["Capacity"]);

                memory.currentClockspeed = (item["ConfiguredClockSpeed"] == null)
                    ? 0u
                    : Convert.ToUInt32(item["ConfiguredClockSpeed"]);

                memory.module.maxClockspeed = (item["Speed"] == null)
                    ? 0u
                    : Convert.ToUInt32(item["Speed"]);

                memory.module.formFactor = (item["FormFactor"] == null)
                    ? (ushort)0
                    : Convert.ToUInt16(item["FormFactor"]);

                memory.module.memoryType = (item["SMBIOSMemoryType"] == null)
                    ? 0u
                    : Convert.ToUInt32(item["SMBIOSMemoryType"]);

                list.Add(memory);
            }

            return list;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            MemoryConfig memoryConfig = (MemoryConfig)obj;

            return this.module.Equals(memoryConfig.module) &&
                   this.currentClockspeed == memoryConfig.currentClockspeed;
        }
    }
}