using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PROG280__Remote_Access_App_Data__
{
    public class ConnectionManager
    {
        private bool _isConnected = false;
        private int _port = 8000;

        private TcpListener? _tcpListener;
        private TcpClient? _tcpClient;

        public TcpListener? TcpListener
        {
            get
            {
                return _tcpListener;
            }
            set
            {
                _tcpListener = value;
                _isServer = true;
            }
        }

        public TcpClient? TcpClient
        {
            get
            {
                return _tcpClient;
            }
            set
            {
                if (_tcpClient != null)
                {
                    _tcpClient = value;
                    _isServer = false;
                    IsConnected = true;
                }
                else
                {
                    _tcpClient = value;
                    IsConnected = false;
                }
            }
        }

        //Where I got to, planning to have this class differentiate between client and server so that each can swap over.
        private bool _isServer = false;

        private ObservableCollection<string> _messages = new ObservableCollection<string>();

        public ObservableCollection<string> Messages { get { return _messages; } }

        public void AddToMessagesList(string message)
        {
            _messages.Add(message);
        }

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

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                }
            }
        }
    }
}
