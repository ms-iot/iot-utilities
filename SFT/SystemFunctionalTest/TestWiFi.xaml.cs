//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Windows.System.Display;

using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Devices.Enumeration;
using Windows.Networking.Connectivity;
using System.Xml.Linq;
using System.Xml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SystemFunctionalTest
{
    public class Config_TestWifi : MenuItemConfigBase
    {
        #region Config: Main Context
        public const uint MAX_SIGSTRENGTH_THRESHOLDCOUNT = 10;
        public const uint DEFAULT_SIGSTRENGTH_THRESHOLDCOUNT = 0; // no threshold
        public const uint MAX_SIGSTRENTH_SIGBAR = 5;
        public const uint MAX_SIGSTRENTH_SIGQUALITY = 100;
        // valid value from 2 to 10, default is 2
        public uint SigStrength_ThresholdCount { get; set; }
        public uint SigStrength_ThresholdSigBar { get; set; }
        public uint SigStrength_ThresholdSigQuality { get; set; } // 100%: -50dBm, 0%: -100dBm 

        public Config_TestWifi()
        {
            this.Reset();
        }

        public new void Reset()
        {
            base.Reset();
            SigStrength_ThresholdCount = DEFAULT_SIGSTRENGTH_THRESHOLDCOUNT;
            SigStrength_ThresholdSigBar = 0;
            SigStrength_ThresholdSigQuality = 0;
        }
        #endregion // Config: Main Context

        #region Config: Basic Defines
        // <MenuItem Name="WiFi">
        //    <Threshold SigalBar="3" Count="1" /> 
        // </MenuItem>
        public static Config_TestWifi LoadFromXml(XElement elMenu)
        {
            Config_TestWifi config = new Config_TestWifi();
            if (elMenu == null) return config;

            LoadFromXml(elMenu, config);
            foreach (XNode node in elMenu.Nodes())
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement elMenuItem = node as XElement;
                    if (elMenuItem == null) continue;

                    if (elMenuItem.Name == "Threshold") // only one is allowed
                    {
                        foreach (XAttribute attr in elMenuItem.Attributes())
                        {
                            if (attr.Name.LocalName == "SignalBar")
                            {
                                uint value = 0;
                                if ((attr.Value != null) &&
                                    uint.TryParse(attr.Value, out value))
                                {
                                    if (value > MAX_SIGSTRENTH_SIGBAR) value = MAX_SIGSTRENTH_SIGBAR; // UI presentation
                                    config.SigStrength_ThresholdSigBar = value;
                                }
                            }
                            else if (attr.Name.LocalName == "SignalQuality")
                            {
                                uint value = 0;
                                if ((attr.Value != null) &&
                                    uint.TryParse(attr.Value, out value))
                                {
                                    if (value > MAX_SIGSTRENTH_SIGQUALITY) value = MAX_SIGSTRENTH_SIGQUALITY; // in percentage
                                    config.SigStrength_ThresholdSigQuality = value;
                                }
                            }
                            else if (attr.Name.LocalName == "Count")
                            {
                                uint value = 0;
                                if ((attr.Value != null) &&
                                    uint.TryParse(attr.Value, out value))
                                {
                                    if (value > MAX_SIGSTRENGTH_THRESHOLDCOUNT) value = MAX_SIGSTRENGTH_THRESHOLDCOUNT; // in percentage
                                    config.SigStrength_ThresholdCount = value;
                                }
                            }
                        }
                    }
                }
            }
            return config;
        }

        #endregion // Config: Basic Defines
    }


    /// <summary>
    /// class Customer is used to store Wifi scan result
    /// </summary>
    public class Customer
    {
        public String Ssid { get; set; }
        public String SecurityEnabled { get; set; }
        public String SignalStrength { get; set; }

        /// <summary>
        /// Constructor for class Customer
        /// </summary>
        public Customer(String ssid, String securityEnabled, String signalStrength)
        {
            this.Ssid = ssid;
            this.SecurityEnabled = securityEnabled;
            this.SignalStrength = signalStrength;
        }
    }

    public sealed partial class TestWiFi : Page
    {
        private Config_TestWifi curConfig = null;
        private const uint _signalStrengthDelta = 25;
        private const string errorInvalidState = "0x8007139f";
        private const string errorSize = "GetAvailableNetworkList(), size == 0";
        private bool isStarted = false;

        private List<WiFiAdapter> _deviceList = new List<WiFiAdapter>();
        private bool _started = false;
        private Windows.Devices.Enumeration.DeviceWatcher _watcher = null;
        private bool hasAutoPassResult = false;

        private bool isConnection = (String.IsNullOrEmpty(App.WifiNameConnection)) ? true : false;
        private bool isEnter = false;

        //Mark 2.4G and 5G APs in the list
        private static string[] PhyTypes = new string[]
        { "Unknown/Any" , //0
                    "2.4GHz; ", //1 , 802.11_FHSS 2.4GHz
                    "2.4GHz; ", //2 , 802.11_DHSS 2.4GHz
                    "Infrared (IR) baseband; ", //3 , 
                    "5GHz; ", //4 , 802.11a_OFDM 5GHz
                    "2.4GHz; ", //5 , 802.11b_HRDSSS 2.4GHz
                    "2.4GHz; ", //6 , 802.11g_ERP 2.4GHz
                    "2.4 or 5GHz; ", //7 , 802.11n_HT 2.4 or 5GHz
                    "5GHz; " //8 , 802.11ac_VHT 5GHz
        };

        /// <summary>
        /// Constructor of class TestWiFi
        /// </summary>
        public TestWiFi()
        {
            this.InitializeComponent();
            curConfig = App.ConfigTestWifi;
            txtTitle.Text = String.Format(CultureInfo.CurrentCulture, App.LoadString("Test"), App.LoadString("Wifi"));
        }

        #region Override methods

        /// <summary>
        /// Called when a page becomes the active page in a frame
        /// </summary>
        /// <param name="e">An object that contains the event data</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Activates a display request
            DisplayRequest displayRequest = new DisplayRequest();
            displayRequest.RequestActive();

            isStarted = true;

            App.LogStart(this.Name);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                InitializeWiFi();
            });
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame
        /// </summary>
        /// <param name="e">An object that contains the event data</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (isStarted)
            {
                isStarted = false;

                // Release resources used for WiFi Test 
                ReleaseWiFi();

                App.LogFinish(this.Name);
            }
            base.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// Initialize WiFi Test
        /// </summary>
        private async void InitializeWiFi()
        {
            txtStatus.Text = App.LoadString("Wifi") + " " + App.LoadString("Searching");
            txtStatus.Visibility = Visibility.Visible;
            hasAutoPassResult = false;
            await StartAsync();
        }

        /// <summary>
        /// Release resources used for WiFi Test
        /// </summary>
        private void ReleaseWiFi()
        {
            // Stop the watcher 
            if (_watcher != null)
            {
                if (_watcher.Status == DeviceWatcherStatus.Started)
                {
                    _watcher.Stop();
                }
                _watcher.Added -= _watcher_Added;
                _watcher.Removed -= _watcher_Removed;
            }

            if (_deviceList != null)
            {
                foreach (var device in _deviceList)
                {
                    device.AvailableNetworksChanged -= device_AvailableNetworksChanged;
                }
                _deviceList.Clear();
            }

            listWiFi.Items.Clear();

            _started = false;
        }

        public async Task<bool> StartAsync()
        {
            // only one start allowed on the object 
            lock (this)
            {
                if (_started) return false;

                _started = true;
            }

            if (await WiFiAdapter.RequestAccessAsync() != WiFiAccessStatus.Allowed)
              return false;

            _started = true;

            // enumerate and monitor WiFi devices in the system 
            string deviceSelector = WiFiAdapter.GetDeviceSelector();

            _watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(deviceSelector);
            _watcher.Added += _watcher_Added;
            _watcher.Removed += _watcher_Removed;

            _watcher.Start();

            return true;
        }

        async void _watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            WiFiAdapter device = await WiFiAdapter.FromIdAsync(args.Id);

            if (device != null)
            {
                // remove the device from the list 
                lock (this)
                {
                    _deviceList.Remove(device);
                }
            }
        }

        async void _watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            WiFiAdapter device = await WiFiAdapter.FromIdAsync(args.Id);

            if (device != null)
            {
                // add the device to the list (this will keep the object around 
                lock (this)
                {
                    _deviceList.Add(device);
                }

                // register for changes 
                device.AvailableNetworksChanged += device_AvailableNetworksChanged;

                // issue a scan 
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await device.ScanAsync().AsTask();
                });
            }
        }

        async void device_AvailableNetworksChanged(WiFiAdapter sender, object args)
        {
            if (isEnter) return;
            isEnter = true;

            if (hasAutoPassResult) return;
            WiFiNetworkReport networkReport = sender.NetworkReport;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                txtStatus.Visibility = Visibility.Collapsed;

                listWiFi.Items.Clear();

                int remainingCount = App.WifiName.Count;
                //uint sigstrengthPassCnt = 0;

                //look for the requested network 
                foreach (WiFiAvailableNetwork network in networkReport.AvailableNetworks)
                {
                    // Remove duplicated SSID
                    String strSsid = network.Ssid;
                    if (!String.IsNullOrEmpty(strSsid))
                    {
                        foreach (Customer item in listWiFi.Items)
                        {
                            if (item.Ssid.ToUpper() == network.Ssid.ToUpper())
                            {
                                strSsid = String.Empty;
                                break;
                            }
                        }
                    }

                    if (strSsid == string.Empty) continue;

                    // Try to connect network if <ConnectionName> setting exists in SFTConfig.xml
                    if (!isConnection && (network.Ssid.ToUpper() == App.WifiNameConnection.ToUpper()))
                    {
                        var connectioResult = await sender.ConnectAsync(network, WiFiReconnectionKind.Automatic);

                        if (connectioResult.ConnectionStatus == WiFiConnectionStatus.Success)
                        {
                            isConnection = true;
                            App.LogComment(network.Ssid + " : Connection OK!");
                            sender.Disconnect();
                        }
                    }

                    // Set signal strength image
                    string image = String.Format(CultureInfo.CurrentCulture, "/Assets/Wifi{0}",
                        (Application.Current.RequestedTheme == ApplicationTheme.Dark) ? "Dark" : "Light");

                    int signalStrength = network.SignalBars;
                    if (signalStrength > 4) signalStrength = 4;
                    if (signalStrength < 1) signalStrength = 1;
                    signalStrength--;
                    image += signalStrength.ToString(CultureInfo.CurrentCulture) + ".png";

                    // Set Authentication Type
                    String authType = App.LoadString("Secure");
                    switch (network.SecuritySettings.NetworkAuthenticationType)
                    {
                        case NetworkAuthenticationType.Open80211:
                            authType = App.LoadString("Open");
                            break;

                        case NetworkAuthenticationType.None:
                            authType = App.LoadString("Open");
                            break;

                        case NetworkAuthenticationType.Unknown:
                            authType = App.LoadString("Open");
                            break;
                    }

                    // convert from rssi to quality: https://msdn.microsoft.com/en-us/library/windows/desktop/ms706828(v=vs.85).aspx
                    int quality = (Math.Min(Math.Max((int)network.NetworkRssiInDecibelMilliwatts, -100), -50) + 100) * 2;
                    // mark 2.4G and 5G APs in the list
                    string details = authType + ", " + App.LoadString("Signal") + " " + quality + "%" + ", " + PhyTypes[(int) network.PhyKind];
                    // Add a scanned network to list box
                    listWiFi.Items.Add(new Customer(network.Ssid, details, image));
                    // App.LogComment("Available network: " + network.Ssid + ", " + authType + ", Bars=" + network.SignalBars.ToString(CultureInfo.CurrentCulture));
                    App.LogComment("Available network: " + network.Ssid + ", " + details + ", Bars=" + network.SignalBars.ToString(CultureInfo.CurrentCulture));

                    // Auto pass: check how many AP passed signal strength threshold value
                    //if (curConfig.SigStrength_ThresholdCount > 0)
                    //{
                    //    if (network.SignalBars >= curConfig.SigStrength_ThresholdSigBar)
                    //        sigstrengthPassCnt++;
                    //}

                    // Auto pass if WiFi test meet auto pass critera: found all WiFi predefined name list
                    if (remainingCount > 0)
                    {
                        bool isAboveThreshold = false;
                        int qualityInPercentage = 0;
                        if (curConfig.SigStrength_ThresholdSigQuality > 0)
                        {
                            qualityInPercentage = (Math.Min(Math.Max((int)network.NetworkRssiInDecibelMilliwatts, -100), -50) + 100) * 2;
                            if (App.WifiName.Contains(network.Ssid) && qualityInPercentage >= curConfig.SigStrength_ThresholdSigQuality)
                            {
                                isAboveThreshold = true;
                            }
                        }
                        else if (curConfig.SigStrength_ThresholdSigBar > 0)
                        {
                            if (App.WifiName.Contains(network.Ssid) && network.SignalBars >= curConfig.SigStrength_ThresholdSigBar)
                            {
                                remainingCount--;
                            }
                        }
                        if (isAboveThreshold)
                        {
                            remainingCount--;
                            App.LogComment(network.Ssid + " Found; Rssi=" + network.NetworkRssiInDecibelMilliwatts + "dBm(%" + qualityInPercentage + "), SigBar=" + network.SignalBars + "; PhyType=" + network.PhyKind);
                        }
                    }
                }

                // Enter Auto-Pass/Auto-Faill condition if one of <AvailableName>, <ConnectionName>, or <Threshold> setting exists in SFTConfig.xml
                if (App.WifiName.Count > 0 || !String.IsNullOrEmpty(App.WifiNameConnection)
                    // || curConfig.SigStrength_ThresholdCount > 0
                    )
                {
                    hasAutoPassResult = true;
                    // App.LogComment("Require " + curConfig.SigStrength_ThresholdCount + " AP that has SignalBar >= " + curConfig.SigStrength_ThresholdSigBar + ": Found " + sigstrengthPassCnt);
                    if (remainingCount == 0 && isConnection) // && sigstrengthPassCnt >= curConfig.SigStrength_ThresholdCount)
                    {
                        App.ResultControl("btnPass", this.Name);
                    }
                    else
                    {
                        App.ResultControl("btnFail", this.Name);
                    }
                }

                isEnter = false;
            });
        }

        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Handle Pass/Fail/Retry test result conditions.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void Result_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            if (App.ResultControl(button.Name, this.Name)) return;

            if (button.Name == "btnReset")
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ReleaseWiFi();
                    InitializeWiFi();
                });
            }
        }
    }
}

