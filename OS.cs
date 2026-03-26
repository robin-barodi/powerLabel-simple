using System;
using System.Collections.Generic;
using System.Management;

namespace powerLabel
{
    public class OS
    {
        public int id { get; set; }
        public string caption { get; set; }
        public string language { get; set; }

        public static Dictionary<uint, string> languageTable = new Dictionary<uint, string>()
        {
            { 9u, "EN" },
            { 1031u, "DE" },
            { 1033u, "US" },
            { 1043u, "NL" }
        };

        public static OS GetOS()
        {
            OS os = new OS
            {
                caption = "Unknown OS",
                language = "Unknown"
            };

            foreach (ManagementObject item in PSInterface.RunObjectQuery("SELECT * FROM Win32_OperatingSystem"))
            {
                os.caption = item["Caption"] as string ?? "Unknown OS";

                uint languageCode = (item["OSLanguage"] == null) ? 0u : Convert.ToUInt32(item["OSLanguage"]);

                if (languageTable.ContainsKey(languageCode))
                {
                    os.language = languageTable[languageCode];
                }
                else
                {
                    os.language = "Unknown";
                }

                break;
            }

            return os;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            OS os = (OS)obj;

            return (this.caption ?? "").Trim() == (os.caption ?? "").Trim() &&
                   (this.language ?? "").Trim() == (os.language ?? "").Trim();
        }
    }
}