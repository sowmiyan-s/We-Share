using System;

namespace WeShare.Core.Models
{
    public class DeviceModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = Environment.MachineName;
        public string Type { get; set; } = "PC"; // PC, Mobile
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}
