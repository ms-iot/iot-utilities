using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;

namespace IoTCoreImageHelper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RefreshDriveList()
        {
            var drives = DriveInfo.GetRemovableDriveList();
            lstDrives.Items.Clear();
            if (drives.Count == 0)
            {
                tbStatus.Text = "No SD cards found. Please insert one and press the refresh button.";
                var item = new ListBoxItem();
                item.Content = "No SD cards found";
                lstDrives.Items.Add(item);
                lstDrives.SelectedIndex = -1;
                lstDrives.IsEnabled = false;
            }
            else
            {
                foreach (var drive in drives)
                {
                    tbStatus.Text = "";
                    var item = new ListBoxItem();
                    item.Content = String.Format("{0} {1} [{2}]", drive.DriveName, drive.SizeString, drive.Model);
                    item.Tag = drive;
                    lstDrives.Items.Add(item);
                    lstDrives.SelectedIndex = -1;
                    lstDrives.IsEnabled = true;
                }
            }
            SetEnableState();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDriveList();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDriveList();
        }

        private void ShowEraseWarning()
        {
            tbWarningMessageHeader.Text = "Erase Content?";
            tbWarningMessageText.Text = "Make sure you back up any files on your card before flashing. Flashing will erase anything previously stored on the card.";
            grdContinueCancel.Visibility = System.Windows.Visibility.Visible;
            grdOk.Visibility = System.Windows.Visibility.Hidden;

            grdMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowCompletedMessage()
        {
            tbWarningMessageHeader.Text = "Completed!";
            tbWarningMessageText.Text = "Windows IoT Core is now onto your SD card. Go ahead and plug it into your device.";
            grdContinueCancel.Visibility = System.Windows.Visibility.Hidden;
            grdOk.Visibility = System.Windows.Visibility.Visible;

            grdMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowErrorMessage(string msg)
        {
            tbWarningMessageHeader.Text = "Errors!";
            tbWarningMessageText.Text = msg;
            grdContinueCancel.Visibility = System.Windows.Visibility.Hidden;
            grdOk.Visibility = System.Windows.Visibility.Visible;

            grdMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowErrorMessage()
        {
            var msg = string.Format("There were some errors while loading Windows IoT Core onto your SD card. Please refer to the logs at '{0}' for more details.", 
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "DISM"));
            ShowErrorMessage(msg);
        }

        private void btnFlash_Click(object sender, RoutedEventArgs e)
        {
            DisableAll();
            tbStatus.Text = "";
            ShowEraseWarning();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            grdMessage.Visibility = System.Windows.Visibility.Hidden;

            tbStatus.Text = "Loading Windows IoT Core onto your SD card...";

            var ffuImage = txtFFUFilename.Text;
            var driveInfo = (DriveInfo)((ListBoxItem)lstDrives.SelectedItem).Tag;
            try
            {
                var res = Dism.FlashFFUImageToDrive(ffuImage, driveInfo);

                tbStatus.Text = "";
                if (res == 0)
                {
                    ShowCompletedMessage();
                }
                else
                {
                    ShowErrorMessage();
                }
            }
            catch (Exception ex)
            {
                tbStatus.Text = "";
                var msg = string.Format("There were some errors while loading Windows IoT Core onto your SD card ('{0}')", ex.Message);
                ShowErrorMessage(msg);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            grdMessage.Visibility = System.Windows.Visibility.Hidden;
            SetEnableState();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            grdMessage.Visibility = System.Windows.Visibility.Hidden;
            SetEnableState();
        }

        private void lstDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbStatus.Text = "";
            SetEnableState();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            tbStatus.Text = "";
            var dlg = new OpenFileDialog();
            if (Properties.Settings.Default.FirstBrowse)
            {
                var current_dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                dlg.InitialDirectory = System.IO.Path.Combine(current_dir, @"FFU");
            }
            dlg.DefaultExt = ".ffu";
            dlg.Filter = "FFU images|*.ffu";
            dlg.CheckFileExists = true;

            var res = dlg.ShowDialog();

            if (res == true)
            {
                if (Properties.Settings.Default.FirstBrowse)
                {
                    Properties.Settings.Default.FirstBrowse = false;
                    Properties.Settings.Default.Save();
                }
                txtFFUFilename.Text = dlg.FileName;
            }

            SetEnableState();
        }

        private void SetEnableState()
        {
            lstDrives.IsEnabled = true;
            btnRefresh.IsEnabled = true;
            var driveSelected = (lstDrives.SelectedIndex != -1);
            grdSelectImage.IsEnabled = driveSelected;
            btnFlash.IsEnabled = driveSelected && File.Exists(txtFFUFilename.Text);
        }

        private void DisableAll()
        {
            lstDrives.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            grdSelectImage.IsEnabled = false;
            btnFlash.IsEnabled = false;
        }

    }
}
