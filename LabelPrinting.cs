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

        private static bool printerAccessPrepared = false;
        private static PrintQueue cachedPrintQueue = null;
        private static string cachedHost = null;
        private static string cachedShare = null;

        private static void ResolveHostAndShare(out string host, out string share)
        {
            string shareName = SettingsHandler.ReadSettings().printerShareName ?? "labelPrinter";

            if (shareName.Contains("\\"))
            {
                int idx = shareName.LastIndexOf('\\');
                host = shareName.Substring(0, idx).TrimStart('\\');
                share = shareName.Substring(idx + 1);
            }
            else
            {
                host = "HP-Z400";
                share = shareName;
            }
        }

        public static void printLabel(Grid grid)
        {
            if (grid == null) return;

            FrameworkElement element = grid as FrameworkElement;
            if (element == null) return;

            lock (printLock)
            {
                try
                {
                    ResolveHostAndShare(out string host, out string share);

                    if (host != cachedHost || share != cachedShare)
                    {
                        try { cachedPrintQueue?.Dispose(); } catch { }
                        cachedPrintQueue = null;
                        printerAccessPrepared = false;
                        cachedHost = host;
                        cachedShare = share;
                    }

                    PrintQueue queue = EnsureBestAvailableQueue(host, share);
                    PrintElementToQueue(queue, element);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Printing process returned an error: " + ex.Message);
                }
            }
        }

        private static PrintQueue EnsureBestAvailableQueue(string host, string share)
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
                    try { cachedPrintQueue.Dispose(); } catch { }
                    cachedPrintQueue = null;
                }
            }

            try
            {
                cachedPrintQueue = OpenRemoteQueue(host, share);
                return cachedPrintQueue;
            }
            catch { }

            if (!printerAccessPrepared)
            {
                PreparePrinterAccessOnce(host, share);
                printerAccessPrepared = true;
            }

            try
            {
                cachedPrintQueue = OpenRemoteQueue(host, share);
                return cachedPrintQueue;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to access printer '\\\\{host}\\{share}' even after SMB/RPC preparation. {ex.Message}");
            }
        }

        private static PrintQueue OpenRemoteQueue(string host, string share)
        {
            PrintServer server = new PrintServer($@"\\{host}");
            PrintQueue queue = new PrintQueue(server, share);
            queue.Refresh();
            return queue;
        }

        private static void PreparePrinterAccessOnce(string host, string share)
        {
            string rpcPath = @"HKLM:\Software\Policies\Microsoft\Windows NT\Printers\RPC";

            string command =
                $"if (-not (Test-Path '{rpcPath}')) {{ New-Item -Path '{rpcPath}' -Force | Out-Null }}; " +
                $"New-ItemProperty -Path '{rpcPath}' -Name 'RpcUseNamedPipeProtocol' -Value 1 -PropertyType DWORD -Force | Out-Null; " +
                "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force; " +
                "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force; " +
                $"Add-Printer -ConnectionName '\\\\{host}\\{share}'";

            PSInterface.RunPowershell(command);
        }

        private static void PrintElementToQueue(PrintQueue queue, FrameworkElement element)
        {
            if (queue == null)
                throw new Exception("Print queue is null.");

            element.UpdateLayout();

            PrintTicket ticket = queue.DefaultPrintTicket ?? new PrintTicket();
            ticket.PageMediaSize = new PageMediaSize(216, 120);
            ticket.PageOrientation = PageOrientation.Landscape; // can stay, but no longer trusted

            PrintCapabilities capabilities = queue.GetPrintCapabilities(ticket);
            if (capabilities?.PageImageableArea == null)
                throw new Exception("Printer did not return a valid imageable area.");

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
                throw new Exception("Label visual has invalid size.");

            double printableWidth = capabilities.PageImageableArea.ExtentWidth;
            double printableHeight = capabilities.PageImageableArea.ExtentHeight;
            double originX = capabilities.PageImageableArea.OriginWidth;
            double originY = capabilities.PageImageableArea.OriginHeight;

            // Because we rotate 90 degrees, width/height swap
            double scale = Math.Min(
                printableWidth / elementHeight,
                printableHeight / elementWidth);

            double rotatedWidth = elementHeight * scale;
            double rotatedHeight = elementWidth * scale;

            double offsetX = originX + (printableWidth - rotatedWidth) / 2.0;
            double offsetY = originY + (printableHeight - rotatedHeight) / 2.0;

            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(element)
                {
                    Stretch = Stretch.Fill
                };

                Matrix m = Matrix.Identity;
                m.Scale(scale, scale);
                m.Rotate(-90);
                m.Translate(offsetX, offsetY + rotatedHeight);

                dc.PushTransform(new MatrixTransform(m));
                dc.DrawRectangle(brush, null, new Rect(0, 0, elementWidth, elementHeight));
                dc.Pop();
            }

            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(queue);
            writer.Write(visual, ticket);
        }

        public static string formatString(string str)
        {
            str = str.Replace(" ", "\u00a0").Replace(".", "\u200B");
            return str;
        }
    }
}