using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PROG280__Remote_Access_App_Data__
{
    public class ConnectionManager : INotifyPropertyChanged
    {
        public delegate void LocalMessageDelegate(string message);
        public event LocalMessageDelegate LocalMessageEvent;

        private bool _connectionStatus = false;
        private int _port;
        public TcpListener? TcpListener;
        public TcpClient? TcpClient;

        //Where I got to, planning to have this class differentiate between client and server so that each can swap over.
        private bool _isServer = false;

        private List<string> _messages = new List<string>();

        public List<string> Messages { get { return _messages; } }

        public ConnectionManager()
        {
            LocalMessageEvent += LocalMessageHandler;
            LocalMessageEvent += AddToMessagesList;
        }

        public void AddToMessagesList(string message)
        {
            _messages.Add(message);
        }

        public void LocalMessageHandler(string message)
        {
            if(LocalMessageEvent != null)
            {
                LocalMessageEvent(message);
            }
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
                    LocalMessageEvent("Invalid Port Detected.");
                    _port = 0;
                };
                OnPropertyChanged();
            }
        }

        public bool ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    switch (_connectionStatus)
                    {
                        case false:
                            LocalMessageEvent("Disconnected.");
                            break;

                        default:
                            LocalMessageEvent("Connected.");
                            break;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
