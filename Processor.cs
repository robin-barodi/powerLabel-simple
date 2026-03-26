using System;
using System.Management;

namespace powerLabel
{
    public class Processor
    {
        public int id { get; set; }
        public string name { get; set; }
        public uint cores { get; set; }
        public uint threads { get; set; }

        public static Processor GetProcessor(ComputerSystem system)
        {
            Processor processor = new Processor();
            ManagementObjectCollection returned = PSInterface.RunObjectQuery("SELECT * FROM Win32_Processor");

            uint processorCount = 0;
            ManagementObject firstItem = null;

            foreach (ManagementObject obj in returned)
            {
                if (firstItem == null)
                {
                    firstItem = obj;
                }

                processorCount++;
            }

            system.processorAmount = (int)processorCount;

            if (firstItem == null)
            {
                processor.name = "Unknown CPU";
                processor.cores = 0;
                processor.threads = 0;
                return processor;
            }

            processor.name = firstItem["Name"] as string ?? "Unknown CPU";
            processor.cores = (firstItem["NumberOfCores"] == null) ? 0u : Convert.ToUInt32(firstItem["NumberOfCores"]);
            processor.threads = (firstItem["NumberOfLogicalProcessors"] == null) ? 0u : Convert.ToUInt32(firstItem["NumberOfLogicalProcessors"]);

            return processor;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Processor processor = (Processor)obj;

            return (this.name ?? "").Trim() == (processor.name ?? "").Trim() &&
                   this.cores == processor.cores &&
                   this.threads == processor.threads;
        }
    }
}