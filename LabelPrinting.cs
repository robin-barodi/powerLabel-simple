using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace powerLabel
{
    public class LabelPrinting
    {
        public static void printLabel(Grid grid)
        {
            try
            {
                FrameworkElement e = grid as FrameworkElement;
                if (e == null)
                    return;

                string printerShareName = SettingsHandler.ReadSettings().printerShareName;
                if (string.IsNullOrWhiteSpace(printerShareName))
                {
                    MessageBox.Show("No printer configured. Please set the Printer Share Name in Settings.");
                    return;
                }

                PrintDialog pd = new PrintDialog();

                // Find the local printer by share name
                PrintServer localServer = new PrintServer();
                PrintQueueCollection queues = localServer.GetPrintQueues();
                PrintQueue targetQueue = null;
                foreach (PrintQueue pq in queues)
                {
                    if (pq.ShareName.Equals(printerShareName, StringComparison.OrdinalIgnoreCase)
                        || pq.Name.Equals(printerShareName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetQueue = pq;
                        break;
                    }
                }

                if (targetQueue == null)
                {
                    MessageBox.Show($"Printer '{printerShareName}' not found on this machine. Make sure it is installed locally.");
                    return;
                }

                pd.PrintQueue = targetQueue;
                pd.PrintTicket.PageMediaSize = new PageMediaSize(216, 120);

                // Store original scale
                Transform originalScale = e.LayoutTransform;

                // Get selected printer capabilities
                PrintCapabilities capabilities = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);

                // Get scale of the print wrt to screen of WPF visual
                double scale = Math.Min(
                    capabilities.PageImageableArea.ExtentWidth / e.ActualWidth,
                    capabilities.PageImageableArea.ExtentHeight / e.ActualHeight);

                // Transform the Visual to scale
                e.LayoutTransform = new ScaleTransform(scale, scale);

                // Get the size of the printer page
                System.Windows.Size sz = new System.Windows.Size(
                    capabilities.PageImageableArea.ExtentWidth,
                    capabilities.PageImageableArea.ExtentHeight);

                // Update the layout of the visual to the printer page size
                e.Measure(sz);
                e.Arrange(new System.Windows.Rect(
                    new System.Windows.Point(
                        capabilities.PageImageableArea.OriginWidth,
                        capabilities.PageImageableArea.OriginHeight),
                    sz));

                // Print the visual
                pd.PrintVisual(grid, "My Print");

                // Restore original transform
                e.LayoutTransform = originalScale;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing process returned an error: " + ex.Message);
            }
        }

        public static string formatString(string str)
        {
            // Replaces spaces with non-breaking spaces and periods with zero-width spaces
            str = str.Replace(" ", "\u00a0").Replace(".", "\u200B");
            return str;
        }
    }
}
