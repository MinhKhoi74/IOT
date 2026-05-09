namespace SmartParking.Configurations
{
    public class ArduinoSerialSettings
    {
        public bool Enabled { get; set; }
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int WriteTimeoutMilliseconds { get; set; } = 1000;
        public string NewLine { get; set; } = "\n";
    }
}
