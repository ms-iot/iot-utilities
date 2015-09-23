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
using System.Windows.Forms;
using System.ComponentModel;

namespace CoreWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CoreList AllCoreInstances = new CoreList();
        private BroadcastWatcher Watcher = new BroadcastWatcher();
        NotifyIcon notifyIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();
            WatcherDataGrid.ItemsSource = AllCoreInstances;
            SetUpMissingDeviceTimer();
            SetUpBroadcastWatcher();
            SetUpSystemTray();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            Minimize();
        }

        private void Minimize()
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        protected override void OnClosed(EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnClosed(e);
        }

        private void SetUpMissingDeviceTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(MissingDeviceTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }


        private void MissingDeviceTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Watcher.AddListeners();
                AllCoreInstances.OnMissingDeviceTimerTick();
                // Instead of creating a thread to look for this event, we just use this polling event. 
                if (App.ActivateInstanceEvent.WaitOne(0))
                {
                    RestoreWindow();
                }
            }));
        }

        private void SetUpBroadcastWatcher()
        {
            Watcher.OnPing += new BroadcastWatcher.PingHandler(BroadcastWatcher_Ping);
            Watcher.AddListeners();
        }

        private void BroadcastWatcher_Ping(string Name, string IP, string Mac)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CoreInstance board;
                board = AllCoreInstances.OnPing(Name, IP, Mac);
            }));
        }

        private CoreInstance GetSelectedBoard()
        {
            object selected = WatcherDataGrid.CurrentItem;
            return selected as CoreInstance;
        }

        private void SetUpSystemTray()
        {
            System.Resources.ResourceManager resMan = new System.Resources.ResourceManager("CoreWatcher.g", typeof(MainWindow).Assembly);
            object obj = resMan.GetObject("Resources/CoreWatcherIcon.ico");
            if (obj != null)
            {
                notifyIcon.Icon = new System.Drawing.Icon((System.IO.Stream)obj);
            }
            notifyIcon.Click += OnSystemTrayClick;
            notifyIcon.DoubleClick += OnSystemTrayClick;
            notifyIcon.BalloonTipTitle = "Windows 10 IoT Core Watcher";
            notifyIcon.BalloonTipText = "App to watch for Windows 10 IoT Core Boards on your network";
            notifyIcon.Text = notifyIcon.BalloonTipText;

            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            notifyIcon.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Close", new System.EventHandler(this.OnSystemTrayExit)));
            notifyIcon.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Restore", new System.EventHandler(this.OnSystemTrayClick)));
        }

        private void OnSystemTrayExit(object sender, EventArgs args)
        {
            BoardTrcMonitor.ShutDownTrcDataMonitor();
            System.Windows.Application.Current.Shutdown();
        }

        private void OnSystemTrayClick(object sender, EventArgs args)
        {
            RestoreWindow();
        }

        private void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(500);
            }
            base.OnStateChanged(e);
        }

        private void AddToClipboard(string s)
        {
            System.Windows.Clipboard.Clear();
            System.Windows.Clipboard.SetText(s);
        }

        private void CopyMacAddress_Click(object sender, RoutedEventArgs e)
        {
            CoreInstance board = GetSelectedBoard();
            if (board != null)
            {
                AddToClipboard(board.MacAddress);
            }
        }

        private void CopyIPAddress_Click(object sender, RoutedEventArgs e)
        {
            CoreInstance board = GetSelectedBoard();
            if (board != null)
            {
                AddToClipboard(board.IpAddress);
            }
        }

        private void TelnetHere_Click(object sender, RoutedEventArgs e)
        {
            CoreInstance board = GetSelectedBoard();
            if (board != null)
            {
                string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                try
                {
                    System.Diagnostics.Process.Start("telnet:" + board.IpAddress);
                }
                catch (Exception ex)
                {
                    ShowAppLaunchFailure(ex);
                }
            }
        }

        private void WebBrowserHere_Click(object sender, RoutedEventArgs e)
        {
            CoreInstance board = GetSelectedBoard();
            if (board != null)
            {
                try
                {
                    System.Diagnostics.Process.Start("http://" + board.IpAddress);
                }
                catch (Exception ex)
                {
                    ShowAppLaunchFailure(ex);
                }
            }
        }

        private void OpenNetworkShare_Click(object sender, RoutedEventArgs e)
        {
            CoreInstance board = GetSelectedBoard();
            if (board != null)
            {
                try
                {
                    System.Diagnostics.Process.Start("\\\\" + board.IpAddress + "\\" + "c$");
                }
                catch (Exception ex)
                {
                    ShowAppLaunchFailure(ex);
                }
            }
        }

        private void ShowAppLaunchFailure(Exception e)
        {
            System.Windows.MessageBox.Show(this, "Failure launching external program:\n" + e.Message);
        }

    }
}
