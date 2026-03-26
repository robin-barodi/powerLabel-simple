using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace powerLabel
{
    public class ComputerSystem
    {
        public Motherboard motherboard { get; set; }
        public Processor processor { get; set; }
        public int processorAmount { get; set; }
        public List<MemoryConfig> memoryModules { get; set; }
        public List<DiskConfig> diskConfigs { get; set; }
        public List<VideoControllerConfig> videoControllerConfigs { get; set; }
        public OS operatingSystem { get; set; }
        public static ComputerSystem system { get; set; }

        public static ComputerSystem GetSystem()
        {
            system = new ComputerSystem();

            system.motherboard = Motherboard.GetMotherboard();
            system.processor = Processor.GetProcessor(system);
            system.memoryModules = MemoryConfig.GetMemory(system);
            system.diskConfigs = DiskConfig.GetDisks(system);
            system.videoControllerConfigs = VideoControllerConfig.GetVideoControllers(system);
            system.operatingSystem = OS.GetOS();

            return system;
        }

        public string getString()
        {
            if (this == null)
            {
                return "";
            }

            // =========================
            // SYSTEM / MODEL LINE
            // =========================
            string modelString = motherboard?.model ?? "Unknown Model";
            modelString = getShortString(modelString, new string[]
            {
                @"(?<ZLine>HP Z\w+) (?<Stupidname>Firefly|Power|Fury|Studio|Mini)*(?:(?!G)[a-zA-Z ])*(?<screensize>\d{2}\w?)?(?:(?![G])[A-Za-z .\d])*(?<generation>G\d+)?",
                @"(EliteBook|ProBook|ZBook|OmniBook|Dragonfly|Elite x360)\s*([A-Za-z0-9\- ]+)",
                @"(Precision \w+ \w+)",
                @"(Latitude|XPS|Inspiron|Vostro|OptiPlex)\s*(\w+)",
                @"(ProDesk|EliteDesk)\s*(\w+)"
            });

            modelString = SplitLongFirstLine(modelString, 24);

            // =========================
            // CPU LINE
            // =========================
            string cpuString = processor?.name ?? "Unknown CPU";
            cpuString = getShortString(cpuString, new string[]
            {
                @"(Platinum|Gold|Silver|Bronze)(?: )(\w*-*\d+\w*)",
                @"(\w+-*\d{3,}\w*)(?: )*(v\d)*",
            });

            if (processorAmount > 1 && !string.IsNullOrWhiteSpace(cpuString))
            {
                cpuString = processorAmount + "x " + cpuString;
            }

            // =========================
            // RAM LINE
            // =========================
            string ramString;
            if (memoryModules == null || memoryModules.Count == 0)
            {
                ramString = "Unknown RAM";
            }
            else
            {
                long totalGb = memoryModules.Sum(item => Convert.ToInt64(item.module.capacity)) / 1073741824;

                uint ramTypeCode = memoryModules.First().module.memoryType;
                string ramType = MemoryModule.memoryTypeLookup.ContainsKey(ramTypeCode)
                    ? MemoryModule.memoryTypeLookup[ramTypeCode]
                    : "Unknown";

                ramString = totalGb + "GB (" + memoryModules.Count + ") " + ramType;
            }

            // =========================
            // DISKS
            // =========================
            string diskString = "";
            List<string> disks = new List<string>();
            List<string> doneDisks = new List<string>();

            if (diskConfigs != null)
            {
                foreach (DiskConfig disk in diskConfigs)
                {
                    disks.Add(disk.ToString());
                }

                foreach (string disk in disks)
                {
                    if (!doneDisks.Any(a => a == disk))
                    {
                        int count = disks.Where(a => a == disk).Count();
                        diskString += (count > 1 ? $"{count}x " : "") + disk.Trim() + "\r\n";
                        doneDisks.Add(disk);
                    }
                }
            }

            // =========================
            // GPU(S) - INCLUDE ALL
            // =========================
            string gpuString = "";
            List<string> doneGpus = new List<string>();

            if (videoControllerConfigs != null)
            {
                foreach (VideoControllerConfig gpu in videoControllerConfigs)
                {
                    string name = gpu.videoController?.name ?? "Unknown GPU";
                    string shortName = ShortenGpuName(name).Trim();

                    if (string.IsNullOrWhiteSpace(shortName))
                    {
                        continue;
                    }

                    if (!doneGpus.Contains(shortName))
                    {
                        gpuString += shortName + "\r\n";
                        doneGpus.Add(shortName);
                    }
                }
            }

            return modelString + "\r\n" +
                   cpuString + " | " + ramString + "\r\n" +
                   diskString +
                   gpuString.TrimEnd();
        }

        public static string getShortString(string input, string[] patterns)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(input, pattern);
                if (match.Success)
                {
                    string result = "";
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(match.Groups[i].Value))
                        {
                            result += match.Groups[i].Value + " ";
                        }
                    }

                    return result.Trim();
                }
            }

            return input.Trim();
        }

        private static string ShortenGpuName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            string shortened = getShortString(input, new string[]
            {
                @"(Quadro|RTX|NVS)\s*([A-Za-z0-9]+)\s*([A-Za-z0-9]+)?",
                @"(GeForce)\s+([A-Za-z]+)\s+(\d{3,4})\s*([A-Za-z0-9]+)?",
                @"(Intel)\s+(Arc)\s+([A-Za-z0-9]+)",
                @"(Intel)\s+(Iris\s+Xe|Iris|UHD\s+Graphics|HD\s+Graphics)\s*([A-Za-z0-9]+)?",
                @"(AMD|Radeon)\s+([A-Za-z0-9\(\)\- ]+)",
            });

            return shortened;
        }

        private static string SplitLongFirstLine(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            input = input.Trim();

            if (input.Length <= maxLength)
            {
                return input;
            }

            int splitIndex = input.LastIndexOf(' ', maxLength);

            if (splitIndex <= 0)
            {
                splitIndex = input.IndexOf(' ', maxLength);
            }

            if (splitIndex <= 0)
            {
                return input;
            }

            string firstLine = input.Substring(0, splitIndex).Trim();
            string secondLine = input.Substring(splitIndex + 1).Trim();

            return firstLine + "\r\n" + secondLine;
        }
    }
}