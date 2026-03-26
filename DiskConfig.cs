using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace powerLabel
{
    public class DiskConfig
    {
        public DiskConfig()
        {
        }

        public DiskConfig(ComputerSystem computerSystem)
        {
            this.disk = new Disk();
            this.computerSystem = computerSystem;
        }

        public int id { get; set; }
        public Disk disk { get; set; }
        public ComputerSystem computerSystem { get; set; }
        public bool systemDisk { get; set; }
        public string busType { get; set; }

        private static readonly string[] busTypeEncoding =
        {
            "Unknown",
            "SCSI",
            "ATAPI",
            "ATA",
            "IEEE 1394",
            "SSA",
            "Fibre Channel",
            "USB",
            "RAID",
            "iSCSI",
            "SAS",
            "SATA",
            "SD",
            "MMC",
            "MAX",
            "File Backed Virtual",
            "Storage Spaces",
            "NVMe"
        };

        public static List<DiskConfig> GetDisks(ComputerSystem system)
        {
            List<DiskConfig> list = new List<DiskConfig>();
            uint osDiskId = 255;

            ManagementObjectCollection partitions = PSInterface.RunObjectQuery("SELECT * FROM Win32_DiskPartition");
            foreach (ManagementObject item in partitions)
            {
                bool isBootPartition = (item["BootPartition"] != null) && Convert.ToBoolean(item["BootPartition"]);
                if (isBootPartition)
                {
                    osDiskId = (item["DiskIndex"] == null) ? 255u : Convert.ToUInt32(item["DiskIndex"]);
                    break;
                }
            }

            ManagementScope scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
            scope.Connect();

            ObjectQuery query = new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection returned = searcher.Get();

            foreach (ManagementObject item in returned)
            {
                DiskConfig diskConfig = new DiskConfig(system);

                diskConfig.disk.model = item["Model"] as string ?? "Unknown Disk";
                diskConfig.disk.size = (item["Size"] == null) ? 0UL : Convert.ToUInt64(item["Size"]);
                diskConfig.disk.serialNumber = item["SerialNumber"] as string ?? "";

                ushort busTypeCode = (item["BusType"] == null) ? (ushort)0 : Convert.ToUInt16(item["BusType"]);
                if (busTypeCode < busTypeEncoding.Length)
                {
                    diskConfig.busType = busTypeEncoding[busTypeCode];
                }
                else
                {
                    diskConfig.busType = "Unknown";
                }

                ushort mediaTypeCode = (item["MediaType"] == null) ? (ushort)0 : Convert.ToUInt16(item["MediaType"]);
                switch (mediaTypeCode)
                {
                    case 3:
                        diskConfig.disk.mediaType = "HDD";
                        break;
                    case 4:
                        diskConfig.disk.mediaType = "SSD";
                        break;
                    case 5:
                        diskConfig.disk.mediaType = "SCM";
                        break;
                    default:
                        diskConfig.disk.mediaType = "Unknown";
                        break;
                }

                uint deviceId = 999999;
                if (item["DeviceId"] != null)
                {
                    uint.TryParse(item["DeviceId"].ToString(), out deviceId);
                }

                if (deviceId == osDiskId)
                {
                    diskConfig.systemDisk = true;
                }

                if (diskConfig.busType == "USB")
                {
                    continue;
                }

                list.Add(diskConfig);
            }

            return list
                .OrderByDescending(disk => disk.systemDisk)
                .ThenByDescending(disk => disk.disk.mediaType == "SSD")
                .ThenByDescending(disk => disk.disk.size)
                .ToList();
        }

        public override string ToString()
        {
            ulong shortSize = disk.size / 1000000000;
            string unit;
            string os = "";

            if (shortSize >= 1000)
            {
                unit = "TB";
                shortSize = shortSize / 1000;
            }
            else
            {
                unit = "GB";
            }

            if (systemDisk && computerSystem?.operatingSystem != null)
            {
                string osCaption = computerSystem.operatingSystem.caption ?? "";
                string osLanguage = computerSystem.operatingSystem.language ?? "";

                if (osCaption.Contains("11"))
                {
                    os = "+ W11P";
                }
                else if (osCaption.Contains("10"))
                {
                    os = "+ W10P";
                }

                if (!string.IsNullOrWhiteSpace(osLanguage) &&
                    !osLanguage.Equals("NL", StringComparison.OrdinalIgnoreCase))
                {
                    os += " " + osLanguage;
                }
            }

            if (disk.mediaType == "SSD")
            {
                if (busType == "NVMe")
                {
                    return $"{shortSize}{unit} {busType} {os}".Trim();
                }

                return $"{shortSize}{unit} {busType} SSD {os}".Trim();
            }

            if (disk.mediaType == "HDD")
            {
                return $"{shortSize}{unit} HDD {os}".Trim();
            }

            return $"{shortSize}{unit} {disk.mediaType} {os}".Trim();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            DiskConfig diskConfig = (DiskConfig)obj;

            return this.disk.Equals(diskConfig.disk) &&
                   (this.busType ?? "").Trim() == (diskConfig.busType ?? "").Trim() &&
                   this.systemDisk == diskConfig.systemDisk;
        }
    }
}