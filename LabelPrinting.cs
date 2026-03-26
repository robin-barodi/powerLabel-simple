using System;
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
                string RPCPath = "HKLM:\\Software\\Policies\\Microsoft\\Windows NT\\Printers\\RPC";
                string setup = $@"
                    If (-NOT (Test-Path '{RPCPath}')) {{ New-Item -Path '{RPCPath}' -Force | Out-Null }}
                    New-ItemProperty -Path '{RPCPath}' -Name 'RpcUseNamedPipeProtocol' -Value 1 -PropertyType DWORD -Force | Out-Null
                    Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force
                    Set-SmbClientConfiguration -RequireSecuritySignature $false -Force
                    Add-Printer -ConnectionName '\\\\{printerHost}\\{printerShareName}'
                ";
                PSInterface.RunPowershell(setup);

                FrameworkElement e = grid as FrameworkElement;
                if (e == null) return;

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

                string teardown = $@"
                    Remove-Printer -Name '\\\\{printerHost}\\{printerShareName}' -ErrorAction SilentlyContinue
                    New-ItemProperty -Path '{RPCPath}' -Name 'RpcUseNamedPipeProtocol' -Value 0 -PropertyType DWORD -Force | Out-Null
                    Set-SmbClientConfiguration -EnableInsecureGuestLogons $false -Force
                    Set-SmbClientConfiguration -RequireSecuritySignature $true -Force
                ";
                PSInterface.RunPowershell(teardown);
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
