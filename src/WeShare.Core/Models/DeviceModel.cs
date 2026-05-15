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

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

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
