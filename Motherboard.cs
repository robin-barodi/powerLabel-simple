using System;
using System.Management;

namespace powerLabel
{
    public class Motherboard
    {
        public int id { get; set; }
        public string model { get; set; }
        public string manufacturer { get; set; }
        public string serialNumber { get; set; }

        public static Motherboard GetMotherboard()
        {
            Motherboard motherboard = new Motherboard
            {
                model = "Unknown Model",
                manufacturer = "Unknown Manufacturer",
                serialNumber = ""
            };

            foreach (ManagementObject system in PSInterface.RunObjectQuery("SELECT * FROM Win32_ComputerSystem"))
            {
                motherboard.manufacturer = system["Manufacturer"] as string ?? "Unknown Manufacturer";

                if (motherboard.manufacturer.Trim().Equals("Lenovo", StringComparison.InvariantCultureIgnoreCase))
                {
                    motherboard.model = system["SystemFamily"] as string ?? "Unknown Model";
                }
                else
                {
                    motherboard.model = system["Model"] as string ?? "Unknown Model";
                }

                break;
            }

            foreach (ManagementObject bios in PSInterface.RunObjectQuery("SELECT * FROM Win32_Bios"))
            {
                motherboard.serialNumber = bios["SerialNumber"] as string ?? "";
                break;
            }

            return motherboard;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Motherboard mobo = (Motherboard)obj;

            return (this.model ?? "").Trim() == (mobo.model ?? "").Trim() &&
                   (this.manufacturer ?? "").Trim() == (mobo.manufacturer ?? "").Trim() &&
                   (this.serialNumber ?? "").Trim() == (mobo.serialNumber ?? "").Trim();
        }
    }
}