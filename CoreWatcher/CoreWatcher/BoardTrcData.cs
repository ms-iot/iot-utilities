using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;

namespace CoreWatcher
{
    static class BoardTrcMonitor
    {
        public static BoardTrcData GetBoardTrcData(string hostNameOrIp)
        {
            if (String.IsNullOrEmpty(hostNameOrIp)) return null;

            // init resources
            lock (monLock)
            {
                if (_bdList == null) _bdList = new List<BoardTrcData>();
                if (_monThread == null)
                {
                    _monThread = new Thread(new ThreadStart(MonitorThread));
                    if (_monThread != null) _monThread.Start();
                }
            }

            // see if name being monitored already
            foreach (BoardTrcData bd in _bdList)
            {
                if (bd.host.CompareTo(hostNameOrIp) == 0) return bd;
            }

            // if not there, add new one to list for scheduled updates
            BoardTrcData newbd = new BoardTrcData();
            newbd.ClearPlatInfo();
            newbd.host = hostNameOrIp;
            lock (monLock)
            {
                _bdList.Add(newbd);
            }

            return null;
        }

        public static void ShutDownTrcDataMonitor ()
        {
            if (_monThread == null) return;
            _monThread.Abort();
            _monThread.Join();
        }

        private static void MonitorThread()
        {
            int i = 0;
            TimeSpan span;

            while (true)
            {
                Thread.Sleep(500);
                if (_bdList == null) continue;

                lock (monLock)
                {
                    ++i;
                    if (i >= _bdList.Count || i < 0) i = 0;
                }

                span = DateTime.Now - _bdList[i].PlatInfo_DateTimeAttempted;
                if (_bdList[i].PlatInfo_DateTimeAttempted == DateTime.MinValue || span.TotalMinutes > 5)
                {
                    _bdList[i].GetTrcData();
                }
            }
        }

        private static Object monLock = new Object();
        private static List<BoardTrcData> _bdList = null;
        private static Thread _monThread = null;
    }

    class BoardTrcData
    {
        public string host = "";

        Socket socket = null;
        string _platInfo = "";
        string _platInfo_AvailRamKb = "";
        string _platInfo_OsBuildLabEx = "";
        string _platInfo_SystemBiosVersion = "";
        string _platInfo_PlatType = "";
        DateTime _lastTrcAttempted = DateTime.MinValue;
        DateTime _lastTrcFetched = DateTime.MinValue;

        public BoardTrcData()
        {
            _lastTrcAttempted = DateTime.MinValue;
            _lastTrcFetched = DateTime.MinValue;
            ClearPlatInfo();
        }

        public void ClearPlatInfo ()
        {
            _platInfo = "";
            _platInfo_AvailRamKb = "";
            _platInfo_OsBuildLabEx = "";
            _platInfo_SystemBiosVersion = "";
            _platInfo_PlatType = "";
        }

        public string PlatInfo
        {
            get { return _platInfo; }
        }
        public string PlatInfo_AvailRamKb
        {
            get { return _platInfo_AvailRamKb; }
        }
        public string PlatInfo_OsBuildLabEx
        {
            get { return _platInfo_OsBuildLabEx; }
        }
        public string PlatInfo_SystemBiosVersion
        {
            get { return _platInfo_SystemBiosVersion; }
        }
        public string PlatInfo_PlatType
        {
            get { return _platInfo_PlatType; }
        }
        public DateTime PlatInfo_DateTimeAttempted
        {
            get { return _lastTrcAttempted; }
        }
        public DateTime PlatInfo_DateTimeUpdated
        {
            get { return _lastTrcFetched; }
        }

        public void GetTrcData(string hostNameOrIp)
        {
            ClearPlatInfo();

            host = hostNameOrIp;
            if (String.IsNullOrEmpty(host)) return;

            ConnectToService();
            if (socket != null && socket.Connected)
            {
                FlushInput(socket, 100);
                GetPlatInfo(socket);
                socket.Disconnect(false);
            }
        }

        public void GetTrcData()
        {
            GetTrcData(host);
        }

        private void GetPlatInfo(Socket sock)
        {
            _platInfo = "";

            string message = "<transact><request from=\"\" type=\"platinfo\"/><command name=\"getplatinfo\" /></transact>";
            try
            {
                _lastTrcAttempted = DateTime.Now;
                Send(sock, Encoding.UTF8.GetBytes(message), 0, message.Length, 2000);
            }
            catch (Exception)
            {
                return;
            }

            byte[] buffer = new byte[20 * 1024];
            try
            {
                int rx = Receive(sock, buffer, buffer.Length, 5000);
                _platInfo = Encoding.ASCII.GetString(buffer, 0, rx);
                ParsePlatInfoXml(_platInfo);
            }
            catch (Exception)
            {
                return;
            }
        }

        void ParsePlatInfoXml(string platInfoXml)
        {
            _platInfo_OsBuildLabEx = "";
            _platInfo_AvailRamKb = "";
            _platInfo_SystemBiosVersion = "";
            _platInfo_PlatType = "";

            try
            {
                XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(platInfoXml));
                reader.MoveToContent();
                String curElem = "";

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            curElem = reader.Name;
                            break;
                        case XmlNodeType.Text:
                            if (curElem == "OsBuildLabEx")
                            {
                                _platInfo_OsBuildLabEx = reader.Value;
                            }
                            else if (curElem == "AvailRamKb")
                            {
                                _platInfo_AvailRamKb = reader.Value;
                            }
                            else if (curElem == "SystemBiosVersion")
                            {
                                _platInfo_SystemBiosVersion = reader.Value;
                            }
                            else if (curElem == "PlatType")
                            {
                                _platInfo_PlatType = reader.Value;
                            }                            
                            break;
                        case XmlNodeType.EndElement:
                            curElem = "";
                            break;
                    }
                }
                reader.Close();
                _lastTrcFetched = DateTime.Now;
            }
            catch (Exception)
            {
            }
        }


        void ConnectToService()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            int startTickCount = Environment.TickCount;
            try
            {
                IPAddress[] IPs = Dns.GetHostAddresses(host);
                if (IPs.Length == 0) return;

                int use_ix = 0;
                for (int i = 0; i < IPs.Length; ++i)
                {
                    use_ix = i; // use last in list if multiple
                }

                IAsyncResult result = socket.BeginConnect(IPs[use_ix], 8027, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(4000, true);
                if (!success)
                {
                    socket.Close();
                    throw new ApplicationException("Failed to connect server.");
                }
                return;
            }
            catch (Exception)
            {
                socket.Dispose();
                socket = null;
            }
        }


        public void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int errorTimeout = Math.Max(10, Math.Min(timeout / 2, 20));
            int sent = 0;
            socket.SendTimeout = timeout;
            do
            {
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(errorTimeout);
                    }
                    else if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        return;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                if ((Environment.TickCount - startTickCount) > timeout) return;
            } while (sent < size);
        }

        public int Receive(Socket socket, byte[] buffer, int size, int timeout)
        {
            socket.ReceiveTimeout = timeout;
            int errorTimeout = Math.Max(10, Math.Min(timeout / 2, 20));
            int startTickCount = Environment.TickCount;
            int received = 0;
            int offset = 0;
            do
            {
                try
                {
                    received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(errorTimeout);
                    }
                    else if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        return received;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                if ((Environment.TickCount - startTickCount) > timeout) return received;
            } while (received == 0);
            return received;
        }

        public void FlushInput(Socket socket, int timeout)
        {
            byte[] buffer = new byte[2 * 1024];
            int startTickCount = Environment.TickCount;
            socket.ReceiveTimeout = timeout;
            int errorTimeout = Math.Max(10, Math.Min(timeout / 2, 20));
            do
            {
                try
                {
                    int rx = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (rx == 0)
                    {
                        return;
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(errorTimeout);
                    }
                    else if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        return;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                if ((Environment.TickCount - startTickCount) > timeout) return;
            } while (true);
        }
    }

}