using System;
using System.Collections.Generic;
using System.Management;

namespace powerLabel
{
    public class VideoControllerConfig
    {
        public int id { get; set; }
        public VideoController videoController { get; set; }
        public ComputerSystem computerSystem { get; set; }

        public static List<VideoControllerConfig> GetVideoControllers(ComputerSystem system)
        {
            List<VideoControllerConfig> list = new List<VideoControllerConfig>();

            ManagementObjectCollection returned = PSInterface.RunObjectQuery("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject item in returned)
            {
                VideoControllerConfig videoControllerConfig = new VideoControllerConfig();
                videoControllerConfig.videoController = new VideoController();

                videoControllerConfig.videoController.manufacturer = item["AdapterCompatibility"] as string ?? "";
                videoControllerConfig.videoController.name = item["Caption"] as string ?? "Unknown GPU";
                videoControllerConfig.videoController.vram = (item["AdapterRam"] == null)
                    ? 0u
                    : Convert.ToUInt32(item["AdapterRam"]);

                videoControllerConfig.computerSystem = system;

                list.Add(videoControllerConfig);
            }

            return list;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            VideoControllerConfig videoControllerConfig = (VideoControllerConfig)obj;

            return this.videoController.Equals(videoControllerConfig.videoController);
        }
    }
}