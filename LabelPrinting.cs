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

                string printerShareName = "labelPrinter";

                PrintDialog pd = new PrintDialog();

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
                    MessageBox.Show($"Printer '{printerShareName}' not found on this machine.");
                    return;
                }

                pd.PrintQueue = targetQueue;
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
