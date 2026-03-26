using System;
using System.Diagnostics;
using System.Management;

namespace powerLabel
{
    public static class PSInterface
    {
        public static ManagementObjectCollection RunObjectQuery(string query)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                return searcher.Get();
            }
            catch (Exception ex)
            {
                throw new Exception($"WMI query failed: {query}\r\n{ex.Message}", ex);
            }
        }

        public static void RunPowershell(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start PowerShell process.");
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"PowerShell command failed with exit code {process.ExitCode}.\r\nCommand: {command}");
                }
            }
        }
    }
}