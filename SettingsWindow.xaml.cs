using System.Windows;

namespace powerLabel
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            printerTextbox.Text = SettingsHandler.ReadSettings().printerShareName;
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            var sett = new SettingsHandler.Settings();
            sett.printerShareName = printerTextbox.Text;
            SettingsHandler.WriteSettings(sett);
            DialogResult = true;
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}