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
            if (system == null) return "";

            // Model
            string modelString = motherboard.model;
            modelString = getShortString(modelString, new string[] {
        @"(?<ZLine>HP Z\w+) (?<Stupidname>Firefly|Power|Fury|Studio|Mini)*(?:(?!G)[a-zA-Z ])*(?<screensize>\d{2}\w?)?(?:(?![G])[A-Za-z .\d])*(?<generation>G\d+)?",
        @"(Precision \w+ \w+)",
        @"(OptiPlex|Vostro|Latitude|XPS|Inspiron|Tower|ProDesk|EliteDesk|Z\d) *(\w+)"
    });

            // CPU
            string cpuString = processor.name;
            cpuString = getShortString(cpuString, new string[] {
        @"(Platinum|Gold|Silver|Bronze)(?: )(\w*-*\d+\w*)",
        @"(\w+-*\d{3,}\w*)(?: )*(v\d)*",
    });
            if (processorAmount > 1)
                cpuString = cpuString.Insert(0, "2x ");

            // RAM
            string ramString = memoryModules.Sum(item => Convert.ToInt64(item.module.capacity)) / 1073741824
                + "GB (" + memoryModules.Count + ") "
                + MemoryModule.memoryTypeLookup[memoryModules.First().module.memoryType];

            // Disks — one per line, OS disk first (already sorted by DiskConfig.GetDisks)
            string diskString = "";
            List<string> disks = new List<string>();
            List<string> doneDisks = new List<string>();
            foreach (DiskConfig disk in system.diskConfigs)
                disks.Add(disk.ToString());
            foreach (string disk in disks)
            {
                if (!doneDisks.Any(a => a == disk))
                {
                    int count = disks.Where(a => a == disk).Count();
                    diskString += (count > 1 ? $"{count}x " : "") + disk.Trim() + "\r\n";
                    doneDisks.Add(disk);
                }
            }

            // GPU — filter out integrated Intel/AMD graphics
            string gpuString = "";
            foreach (VideoControllerConfig gpu in videoControllerConfigs)
            {
                string name = gpu.videoController.name ?? "";
                if (name.Contains("Intel") || name.Contains("AMD Radeon(TM)") ||
                    name.Contains("UHD Graphics") || name.Contains("HD Graphics"))
                    continue;

                string shortName = getShortString(name, new string[] {
            @"(Quadro|RTX|NVS) *(\w+) ?(\d+)?",
            @"(GeForce) (\wTX?) (\d{3,})(?: (\w+))*"
        });
                gpuString += shortName + "\r\n";
            }

            return modelString + "\r\n"
                + cpuString + " | " + ramString + "\r\n"
                + diskString
                + gpuString.TrimEnd();
        }

        public static string getShortString(string input, string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(input, pattern);
                if (match.Success)
                {
                    string result = "";
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.Groups[i].Value != "")
                            result += match.Groups[i].Value + " ";
                    }
                    return result.Trim();
                }
            }
            return input;
        }
    }
}