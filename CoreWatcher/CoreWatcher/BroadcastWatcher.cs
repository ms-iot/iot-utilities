using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace CoreWatcher
{
    class EthernetInstance
    {
        public IPAddress Address = null;
        public bool Visited = false;
    }
    
    class BroadcastWatcher
    {
        public delegate void PingHandler(string Name, string IP, string Mac);
        public PingHandler OnPing { get; set; }
        Dictionary<IPAddress, EthernetInstance> NetworkAdapters = new Dictionary<IPAddress, EthernetInstance>();

        private void UnpackBuffer(byte[] buffer, out string sName, out string sIP, out string sMac)
        {
            string str = System.Text.Encoding.Unicode.GetString(buffer, 0, buffer.Length);
            sName = string.Empty;
            sIP = string.Empty;
            sMac = string.Empty;

            if (buffer.Length == (75 * sizeof(char)))
            {
                int host_offset = 0;
                const int host_len = 33;
                const int ipv4_offset = host_len;
                const int ipv4_len = 4 * 4 + 1;
                const int mac_offset = host_len + ipv4_len;

                // get the hostname
                int iBase = host_offset;
                while (str[iBase] != 0x00)
                {
                    sName += str[iBase++];
                }

                // get the IPv4 address
                iBase = ipv4_offset;
                while (str[iBase] != 0x00)
                {
                    sIP += str[iBase++];
                }

                // Get the MAC address
                iBase = mac_offset;
                while (str[iBase] != 0x00)
                {
                    sMac += str[iBase++];
                }
            }
        }

        private void OnReceiveSink(IAsyncResult result)
        {
            IPEndPoint ep = null;
            var args = (object[])result.AsyncState;
            var session = (UdpClient)args[0];
            var local = (IPEndPoint)args[1];

            byte[] buffer = session.EndReceive(result, ref ep);

            if (buffer.Length == (75 * sizeof(char))) 
            {
                string sName = string.Empty;
                string sIP = string.Empty;
                string sMac = string.Empty;

                UnpackBuffer(buffer, out sName, out sIP, out sMac);

                if (OnPing != null)
                {
                    OnPing(sName, sIP, sMac);
                }
            }

            //We make the next call to the begin receive
            session.BeginReceive(OnReceiveSink, args);
        }

        static List<IPAddress> GetValidIPAddresses( )
        {
            List<IPAddress> IPAddresses=new List<IPAddress>();
            // join multicast group on all available network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
                    (networkInterface.OperationalStatus != OperationalStatus.Up))
                {
                    continue;
                }

                IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
                IPAddress ipAddress = null;

                foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
                {
                    if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    IPAddresses.Add(unicastIPAddress.Address);
                    break;
                }

                if (ipAddress == null || ipAddress == IPAddress.Parse("127.0.0.1"))
                {
                    continue;
                }
            }
            return IPAddresses;
        }

        public void AddListeners()
        {
            // Mark all adapters as not visited.
            foreach(var inst in NetworkAdapters.Values)
            {
                inst.Visited = false;
            }
            
            List<IPAddress> IPList = GetValidIPAddresses();
            foreach(IPAddress localAddress in IPList)
            {
                if (NetworkAdapters.ContainsKey(localAddress))
                {
                    NetworkAdapters[localAddress].Visited = true;
                }
                else
                {
                    EthernetInstance inst = new EthernetInstance();
                    IPAddress multicastaddress = IPAddress.Parse("239.0.0.222");
                    int port = 6;
                    var udpClient = new UdpClient(AddressFamily.InterNetwork);
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    try
                    {
                        udpClient.Client.Bind(new IPEndPoint(localAddress, port));
                    }
                    catch (SocketException e)
                    {
                        //
                        // Bind() can return WSAEADDRNOTAVAIL if we try to bind
                        // too soon after plugging in a USB NIC.  If this 
                        // happens, we just continue and catch this connection 
                        // next time.  The cause of this is uncertain.  The
                        // message means "the ip address is invalid", but if
                        // we just wait a little while, the ip address becomes
                        // valid.  
                        //
                        // Easy to repro.  Just start this app and plug in a 
                        // USB adapter after it's running.
                        //
                        if (e.ErrorCode == 10049) // WSAEADDRNOTAVAIL
                        {
                            continue;
                        }
                    }
                    udpClient.JoinMulticastGroup(multicastaddress, localAddress);
                    udpClient.BeginReceive(OnReceiveSink,
                                           new object[]
                                   {
                                       udpClient, new IPEndPoint(localAddress, ((IPEndPoint) udpClient.Client.LocalEndPoint).Port)
                                   });
                    
                    inst.Address = localAddress;
                    inst.Visited = true;
                    NetworkAdapters.Add(localAddress,inst);
                }
            }

            // Remove un-visited adapters from our collection.  Operate on a copy of the collection since we're modifying from within an enumerator
            var newCollection = new Dictionary<IPAddress, EthernetInstance>(NetworkAdapters);
            foreach(var inst in NetworkAdapters.Values)
            {
                if (inst.Visited == false)
                {
                    newCollection.Remove(inst.Address);
                }
            }
            NetworkAdapters = newCollection;
            
        }
    }
}
