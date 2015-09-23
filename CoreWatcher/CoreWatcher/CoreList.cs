using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;


namespace CoreWatcher
{
    public class CoreList : ObservableCollection<CoreInstance>
    {
        public CoreInstance OnPing(string Name, string IP, string Mac)
        {
            CoreInstance newDevice = new CoreInstance();
            newDevice.BoardName = Name;
            newDevice.IpAddress = IP;
            newDevice.MacAddress = Mac;
            newDevice.LastPing = DateTime.Now;
            newDevice.Online = true;
            if (this.Contains(newDevice))
            {
                int i = this.IndexOf(newDevice);

                this[i].LastPing = DateTime.Now;
                this[i].IpAddress = IP;
                this[i].BoardName = Name;
                this[i].Online = true;

                BoardTrcData bdTrcData = BoardTrcMonitor.GetBoardTrcData(IP);
                if (bdTrcData != null)
                {
                    this[i].OsVersion = bdTrcData.PlatInfo_OsBuildLabEx;
                    this[i].BiosVersion = bdTrcData.PlatInfo_SystemBiosVersion;
                    this[i].PlatType = bdTrcData.PlatInfo_PlatType;
                }
            }
            else
            {
                Add(newDevice);
            }
            return newDevice;
        }

        public void OnMissingDeviceTimerTick()
        {
            foreach (CoreInstance inst in this)
            {
                if (DateTime.Now - inst.LastPing > PingTimeout)
                {
                    inst.Online = false;
                }
            }
        }

        TimeSpan PingTimeout = new TimeSpan(0, 0, 6);  // How many seconds between pings before a device is marked as off
    }

}
