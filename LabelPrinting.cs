using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Xps;

namespace powerLabel
{
    public class LabelPrinting
    {
        private static readonly object printLock = new object();

        private static string printerHost = "HP-Z400";
        private static string printerShareName = "labelPrinter";

        private static bool printerAccessPrepared = false;
        private static PrintQueue cachedPrintQueue = null;

        public static void printLabel(Grid grid)
        {
            if (grid == null)
            {
                return;
            }

            FrameworkElement element = grid as FrameworkElement;
            if (element == null)
            {
                return;
            }

            lock (printLock)
            {
                try
                {
                    PrintQueue queue = EnsureBestAvailableQueue();
                    PrintElementToQueue(queue, element);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Printing process returned an error: " + ex.Message);
                }
            }
        }

        private static PrintQueue EnsureBestAvailableQueue()
        {
            if (cachedPrintQueue != null)
            {
                try
                {
                    cachedPrintQueue.Refresh();
                    return cachedPrintQueue;
                }
                catch
                {
                    try
                    {
                        cachedPrintQueue.Dispose();
                    }
                    catch
                    {
                    }

                    cachedPrintQueue = null;
                }
            }

            try
            {
                cachedPrintQueue = OpenRemoteQueue();
                return cachedPrintQueue;
            }
            catch
            {
            }

            if (!printerAccessPrepared)
            {
                PreparePrinterAccessOnce();
                printerAccessPrepared = true;
            }

            try
            {
                cachedPrintQueue = OpenRemoteQueue();
                return cachedPrintQueue;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to access printer '\\\\{printerHost}\\{printerShareName}' even after SMB/RPC preparation. {ex.Message}");
            }
        }

        private static PrintQueue OpenRemoteQueue()
        {
            PrintServer server = new PrintServer($@"\\{printerHost}");
            PrintQueue queue = new PrintQueue(server, printerShareName);
            queue.Refresh();
            return queue;
        }

        private static void PreparePrinterAccessOnce()
        {
            string rpcPath = @"HKLM:\Software\Policies\Microsoft\Windows NT\Printers\RPC";

            string command =
                $"if (-not (Test-Path '{rpcPath}')) {{ New-Item -Path '{rpcPath}' -Force | Out-Null }}; " +
                $"New-ItemProperty -Path '{rpcPath}' -Name 'RpcUseNamedPipeProtocol' -Value 1 -PropertyType DWORD -Force | Out-Null; " +
                "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force; " +
                "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force;";

            PSInterface.RunPowershell(command);
        }

        private static void PrintElementToQueue(PrintQueue queue, FrameworkElement element)
        {
            if (queue == null)
            {
                throw new Exception("Print queue is null.");
            }

            element.UpdateLayout();

            PrintTicket ticket = queue.DefaultPrintTicket ?? new PrintTicket();
            ticket.PageMediaSize = new PageMediaSize(216, 120);

            PrintCapabilities capabilities = queue.GetPrintCapabilities(ticket);
            if (capabilities?.PageImageableArea == null)
            {
                throw new Exception("Printer did not return a valid imageable area.");
            }

            double elementWidth = element.ActualWidth;
            double elementHeight = element.ActualHeight;

            if (elementWidth <= 0 || elementHeight <= 0)
            {
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                element.Arrange(new Rect(element.DesiredSize));
                element.UpdateLayout();

                elementWidth = element.ActualWidth;
                elementHeight = element.ActualHeight;
            }

            if (elementWidth <= 0 || elementHeight <= 0)
            {
                throw new Exception("Label visual has invalid size.");
            }

            double scale = Math.Min(
                capabilities.PageImageableArea.ExtentWidth / elementWidth,
                capabilities.PageImageableArea.ExtentHeight / elementHeight);

            Transform originalTransform = element.LayoutTransform;

            try
            {
                element.LayoutTransform = new ScaleTransform(scale, scale);

                Size arrangedSize = new Size(
                    capabilities.PageImageableArea.ExtentWidth,
                    capabilities.PageImageableArea.ExtentHeight);

                element.Measure(arrangedSize);
                element.Arrange(
                    new Rect(
                        new Point(
                            capabilities.PageImageableArea.OriginWidth,
                            capabilities.PageImageableArea.OriginHeight),
                        arrangedSize));

                element.UpdateLayout();

                XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(queue);
                writer.Write(element, ticket);
            }
            finally
            {
                element.LayoutTransform = originalTransform;
                element.UpdateLayout();
            }
        }

        public static string formatString(string str)
        {
            str = str.Replace(" ", "\u00a0").Replace(".", "\u200B");
            return str;
        }
    }
}