using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PROG280__Remote_Access_App_Data__
{
    public class NetworkConnected
    {
        public bool IsConnected { get; set; } = false;

        protected const int _chunkSize = 1024;
        protected const int _packetSize = 1500;
        protected int _port = 9000;

        public string LocalIPAddress { get; set; } = string.Empty;
        public string RemoteIPAddress { get; set; } = string.Empty;

        public int Port
        {
            get { return _port; }
            set
            {
                if (int.TryParse(value.ToString(), out int parsedValue))
                {
                    _port = parsedValue;
                }
                else
                {
                    _port = 0;
                };
            }
        }

        public TcpListener? TcpListener { get; set; }
        public TcpClient? TcpVideoClient { get; set; }

        protected NetworkStream? _videoStream { get; set; }

        public static ObservableCollection<string> Messages { get; set; } = new();

        public static void AddToMessagesList(string message)
        {
            Messages.Add(message);
        }

        public void CloseConnections()
        {
            _videoStream!.Close();
            TcpVideoClient!.Close();
            _videoStream.Dispose();
            TcpVideoClient?.Dispose();
        }

        public void ShutDown()
        {
            TcpListener!.Stop();
            TcpListener?.Dispose();
        }
    }
}
