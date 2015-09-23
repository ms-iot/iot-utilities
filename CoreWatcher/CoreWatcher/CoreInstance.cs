using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CoreWatcher
{
    public class CoreInstance : INotifyPropertyChanged
    {
        private string _boardName;
        private string _macAddress;
        private string _ipAddress;
        private DateTime _lastPing;
        private bool _online;

        private string _osVersion;
        private string _biosVersion;
        private string _platType;

        public string BoardName 
        {
            get
            {
                return _boardName;
            }
            set
            {
                if (_boardName != value)
                {
                    _boardName = value;
                    RaisePropertyChanged("BoardName");
                }
            }
        }

        public string MacAddress {
            get 
            {
                return _macAddress;
            }
            set
            {
                if (_macAddress != value)
                {
                    _macAddress = value;
                    RaisePropertyChanged("MacAddress");
                }
            }
        }

        public string IpAddress 
        { 
            get
            {
                return _ipAddress;
            } 
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    RaisePropertyChanged("IpAddress");
                }
            } 
        }
        public DateTime LastPing 
        { 
            get
            {
                return _lastPing;
            } 
            set
            {
                if (_lastPing != value)
                {
                    _lastPing = value;
                    RaisePropertyChanged("LastPing");
                }
            } 
        }
        public bool Online 
        { 
            get
            {
                return _online;
            }
            set
            {
                if (_online != value)
                {
                    _online = value;
                    RaisePropertyChanged("Online");
                }
            }
        }
        public string OsVersion
        {
            get
            {
                return _osVersion;
            }
            set
            {
                if (_osVersion != value)
                {
                    _osVersion = value;
                    RaisePropertyChanged("OsVersion");
                }
            }
        }
        public string BiosVersion
        {
            get
            {
                return _biosVersion;
            }
            set
            {
                if (_biosVersion != value)
                {
                    _biosVersion = value;
                    RaisePropertyChanged("BiosVersion");
                }
            }
        }
        public string PlatType
        {
            get
            {
                return _platType;
            }
            set
            {
                if (_platType != value)
                {
                    _platType = value;
                    RaisePropertyChanged("PlatType");
                }
            }
        }

        public CoreInstance()
        {
            BoardName = String.Empty;
            MacAddress = String.Empty;
            IpAddress = String.Empty;
            Online = false;
            OsVersion = String.Empty;
            BiosVersion = String.Empty;
            PlatType = String.Empty;
        }

        public override int GetHashCode()
        {
            return MacAddress.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            CoreInstance other = obj as CoreInstance;
            if (this == null || other == null)
            {
                return false;
            }
            else
            {
                return MacAddress.Equals(other.MacAddress);
            }
        }

#region INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler e = PropertyChanged;
            if (e != null)
            {
                e(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
#endregion

    }
}
