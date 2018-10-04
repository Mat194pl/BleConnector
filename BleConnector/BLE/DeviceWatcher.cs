using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth.Advertisement;

namespace BleConnector.BLE
{
    internal class DeviceWatcher : IDeviceWatcher
    {
        private BluetoothLEAdvertisementWatcher deviceWatcher;
        private string macFilter = "";
        private Stopwatch watch = new Stopwatch();

        public event DeviceFoundEventHandler DeviceFoundEvent;

        public void SetMacFilterString(string Mac)
        {
            macFilter = Mac;
        }

        public void StartScanning()
        {
            watch.Restart();
            deviceWatcher = new BluetoothLEAdvertisementWatcher();

            deviceWatcher.ScanningMode = BluetoothLEScanningMode.Active;
            deviceWatcher.Received += DeviceWatcher_Received;

            deviceWatcher.Start();
        }

        public void StopScanning()
        {
            deviceWatcher.Stop();
        }

        private void DeviceWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var tempMac = args.BluetoothAddress.ToString("X");
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";
            var macAddress = Regex.Replace(tempMac, regex, replace);

            // TODO: Add log advertising data functionality

            /*var list = args.Advertisement.DataSections;
            foreach (BluetoothLEAdvertisementDataSection data in args.Advertisement.DataSections)
            {
                var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(data.Data);
                byte[] raw = new byte[data.Data.Length];
                dataReader.ReadBytes(raw);
                string hex = BitConverter.ToString(raw);

                if (data.Data.Length >= 21 && raw[data.Data.Length - 1] == 0x84)
                {
                    Console.WriteLine("Data:");
                    Console.WriteLine("DT: " + data.DataType);

                    Console.WriteLine(hex);
                }
            }*/

            if (macAddress.Equals(macFilter))
            {
                if (watch.ElapsedMilliseconds > 1000) // Fire event not more than once per second
                {
                    DeviceFoundEvent?.Invoke();

                    watch.Restart();
                }

                //Console.WriteLine("Fond" + args.AdvertisementType.ToString());

                /*var list = args.Advertisement.DataSections;
                foreach (BluetoothLEAdvertisementDataSection data in list)
                {
                    Console.WriteLine("Data");
                }
                Console.WriteLine("MAC: " + macAddress);
                foreach (Guid uuid in args.Advertisement.ServiceUuids)
                {
                    Console.WriteLine("UUID: " + uuid);
                }*/
            }
        }
    }
}