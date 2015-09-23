using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace CoreWatcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string EventName = @"Local\CoreWatcherEvent";

        public static EventWaitHandle ActivateInstanceEvent;

        static App()
        {

            ActivateInstanceEvent = new EventWaitHandle(true, EventResetMode.AutoReset, EventName);

            Process currentProcess = Process.GetCurrentProcess();

            foreach (Process p in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if ((p.SessionId == currentProcess.SessionId) &&
                     (p.Id != currentProcess.Id))
                {
                    // There's another process with the same name in our session.  Activate it. 
                    ActivateInstanceEvent.Set();

                    // sayonara
                    Environment.Exit(-2);
                }
            }
        }
    }
}
