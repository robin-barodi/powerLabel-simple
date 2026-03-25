using System;
using System.Management;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace powerLabel
{
    public class LabelPrinting
    {
        private static string printerHost = "HP-Z400";
        private static string printerShareName = "labelPrinter";

        public static void printLabel(Grid grid)
        {
            try
            {
                // Set registry to allow RPC over remote pipes
                string RPCPath = "HKLM:\\Software\\Policies\\Microsoft\\Windows NT\\Printers\\RPC";
                PSInterface.RunPowershell($"If (-NOT (Test-Path '{RPCPath}')) {{ New-Item -Path '{RPCPath}' -Force | Out-Null}}");
                PSInterface.RunPowershell($"New-ItemProperty -Path '{RPCPath}' -Name 'RpcUseNamedPipeProtocol' -Value 1 -PropertyType DWORD -Force");

                // Workaround for W11 24H2
                PSInterface.RunPowershell("Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force");
                PSInterface.RunPowershell("Set-SmbClientConfiguration -RequireSecuritySignature $false -Force");

                // Add printer
                PSInterface.RunPowershell($"Add-Printer -ConnectionName \"\\\\{printerHost}\\{printerShareName}\"");

                FrameworkElement e = grid as FrameworkElement;
                if (e == null)
                    return;

                PrintDialog pd = new PrintDialog();
                PrintServer myPrintServer = new PrintServer($"\\\\{printerHost}");
                PrintQueueCollection myPrintQueues = myPrintServer.GetPrintQueues();
                foreach (PrintQueue pq in myPrintQueues)
                {
                    pd.PrintQueue = pq;
                }

                pd.PrintTicket.PageMediaSize = new PageMediaSize(216, 120);

                Transform originalScale = e.LayoutTransform;
                PrintCapabilities capabilities = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);

                double scale = Math.Min(
                    capabilities.PageImageableArea.ExtentWidth / e.ActualWidth,
                    capabilities.PageImageableArea.ExtentHeight / e.ActualHeight);

                e.LayoutTransform = new ScaleTransform(scale, scale);

                System.Windows.Size sz = new System.Windows.Size(
                    capabilities.PageImageableArea.ExtentWidth,
                    capabilities.PageImageableArea.ExtentHeight);

                e.Measure(sz);
                e.Arrange(new System.Windows.Rect(
                    new System.Windows.Point(
                        capabilities.PageImageableArea.OriginWidth,
                        capabilities.PageImageableArea.OriginHeight),
                    sz));

                pd.PrintVisual(grid, "My Print");
                e.LayoutTransform = originalScale;

                // Remove printer
                ConnectionOptions options = new ConnectionOptions();
                options.EnablePrivileges = true;
                ManagementScope scope = new ManagementScope(ManagementPath.DefaultPath, options);
                scope.Connect();
                ManagementClass printerClass = new ManagementClass("Win32_Printer");
                ManagementObjectCollection printers = printerClass.GetInstances();
                foreach (ManagementObject printer in printers)
                {
                    if ((string)printer["ShareName"] == printerShareName)
                    {
                        printer.Delete();
                    }
                }

                // Unset RPC
                PSInterface.RunPowershell($"New-ItemProperty -Path '{RPCPath}' -Name 'RpcUseNamedPipeProtocol' -Value 0 -PropertyType DWORD -Force");

                // Reverse 24H2 workaround
                PSInterface.RunPowershell("Set-SmbClientConfiguration -EnableInsecureGuestLogons $false -Force");
                PSInterface.RunPowershell("Set-SmbClientConfiguration -RequireSecuritySignature $true -Force");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing process returned an error: " + ex.Message);
            }
        }

        public static string formatString(string str)
        {
            str = str.Replace(" ", "\u00a0").Replace(".", "\u200B");
            return str;
        }
    }
}