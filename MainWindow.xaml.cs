using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace powerLabel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AutoUpdater.Mandatory = true;
            AutoUpdater.UpdateMode = Mode.Forced;
            AutoUpdater.Start("https://github.com/ItsSiem/powerLabel/releases/latest/download/versions.xml");
            InitializeComponent();
        }

        private void scanSystem(object sender, RoutedEventArgs e)
        {
            ComputerSystem.GetSystem();
            ComputerSystem system = ComputerSystem.system;

            // Model Label string processing
            string modelString = system.motherboard.model;
            modelString = ComputerSystem.getShortString(modelString, new string[] {
                @"(?<ZLine>HP Z\w+)(?:(?!G)[a-zA-Z ])*(?<screensize>\d{2}\w?)?(?:(?![G])[A-Za-z .\d])*(?<generation>G\d)?",
                @"Precision \w* \w*"
            });

            // CPU Label string processing
            string cpuString = system.processor.name;
            cpuString = ComputerSystem.getShortString(cpuString, new string[] {
                @"(Platinum|Gold|Silver|Bronze)(?: )(\w*-*\d+\w*)",
                @"(\w+-*\d{3,}\w*)(?: )*(v\d)*",
            });
            if (system.processorAmount > 1)
            {
                cpuString = cpuString.Insert(0, "2x ");
            }

            // RAM Label string processing
            string ramString = system.memoryModules.Sum(item => Convert.ToInt64(item.module.capacity)) / 1073741824
                + "GB " + MemoryModule.memoryTypeLookup[system.memoryModules.First().module.memoryType];

            // Disk Label string processing
            string diskString = "";
            List<string> disks = new List<string>();
            List<string> doneDisks = new List<string>();
            foreach (DiskConfig disk in system.diskConfigs)
            {
                disks.Add(disk.ToString());
            }
            foreach (string disk in disks)
            {
                if (!doneDisks.Any(a => a == disk))
                {
                    if (disks.Where(a => a == disk).Count() > 1)
                    {
                        int multiplier = disks.Where(a => a == disk).Count();
                        diskString += $"{multiplier}x {disk}\r\n";
                        doneDisks.Add(disk);
                    }
                    else
                    {
                        diskString += disk + "\r\n";
                        doneDisks.Add(disk);
                    }
                }
            }

            // GPU Label string processing
            string gpuString = "";
            foreach (VideoControllerConfig gpu in system.videoControllerConfigs)
            {
                gpuString += ComputerSystem.getShortString(gpu.videoController.name, new string[] {
                    @"\w{2,3} Graphics \w+",
                    @"(Quadro|RTX) *(\w+) ?(\d+)?",
                    @"(GeForce) (\wTX?) (\d{3,})(?:[A-Za-z ]*)",
                    @"(Radeon) (Pro|RX)? ?(\w+)",
                }) + "\r\n";
            }

            system.SetLabelStrings(modelString, cpuString, ramString, diskString, gpuString);
        }

        private void scanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                scanSystem(sender, e);
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
            {
                labelText.FontWeight = FontWeights.Bold;
            }
            else
            {
                labelText.FontWeight = FontWeights.Normal;
            }
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settWindow = new SettingsWindow();
            settWindow.ShowDialog();
        }

        private void partSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            componentGrid.Children.Clear();
            componentGrid.RowDefinitions.Clear();
            componentListBox.Items.Clear();

            if (ComputerSystem.system == null)
                return;

            object component = null;
            switch (partSelector.SelectedIndex)
            {
                case 0: component = ComputerSystem.system.motherboard; break;
                case 1: component = ComputerSystem.system.bios; break;
                case 2: component = ComputerSystem.system.processor; break;
                case 3: component = ComputerSystem.system.memoryModules; break;
                case 4: component = ComputerSystem.system.diskConfigs; break;
                case 5: component = ComputerSystem.system.videoControllerConfigs; break;
                case 6: component = ComputerSystem.system.operatingSystem; break;
                default: break;
            }

            if (component == null)
                return;

            var enumerable = component as System.Collections.IList;
            if (enumerable == null)
            {
                // Single object — wrap it
                enumerable = new List<object> { component };
            }

            foreach (var item in enumerable)
            {
                ListBoxItem lbItem = new ListBoxItem();
                lbItem.Content = item.GetType().GetProperty("name")?.GetValue(item)
                              ?? item.GetType().GetProperty("model")?.GetValue(item)
                              ?? item.ToString();
                componentListBox.Items.Add(lbItem);
            }
        }

        private void componentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = componentListBox.SelectedIndex;
            if (selectedIndex < 0 || ComputerSystem.system == null)
                return;

            componentGrid.Children.Clear();
            componentGrid.RowDefinitions.Clear();

            object component = null;
            switch (partSelector.SelectedIndex)
            {
                case 0: component = ComputerSystem.system.motherboard; break;
                case 1: component = ComputerSystem.system.bios; break;
                case 2: component = ComputerSystem.system.processor; break;
                case 3: component = ComputerSystem.system.memoryModules; break;
                case 4: component = ComputerSystem.system.diskConfigs; break;
                case 5: component = ComputerSystem.system.videoControllerConfigs; break;
                case 6: component = ComputerSystem.system.operatingSystem; break;
                default: break;
            }

            if (component == null)
                return;

            var enumerable = component as System.Collections.IList;
            if (enumerable == null)
            {
                enumerable = new List<object> { component };
            }

            int propertyIndex = 0;
            foreach (PropertyInfo propertyInfo in enumerable[selectedIndex].GetType().GetProperties())
            {
                componentGrid.RowDefinitions.Add(new RowDefinition());
                Label label = new Label();
                label.Content = propertyInfo.Name;
                componentGrid.Children.Add(label);
                Grid.SetRow(label, propertyIndex);
                Grid.SetColumn(label, 0);

                Label value = new Label();
                value.Content = propertyInfo.GetValue(enumerable[selectedIndex]);
                componentGrid.Children.Add(value);
                Grid.SetRow(value, propertyIndex);
                Grid.SetColumn(value, 1);

                propertyIndex++;
            }
        }
    }
}
