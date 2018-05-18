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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Devices.Radios;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=402347&clcid=0x409

namespace SystemFunctionalTest
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        #region Properties

        public static Mutex appStateMutex = new Mutex();

        public static Frame RootFrame { get; private set; }

        private bool ShouldRestrictedActivation = false;

        // Auto/Manul mode setting
        public static bool IsAuto { get; set; }
        public static bool IsAutoStopAtFail { get; private set; }

        public static bool IsModeHeadLess { get; private set; }

        // User Preferred Language settings
        public static string UserLanguage { get; private set; }


        // User Preferred Log Option settings :
        // -1: not set; 0: disable; 1:enable
        public static Int16 UserLogOption { get; private set; }

        // Phase settings
        public static Collection<string> PhaseName { get; private set; }
        public static UInt16 PhaseCount { get; private set; }

        // Main menu settings
        public static Collection<string> MainMenuName { get; private set; }
        public static UInt16 MainMenuCount { get; private set; }

        // Test menu settings
        public static Collection<string> TestName { get; private set; }
        public static UInt16 TestCount { get; private set; }

        // Wifi test page settings
        public static Collection<string> WifiName { get; private set; }
        public static String WifiNameConnection { get; private set; }

        public static Config_TestWifi ConfigTestWifi { get; private set; }

        // All main menu item and test items names
        public static Dictionary<string, string> MenuItemName { get; private set; }

        // Test State and Test Result key-value pairs
        public static uint TestStateValue { get; set; }   // for each bit: 1 -> Test, 0 -> Untest
        public static uint TestResultValue { get; set; }  // for each bit: 1 -> Pass, 0 -> Fail

        #endregion // Properties

        #region Fields

        // Max retry count for each test item
        private const UInt16 MAX_RETRY_COUNT = 3;

        // Customization settings
        private const String TestSettingsFile = "SFTConfig.xml";
        private const String TestSettingsOemFolder = @"c:\Programs\CommonFiles\OEM\Public\";
        private const UInt16 MAX_PHASE_COUNT = 6;  // Index start from 0
        private const UInt16 MAX_MENU_COUNT = 6;   // Index start from 0
        private const UInt16 MAX_TEST_COUNT = 31;  // Index start from 0
        private const UInt16 MAX_WIFINAME_COUNT = 10; // Index start from 0

        // Test State and Test Result registry key name
        private const string TestStateKeyName = "TestState";
        private const string TestResultKeyName = "TestResult";

        // Test State and Test Result registry key name
        private static string TestStateKey = "TestState";
        private static string TestResultKey = "TestResult";

        // Each test item max reset count = 3, if reset count > 3 then return fail automatically
        private static UInt32 PrevTestItem = 0;
        private static UInt16 ResetCount = 0;

#if !DISABLE_LOGGING
        private static StorageFile storageFile;
        private static string _log = string.Empty;
        // Configure a public folder to store log file, in which folder user may access via MTP connection 
        private static StorageFolder LogFolder = null;
        private static bool LogIsEnabled = true;
#endif
        private static bool IsWiFiConntable = false;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;

            // Initialize all settings for menu items and test items in MainPage.xaml and MenuPage.xaml
            InitializeMenuSettings();

            // Log folder
            InitializeLog();
        }

        #endregion // Constructor

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            App.LogStart("_App");
            App.LogComment("App Launched!");
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            RootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new Frame();
                // Set the default language
                RootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                RootFrame.NavigationFailed += OnNavigationFailed;

                if (e != null && e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = RootFrame;
            }

            // Support different font size settings used for desktop and mobile platform seperately
            InitializeFontSize();

            if (RootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (e != null)
                    RootFrame.Navigate(typeof(PhasePage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            Windows.UI.ViewManagement.ApplicationView.TerminateAppOnFinalViewClose = true;
        }


        /// <summary> 
        // Handle protocol activations and continuation activations. 
        /// </summary> 
        protected override void OnActivated(IActivatedEventArgs e)
        {
            if (e.Kind == ActivationKind.Protocol) 
            {
                ProtocolActivatedEventArgs protocolArgs = e as ProtocolActivatedEventArgs;

                if (!ShouldRestrictedActivation)
                {
                    App.LogStart("_App");
                    App.LogComment("App Activated by " + protocolArgs.CallerPackageFamilyName + " !");

                    RootFrame = Window.Current.Content as Frame;

                    // Do not repeat app initialization when the Window already has content, 
                    // just ensure that the window is active 
                    if (RootFrame == null)
                    {
                        // Create a Frame to act as the navigation context and navigate to the first page 
                        RootFrame = new Frame();

                        // Set the default language 
                        RootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                        RootFrame.NavigationFailed += OnNavigationFailed;

                        // Place the frame in the current Window 
                        Window.Current.Content = RootFrame;


                        // Support different font size settings used for desktop and mobile platform seperately
                        InitializeFontSize();

                        if (RootFrame.Content == null)
                        {
                            // When the navigation stack isn't restored navigate to the first page,
                            // configuring the new page by passing required information as a navigation
                            // parameter
                            if (e != null)
                                RootFrame.Navigate(typeof(PhasePage), protocolArgs.Uri);
                        }
                    }

                    // Ensure the current window is active
                    Window.Current.Activate();

                    // Set orientation to fit to native orientation
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                    Windows.UI.ViewManagement.ApplicationView.TerminateAppOnFinalViewClose = true;
                }
                else
                {
                    if (e.PreviousExecutionState == ApplicationExecutionState.NotRunning || e.PreviousExecutionState == ApplicationExecutionState.Terminated || e.PreviousExecutionState == ApplicationExecutionState.ClosedByUser)
                    {
                        Application.Current.Exit();
                    }
                }
            } 
        } 


        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            //TODO: Save application state and stop any background activity
            SaveTestResultSettings();

            await ReleaseEarlyStartResource();
            App.LogComment("Suspending from page : " + RootFrame.CurrentSourcePageType.ToString());
            App.LogComment("App Suspended!");
            App.LogFinish("_App");

            deferral.Complete();
        }

        private async void OnResuming(object sender, object e)
        {
            App.LogStart("_App");
            App.LogComment("App Resumed!");
            await AquireEarlyStartResource();
        }



        public static async void ExitApp()
        {
            SaveTestResultSettings();
            await ReleaseEarlyStartResource();
            App.LogComment("Application Exit!");
            App.LogFinish("_App");
            await Task.Delay(500);
            Application.Current.Exit();
        }

        public static async Task AquireEarlyStartResource()
        {
            //App.LogComment("AquireEarlyStartResource...");
            appStateMutex.WaitOne();
            try
            {
                int radioFlag = 0;
                if (App.ConfigTestWifi != null) radioFlag |= 0x01;
                await EnableRadio(radioFlag);
            }
            catch
            { }
            //App.LogComment("AquireEarlyStartResource OK!");
            appStateMutex.ReleaseMutex();
        }
        private static async Task<bool> ReleaseEarlyStartResource()
        {
            appStateMutex.WaitOne();
            //App.LogComment("ReleaseEarlyStartResource...");
            try
            {
                // Restore Bluetooth and WiFi oritinal state
                if (!IsWiFiConntable)
                    await DisableRadio();
            }
            catch
            { }
            //App.LogComment("ReleaseEarlyStartResource Finished!");
            appStateMutex.ReleaseMutex();
            return true;
        }

        #region Localization
        /// <summary>
        /// Base on the current language setting to load localizated string. 
        /// </summary>
        public static String LoadString(String resString)
        {
            return ResourceLoader.GetForCurrentView().GetString(resString);
        }
        #endregion

        #region Log

        /// <summary>
        /// Initialization for log
        /// </summary>
        private static void InitializeLog()
        {
#if !DISABLE_LOGGING
            // set folder path to store log file
            LogFolder = KnownFolders.PicturesLibrary;
#endif 
        }

        /// <summary>
        /// Get application title
        /// </summary>
        private static string GetAppTitle()
        {
            return (Windows.ApplicationModel.Package.Current.DisplayName);
        }

        /// <summary>
        /// Log file
        /// </summary>
        /// <param name="message">Message to log</param>
        private static void Log(string message)
        {
#if !DISABLE_LOGGING
            if (!LogIsEnabled) return;
            _log += String.Format(CultureInfo.CurrentCulture, "{0}\t{1}\r\n",
                                  DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", CultureInfo.CurrentCulture), message);
#endif
        }

        /// <summary>
        /// Append Log
        /// </summary>
        private async static void LogAppend()
        {
#if !DISABLE_LOGGING
            if (!LogIsEnabled) return;
            if (storageFile == null && LogFolder != null)
            {
                storageFile = await LogFolder.CreateFileAsync(String.Format(CultureInfo.CurrentCulture, "{0}_{1}.dat", GetAppTitle(), TestStateKey), CreationCollisionOption.OpenIfExists);
            }

            if (storageFile != null)
            {
                try
                {
                    await FileIO.AppendTextAsync(storageFile, _log);
                    _log = string.Empty;
                }
                catch
                {
                }
            }
#endif
        }

        /// <summary>
        /// Log Pass Result
        /// </summary>
        /// <param name="message">Pass message</param>
        public static void LogPass(string message)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "PASS\t{0}", message));
#endif
        }

        /// <summary>
        /// Log Fail Result.
        /// </summary>
        /// <param name="message">Fail message</param>
        public static void LogFail(string message)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "FAIL\t{0}", message));
#endif
        }

        /// <summary>
        /// Log comment
        /// </summary>
        /// <param name="message">Comment message</param>
        public static void LogComment(string message)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "COMMENT\t{0}", message));
#endif
        }

        /// <summary>
        /// Log comment
        /// </summary>
        /// <param name="message">Comment message</param>
        public static void LogDebug(string message)
        {
            Debug.WriteLine(message);
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "DEBUG\t{0}", message));
            LogAppend();
#endif
        }


        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void LogError(string message)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "ERROR\t{0}", message));
#endif
        }

        /// <summary>
        /// Start to log
        /// </summary>
        /// <param name="testName">Test item name to log</param>
        public static void LogStart(string testName)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "START\t{0}", testName));
#endif
        }

        /// <summary>
        /// Finish to log
        /// </summary>
        /// <param name="testName">Test item name to log</param>
        public static void LogFinish(string testName)
        {
#if !DISABLE_LOGGING
            Log(String.Format(CultureInfo.CurrentCulture, "FINISH\t{0}", testName));
            LogAppend();
#endif
        }

        #endregion // Log

        

        #region MenuSettings

        /// <summary>
        /// Support different font size settings used for desktop and mobile platform seperately.
        /// </summary>
        private static void InitializeFontSize()
        {
            AddFontSize("MenuButtonHeight", "56", "66");
            AddFontSize("MenuFontSize", "16", "26");
            AddFontSize("FeedbackFontSize", "28", "36");

            AddFontSize("WifiFontSize", "26", "36");
            AddFontSize("WifiSubFontSize", "18", "22");
            AddFontSize("SimpleListMainFontSize", "26", "36");
            AddFontSize("SimpleListSubFontSize", "18", "22");

            AddFontSize("TitleFontSize", "32", "36");
            AddFontSize("MessageFontSize", "16", "20");
            AddFontSize("StatusFontSize", "22", "26");

            AddFontSize("InfoKeyFontSize", "16", "20");
            AddFontSize("InfoValueFontSize", "16", "20");

            AddFontSize("TestInfoKeyFontSize", "20", "24");
            AddFontSize("TestInfoValueFontSize", "20", "24");
        }

        /// <summary>
        /// Initialize different font size used for desktop and mobile seperately.
        /// </summary>
        /// <param name="fontsizeName">FontSize name used in <Application.Resources>.</param>
        /// <param name="mobileSize">Font size used for mobile platform.</param>
        /// <param name="desktopSize">Font size used for desktop platform.</param>
        private static void AddFontSize(String fontsizeName, String mobileSize, String desktopSize)
        {
            if (Current.Resources.ContainsKey(fontsizeName))
               Current.Resources.Remove(fontsizeName);
            Current.Resources.Add(fontsizeName, desktopSize);
        }

        /// <summary>
        /// Initialize main menu and test menu settings.
        /// </summary>
        private static void InitializeMenuSettings()
        {
            IsAuto = false;
            IsAutoStopAtFail = false;
            IsModeHeadLess = false;
            TestResultValue = TestStateValue = 0;

            UserLanguage = ""; // default language selection is decided by AppResource ( system language dependent )
            UserLogOption = -1; // user log option, initially not set

            PhaseName = new Collection<string>();
            PhaseCount = 0;

            MainMenuName = new Collection<string>();
            MainMenuCount = 0;

            TestName = new Collection<string>();
            TestCount = 0;

            WifiName = new Collection<string>();
            WifiNameConnection = String.Empty;

            // Initialize Test Item Display Name in MenuPage.xaml
            // InitializeMenuItemName();
        }

 
        /// <summary>
        /// Initialize all predefined main menu item names and test menu item names.
        /// </summary>
        private static void InitializeMenuItemName()
        {
            MenuItemName = new Dictionary<string, string>();

            // Menu item names in MenuPage.xaml
            MenuItemName.Add("WiFi", LoadString("Wifi"));
            MenuItemName.Add("Compass", LoadString("Compass"));
            MenuItemName.Add("Accelerometer", LoadString("Accelerometer"));


            // Menu item names in MainPage.xaml
            MenuItemName.Add("Auto", LoadString("Auto"));
            MenuItemName.Add("Manual", LoadString("Manual"));
            MenuItemName.Add("ClearResult", LoadString("ClearTestResult"));
        }

        /// <summary>
        /// Load default settings for main menu items and test menu items.
        /// </summary>
        private static void LoadDefaultMenuSettings()
        {
            if (MainMenuCount == 0)
            {
                MainMenuCount = MAX_MENU_COUNT;
                MainMenuName.Add("Auto");
                MainMenuName.Add("Manual");
                MainMenuName.Add("ClearResult");
            }

            if (TestCount == 0)
            {
                TestCount = MAX_TEST_COUNT;
                TestName.Add("WiFi");
                TestName.Add("Compass");
                TestName.Add("Accelerometer");
            }
        }

        /// <summary>
        /// Load test result flags from local registry.
        /// </summary>
        public static void LoadTestResultSettings(UInt16 indexPhase)
        {
            TestStateKey = PhaseName[indexPhase] + TestStateKeyName;
            TestResultKey = PhaseName[indexPhase] + TestResultKeyName;

            TestResultValue = TestStateValue = 0;

            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                Object value = localSettings.Values[TestStateKey];
                if (value == null)
                    localSettings.Values[TestStateKey] = TestStateValue.ToString(CultureInfo.CurrentCulture);
                else
                    TestStateValue = Convert.ToUInt32(value, CultureInfo.CurrentCulture);

                value = localSettings.Values[TestResultKey];
                if (value == null)
                    localSettings.Values[TestResultKey] = TestResultValue.ToString(CultureInfo.CurrentCulture);
                else
                    TestResultValue = Convert.ToUInt32(value, CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            { }
            catch (OverflowException)
            { }
        }

        /// <summary>
        /// Save test result flags to local registry.
        /// </summary>
        private static void SaveTestResultSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[TestStateKey] = TestStateValue.ToString(CultureInfo.CurrentCulture);
            localSettings.Values[TestResultKey] = TestResultValue.ToString(CultureInfo.CurrentCulture);
        }

        private static void SetAutoProperty(XElement elMenu)
        {

            foreach (XNode node in elMenu.Nodes())
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement elMenuItem = node as XElement;
                    if (elMenuItem == null) continue;

                    if (elMenuItem.Name == "Property")
                    {
                        foreach (XAttribute attr in elMenuItem.Attributes())
                        {
                            if (attr.Name.LocalName == "StopAtFail")
                            {
                                if (attr.Value.ToLower() == "true")
                                    IsAutoStopAtFail = true;
                                else if (attr.Value.ToLower() == "false")
                                    IsAutoStopAtFail = false;
                            }

                            if(attr.Name.LocalName == "IsHeadLess")
                            {
                                if (attr.Value.ToLower() == "true")
                                    IsModeHeadLess = true;
                                else if (attr.Value.ToLower() == "false")
                                    IsModeHeadLess = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read [WiFi] element's child [AvailableName] and [ConnectionName] from SFTConfig.xaml to set customized Wifi name list in TestWiFi.xaml.
        /// </summary>
        /// <param name="elMenu">[WiFi] element.</param>
        private static void SetWifiName(XElement elMenu)
        {
            WifiName.Clear();

            foreach (XNode node in elMenu.Nodes())
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement elMenuItem = node as XElement;
                    if (elMenuItem == null) continue;

                    if (elMenuItem.Name == "AvailableName")
                    {
                        // Get a predifined Wifi name
                        if ((elMenuItem.Value != null) &&
                            WifiName.Count < MAX_WIFINAME_COUNT)
                        {
                            // Skip duplicate Wifi name
                            if (WifiName.Contains(elMenuItem.Value)) continue;

                            WifiName.Add(elMenuItem.Value);
                        }
                    }

                    if (elMenuItem.Name == "ConnectionName")
                    {
                        // Get the first predifined Wifi connection name
                        if ((elMenuItem.Value != null) &&
                            (String.IsNullOrEmpty(WifiNameConnection)))
                        {
                            WifiNameConnection = elMenuItem.Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read [TestMenu] element's child [MenuItem] from SFTConfig.xaml to set customized test items in MenuPage.xaml.
        /// </summary>
        /// <param name="elMenu">[TestMenu] element.</param>
        private static void SetTestMenu(XElement elMenu)
        {
            string value = "";

            TestCount = 0;
            TestName.Clear();

            foreach (XNode node in elMenu.Nodes())
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement elMenuItem = node as XElement;
                    if (elMenuItem == null) continue;

                    if (elMenuItem.Name == "MenuItem")
                    {
                        foreach (XAttribute attr in elMenuItem.Attributes())
                        {
                            if (attr.Name.LocalName == "Name")
                            {
                                // Check if the input test item name is valid or not
                                if ((attr.Value != null) &&
                                    (App.MenuItemName.TryGetValue(attr.Value, out value)) &&
                                    TestCount < MAX_TEST_COUNT)
                                {
                                    // Check duplicate menu item name
                                    if (TestName.Contains(attr.Value)) break;

                                    // Add new menu item name and page
                                    TestName.Add(attr.Value);
                                    TestCount++;

                                    // Customize WiFi predefined name list
                                    if (attr.Value == "WiFi")
                                    {
                                        if (elMenuItem.HasElements) SetWifiName(elMenuItem);
                                        ConfigTestWifi = Config_TestWifi.LoadFromXml(elMenuItem);
                                    }

                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read [MainMenu] element's child [MenuItem] from SFTConfig.xaml to set customized main menu items in MainPage.xaml.
        /// </summary>
        /// <param name="elMenu">[MainMenu] element.</param>
        private static void SetMainMenu(XElement elMenu)
        {
            string value = "";

            MainMenuCount = 0;

            foreach (XNode node in elMenu.Nodes())
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement elMenuItem = node as XElement;
                    if (elMenuItem == null) continue;

                    if (elMenuItem.Name == "MenuItem")
                    {
                        foreach (XAttribute attr in elMenuItem.Attributes())
                        {
                            if (attr.Name.LocalName == "Name")
                            {
                                // Check if the input MainPage menu item name is valid or not
                                if ((attr.Value != null) &&
                                    (App.MenuItemName.TryGetValue(attr.Value, out value)) &&
                                    MainMenuCount < MAX_MENU_COUNT)
                                {
                                    // Check duplicate menu item name
                                    if (!MainMenuName.Contains(attr.Value))
                                    {
                                        MainMenuName.Add(attr.Value);
                                        MainMenuCount++;
                                    }
                                }

                                if (attr.Value == "Auto" && elMenuItem.HasElements)
                                {
                                    SetAutoProperty(elMenuItem);
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Based on specified phase to load customized main menu items and test menu items.
        /// </summary>
        /// <param name="indexPhase">Phase menu item index.</param>
        public static async Task LoadMenuSettings(UInt16 indexPhase)
        {
            string phaseName = String.Empty;
            Stream xmlStream = null;


            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from Picture folder next
                try
                {
                    var folder = KnownFolders.PicturesLibrary;
                    var file = await folder.GetFileAsync(TestSettingsFile);
                    xmlStream = await file.OpenStreamForReadAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from c:\Programs\CommonFiles\OEM\Public first
                try
                {
                    xmlStream = File.OpenRead(Path.Combine(TestSettingsOemFolder, TestSettingsFile));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from App's current folder
                try
                {
                    var file = await Package.Current.InstalledLocation.GetFileAsync(TestSettingsFile);
                    xmlStream = await file.OpenStreamForReadAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
                goto Exit;

            try
            {
                using (XmlReader reader = XmlReader.Create(xmlStream))
                {
                    if (reader == null) return;

                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "Phase")
                            {
                                XElement elPhase = XNode.ReadFrom(reader) as XElement;

                                if (elPhase == null) continue;

                                foreach (XAttribute attr in elPhase.Attributes())
                                {
                                    if (attr.Name.LocalName == "Name")
                                    {
                                        phaseName = attr.Value;
                                        break;
                                    }
                                }

                                if (phaseName != PhaseName[indexPhase]) continue;

                                foreach (XNode node in elPhase.Nodes())
                                {
                                    if (node.NodeType == XmlNodeType.Element)
                                    {
                                        XElement elMenu = node as XElement;
                                        if (elMenu == null) continue;

                                        if (elMenu.Name == "TestMenu" && elMenu.HasElements)
                                        {
                                            SetTestMenu(elMenu);
                                        }
                                        else if (elMenu.Name == "MainMenu" && elMenu.HasElements)
                                        {
                                            SetMainMenu(elMenu);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }

            Exit:

            LoadDefaultMenuSettings();
            LoadTestResultSettings(indexPhase);
        }

        /// <summary>
        /// Load customized phase names from SFTConfig.xml.
        /// </summary>
        public static async Task LoadPhase()
        {
            PhaseCount = 0;
            Stream xmlStream = null;

            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from Picture folder as first priority
                try
                {
                    var folder = KnownFolders.PicturesLibrary;
                    var file = await folder.GetFileAsync(TestSettingsFile);
                    xmlStream = await file.OpenStreamForReadAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from c:\Programs\CommonFiles\OEM\Public as second priority
                try
                {
                    // should use file IO API to access to OEM folder
                    xmlStream = File.OpenRead(Path.Combine(TestSettingsOemFolder, TestSettingsFile));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
            {
                // Try to get SFTConfig.xml from App's current folder as third priority
                try
                {
                    var file = await Package.Current.InstalledLocation.GetFileAsync(TestSettingsFile);
                    xmlStream = await file.OpenStreamForReadAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (xmlStream == null)
                goto Exit;

            try
            {
                using (XmlReader reader = XmlReader.Create(xmlStream))
                {
                    if (reader == null) return;

                    reader.MoveToContent();
                    while (PhaseCount < MAX_PHASE_COUNT && (reader.Read()))
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "Phase")
                            {
                                XElement elPhase = XNode.ReadFrom(reader) as XElement;

                                if (elPhase == null) continue;

                                foreach (XAttribute attr in elPhase.Attributes())
                                {
                                    if (attr.Name.LocalName == "Name")
                                    {
                                        PhaseName.Add(attr.Value);
                                        PhaseCount++;
                                        break;
                                    }
                                }
                            }
                            else if (reader.Name == "Language")
                            {
                                if (String.IsNullOrEmpty(UserLanguage))
                                {
                                    XElement elLang = XNode.ReadFrom(reader) as XElement;

                                    if (elLang == null) continue;

                                    foreach (XAttribute attr in elLang.Attributes())
                                    {
                                        if (attr.Name.LocalName == "Name")
                                        {
                                            UserLanguage = attr.Value;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (reader.Name == "Log")
                            {
                                if (UserLogOption == -1) // not set
                                {
                                    XElement elLogOpt = XNode.ReadFrom(reader) as XElement;

                                    if (elLogOpt == null) continue;

                                    foreach (XAttribute attr in elLogOpt.Attributes())
                                    {
                                        if (attr.Name.LocalName == "Enable")
                                        {
                                            if (attr.Value.ToLower() == "true")
                                                UserLogOption = 1;
                                            else if (attr.Value.ToLower() == "false")
                                                UserLogOption = 0;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (reader.Name == "AutoTest")
                            {
                                XElement elLogOpt = XNode.ReadFrom(reader) as XElement;

                                if (elLogOpt == null) continue;

                                foreach (XAttribute attr in elLogOpt.Attributes())
                                {
                                    if (attr.Name.LocalName == "StopAtFail")
                                    {
                                        if (attr.Value.ToLower() == "true")
                                            IsAutoStopAtFail = true;
                                        else if (attr.Value.ToLower() == "false")
                                            IsAutoStopAtFail = false;
                                    }
                                    else if (attr.Name.LocalName == "IsHeadLess")
                                    {
                                        if (attr.Value.ToLower() == "true")
                                            IsModeHeadLess = true;
                                        else if (attr.Value.ToLower() == "false")
                                            IsModeHeadLess = false;
                                    }
                                    break;
                                }
                            }
                           
                        }
                    }
                }
            }
            catch (Exception)
            { }

            Exit:

            if (!String.IsNullOrEmpty(App.UserLanguage))
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = App.UserLanguage;

                // fix issue: without the following code, for the first time launch of this app, 
                // some string will be in english, while some are in user-specified language.
                //
                // an workaround to ensure loading string resource from user specified language.
                var defaultContext = Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView();//ResourceManager.Current.DefaultContext;
                var topLangFullName = (new CultureInfo(defaultContext.Languages[0])).EnglishName;
                var userLangFullName = (new CultureInfo(App.UserLanguage)).EnglishName;
                for (int i=0; i<3 && (topLangFullName != userLangFullName);i++)
                {
                    await Task.Delay(200);
                    topLangFullName = (new CultureInfo(defaultContext.Languages[0])).EnglishName;
                }
            }

            // Initialize Test Item Display Name in MenuPage.xaml
            InitializeMenuItemName();

#if !DISABLE_LOGGING
            if (App.UserLogOption == 0)
                App.LogIsEnabled = false;
            else if (App.UserLogOption == 1)
                App.LogIsEnabled = true;
#endif

            // No <Phase> element found
            if (PhaseCount == 0)
            {
                PhaseName.Add("");
            }
        }
#endregion // MenuSettings

#region Wifi

        /// <summary>
        /// Turn on Bluetooth or WiFi.
        /// </summary>
        private static async Task<bool> EnableRadio(int radioFlag)
        {
            if (radioFlag == 0) return true;
            try
            {
                var radios = await Radio.GetRadiosAsync();
                if (radios.Count > 0)
                {
                    foreach (var radio in radios)
                    {
                        App.LogComment("[EnableRadio] Radio.Type: " +  radio.Kind.ToString() + ", State = " + radio.State.ToString());

                        if (((radioFlag & 0x1) != 0) && radio.Kind == RadioKind.WiFi)
                        {
                            if (radio.State == RadioState.Off)
                            {
                                await radio.SetStateAsync(RadioState.On);
                                App.LogComment("Wifi turned on! " + radio.State.ToString());
                            }
                            else
                            {
                                IsWiFiConntable = true;
                            }
                        }
                    }
                }
            }
            catch
            { }
            return true;
        }


        /// <summary>
        /// Turn off Bluetooth or WiFi.
        /// </summary>
        private static async Task<bool> DisableRadio()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();

                if (radios.Count > 0)
                {
                    foreach (var radio in radios)
                    {
                        App.LogComment("[DisableRadio] Radio.Type: " + radio.Kind.ToString() + ", State = " + radio.State.ToString());

                        if (radio.Kind == RadioKind.WiFi && (!IsWiFiConntable))
                        {
                            if (radio.State == RadioState.On)
                            {
                                await radio.SetStateAsync(RadioState.Off);
                                App.LogComment("Wifi turned off! " + radio.State.ToString());
                            }
                        }
                    }
                }
            }
            catch
            { }
            return true;
        }

#endregion // Bluetooth


#region TestResultControl

        /// <summary>
        /// Get index for the specified test item.
        /// </summary>
        /// <param name="Name">Specified test item's name.</param>
        private static uint GetTestIndex(String Name)
        {
            uint nIndex = TestCount;
            for (UInt16 i = 0; i < TestCount; i++)
            {
                if (Name.Substring(1) == TestName[i])
                {
                    nIndex = i;
                    break;
                }
            }

            return nIndex;
        }

        /// <summary>
        /// Get Type for the specified Test Page.
        /// </summary>
        /// 
        /// <param name="Name">Specified test item's name.</param>
        public static Type GetTestPageType(String Name)
        {
            Type classType = typeof(MainPage);

            switch (Name)
            {
                case "Accelerometer":
                    classType = typeof(TestAccelerometer);
                    break;

                case "Compass":
                    classType = typeof(TestCompass);
                    break;

                case "WiFi":
                    classType = typeof(TestWiFi);
                    break;

            }

            return classType;
        }

        /// <summary>
        /// Get parameter for the specified Test Page.
        /// </summary>
        /// 
        /// <param name="Name">Specified test item's name.</param>
        public static String GetTestPageParameter(String Name)
        {
            String strParameter = null;

            switch (Name)
            {
                default:
                    break;
            }

            return strParameter;
        }


        /// <summary>
        /// Handle Pass/Fail/Retry test result conditions.
        /// </summary>
        /// <param name="buttonName">Clicked Pass/Fail/Retry button.</param>
        /// <param name="testName">Current test item.</param>
        public static bool ResultControl(String buttonName, String testName)
        {
            bool isNavigatedTo = false; // a flag to indicate if result control requests to navigate to other page
            uint nTestItem = GetTestIndex(testName);

            if (nTestItem >= TestCount) return isNavigatedTo;  // unknown test item

            uint value = (uint)1 << (int)nTestItem;

            switch (buttonName)
            {
                case "btnPass":
                    // Write pass result
                    TestStateValue |= value;
                    TestResultValue |= value;

                    LogPass(testName);
                    break;

                case "btnFail":
                    // Write fail result
                    TestStateValue |= value;
                    TestResultValue &= ~value;

                    if (IsAutoStopAtFail)
                        IsAuto = false;
#if AUTO_BREAK_BY_FAIL
                    // For user want to break Auto mode operation while test fail
                    IsAuto = false;
#endif // AUTO_BREAK_BY_FAIL

                    LogFail(testName);
                    break;


                case "btnReset": // btnReset will stay at current test page and retry operation have to be handled by each test item
                    // if reset count > 3, set fail automatically and return back to MenuPage.xaml
                    if (PrevTestItem == nTestItem)
                    {
                        ResetCount++;
                    }
                    else
                    {
                        PrevTestItem = nTestItem;
                        ResetCount = 0;
                    }

                    if (ResetCount >= MAX_RETRY_COUNT)
                    {
                        // Reset PrevTestItem and ResetCount
                        PrevTestItem = 0;
                        ResetCount = 0;

                        // Write fail result
                        TestStateValue |= value;
                        TestResultValue &= ~value;
                        buttonName = "btnFail";
                    }
                    else
                    {
                        // Reset TestState and TestResult values
                        TestStateValue &= ~value;
                        TestResultValue &= ~value;
                    }
                    break;
            }

            if (buttonName != "btnReset")
            {
                // Save test result to registry
                SaveTestResultSettings();

                // Reset PrevTestItem and ResetCount
                PrevTestItem = 0;
                ResetCount = 0;

                // Check Auto or Manual mode to go to next page or menu page
                nTestItem++;
                if (IsAuto && (nTestItem < TestCount))  // Go to next page for Auto mode
                {
                    RootFrame.Navigate(GetTestPageType(TestName[(int)nTestItem]),
                                       GetTestPageParameter(TestName[(int)nTestItem]));
                }
                else
                {
                    // Return back to MenuPage.xaml for Manual mode or for Auto mode while all test items finished
                        RootFrame.Navigate(typeof(MenuPage));
                    if (App.IsModeHeadLess)
                    {
                        App.ExitApp();
                    }
                }
                isNavigatedTo = true;
            }
            return isNavigatedTo;
        }

#endregion // TestResultControl

    }
}
