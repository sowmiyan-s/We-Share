using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WeShare.Core.Models
{
    public class DeviceModel : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = Environment.MachineName;
        private string _type = "PC";
        private string _ipAddress = string.Empty;
        private int _port;
        private string? _ssid;
        private string? _password;
        private DateTime _lastSeen = DateTime.Now;
        private bool _isReceiver = false;
        private bool _isFavorite = false;
        private string? _customNickname;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public string? CustomNickname
        {
            get => _customNickname;
            set
            {
                if (SetProperty(ref _customNickname, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string DisplayName => !string.IsNullOrWhiteSpace(_customNickname) ? _customNickname : _name;

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string? Ssid
        {
            get => _ssid;
            set => SetProperty(ref _ssid, value);
        }

        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set => SetProperty(ref _lastSeen, value);
        }

        private string _role = "Idle";
        private string _connectionStatus = "Disconnected";

        public string Role
        {
            get => _role;
            set
            {
                if (SetProperty(ref _role, value))
                {
                    _isReceiver = string.Equals(_role, "Receiver", StringComparison.OrdinalIgnoreCase);
                    OnPropertyChanged(nameof(RoleDisplay));
                    OnPropertyChanged(nameof(IsReceiver));
                }
            }
        }

        public string RoleDisplay => _role switch
        {
            "Receiver" => "Ready to Receive",
            "Sender" => "Ready to Send",
            _ => "Idle"
        };

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public bool IsReceiver
        {
            get => _isReceiver || string.Equals(_role, "Receiver", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (SetProperty(ref _isReceiver, value))
                {
                    if (value && !string.Equals(_role, "Receiver", StringComparison.OrdinalIgnoreCase)) 
                        _role = "Receiver";
                    else if (!value && string.Equals(_role, "Receiver", StringComparison.OrdinalIgnoreCase)) 
                        _role = "Idle";
                    OnPropertyChanged(nameof(Role));
                    OnPropertyChanged(nameof(RoleDisplay));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
