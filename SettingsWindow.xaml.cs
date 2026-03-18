using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoUpdaterDotNET;

namespace powerLabel
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            refreshSettings();
        }

        private void refreshSettings()
        {
            SettingsHandler.Settings settings = SettingsHandler.ReadSettings();
            employeeList.Items.Clear();
            foreach (string item in settings.employees)
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Content = item;
                employeeList.Items.Add(listViewItem);
            }

            printerShareName.Text = settings.printerShareName;
        }

        private void addEmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (employeeTextbox.Text.Trim() != "")
            {
                ListViewItem employee = new ListViewItem();
                employee.Content = employeeTextbox.Text.Trim();
                employeeList.Items.Add(employee);
                employeeTextbox.Text = "";
            }
        }

        private void saveSettings(object sender, RoutedEventArgs e)
        {
            SettingsHandler.Settings sett = new SettingsHandler.Settings();

            List<string> empList = new List<string>();
            foreach (ListViewItem item in employeeList.Items)
            {
                empList.Add(item.Content.ToString());
            }

            sett.employees = empList;
            sett.printerShareName = printerShareName.Text;

            SettingsHandler.WriteSettings(sett);

            DialogResult = true;
        }

        private void employeeTextbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                addEmployeeBtn_Click(sender, e);
            }
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            ListViewItem selected = (ListViewItem)employeeList.SelectedItem;
            employeeList.Items.Remove(selected);
            employeeTextbox.Text = "";
            employeeList.UnselectAll();
        }

        private void Employee_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (employeeList.SelectedItem != null)
            {
                ListViewItem selected = (ListViewItem)employeeList.SelectedItem;
                employeeTextbox.Text = selected.Content.ToString();
                deleteBtn.Visibility = Visibility.Visible;
            }
            else
            {
                deleteBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void printerSettings_TextChanged(object sender, TextChangedEventArgs e)
        {
            // No-op: local printer, no address preview needed
        }
    }
}
