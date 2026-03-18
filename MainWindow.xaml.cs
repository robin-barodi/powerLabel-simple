using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace powerLabel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void scanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ComputerSystem.GetSystem();
                leftPanelGrid.Visibility = Visibility.Visible;
                drawLabelPreview();
                printBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during scan: " + ex.Message);
            }
        }

        private void printBtn_Click(object sender, RoutedEventArgs e)
        {
            LabelPrinting.printLabel(labelGrid);
        }

        private void drawLabelPreview()
        {
            labelText.Text = LabelPrinting.formatString(ComputerSystem.system.getString());
            boldCheckbox_Changed(new object(), new RoutedEventArgs());
        }

        private void boldCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (boldCheckbox.IsChecked == true)
                labelText.FontWeight = FontWeights.Bold;
            else
                labelText.FontWeight = FontWeights.Normal;
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settWindow = new SettingsWindow();
            settWindow.ShowDialog();
        }
    }
}