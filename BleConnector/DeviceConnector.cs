using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BleConnector
{
    public class DeviceConnector
    {
        public delegate void CharacteristicDataReceivedHandler(Guid characteristicUuuid, byte[] receivedData);
        CharacteristicDataReceivedHandler characteristicDataHandler;

        private bool is_connected = false;
        public bool IsConnected
        {
            get
            {
                return is_connected;
            }
        }

        public bool IsDeviceFound
        {
            get
            {
                return bluetoothLeDevice != null;
            }
        }

        GattCharacteristic logChar = null;
        private static BluetoothLEDevice bluetoothLeDevice = null;

        // Define Service List.  
        public List<string> ServiceList = new List<string>();
        public List<string> characteristicList = new List<string>();

        private List<GattDeviceService> deviceServices = null;
        private List<GattCharacteristic> deviceCharacteristics = null;

        bool mutex = true;

        public async Task ConnectToDevice(string mac)
        {
            if (mutex)
            {
                mutex = false;
            }
            else
            {
                return;
            }
            Console.WriteLine("Try to connect to " + mac);
            string hex = mac.Replace(":", "");
            ulong macAsNumber = Convert.ToUInt64(hex, 16);

            bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(macAsNumber);

            if (bluetoothLeDevice == null)
            {
                Console.WriteLine("Cannot connect to " + mac);
            }
            else
            {
                Console.WriteLine("Connecting to " + mac);             
                bluetoothLeDevice.ConnectionStatusChanged += BluetoothLeDevice_ConnectionStatusChanged;
                bluetoothLeDevice.GattServicesChanged += BluetoothLeDevice_GattServicesChanged;
            }
            mutex = true;
        }

        private void BluetoothLeDevice_GattServicesChanged(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Device gatt services changed: " + sender.ConnectionStatus);

            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                is_connected = true;
            }

            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                bluetoothLeDevice = null;
            }

        }

        private void BluetoothLeDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("Device connection status: " + sender.ConnectionStatus);
        }

        public async Task RegisterCharacteristicDataHandler(Guid charUuid, CharacteristicDataReceivedHandler receivedHandler)
        {
            await RegisterCharacteristicChange(charUuid);

            characteristicDataHandler = receivedHandler;
        }

        public async Task WriteDataToCharacteristic(Guid charUuid, byte[] dataToWrite)
        {
            IBuffer bufferToSend = dataToWrite.AsBuffer();

            var chr = deviceCharacteristics.Where(i => i.Uuid == charUuid).First();

            if (chr == null)
            {
                return;
            }

            try
            {
                var result = await chr.WriteValueWithResultAsync(bufferToSend);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task DiscoverServices()
        {
            IReadOnlyList<GattDeviceService> services = null;
            IReadOnlyList<GattCharacteristic> characteristics = null;
            GattDeviceServicesResult gattDeviceServicesResult = null;
            GattCharacteristicsResult gattCharacteristicResult = null;
            deviceServices = new List<GattDeviceService>();
            deviceCharacteristics = new List<GattCharacteristic>();
            if (bluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                gattDeviceServicesResult = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (gattDeviceServicesResult.Status == GattCommunicationStatus.Success)
                {
                    services = gattDeviceServicesResult.Services;            

                    foreach (GattDeviceService ser in services)
                    {
                        Console.WriteLine("\tFound service with UUID: " + ser.Uuid.ToString());
                        ServiceList.Add(ser.Uuid.ToString());
                        deviceServices.Add(ser);
                        // Discover characteristics
                        try
                        {
                            // Ensure we have access to the device.
                            var accessStatus = await ser.RequestAccessAsync();
                            if (accessStatus == DeviceAccessStatus.Allowed)
                            {
                                // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                                // and the new Async functions to get the characteristics of unpaired devices as well. 
                                gattCharacteristicResult = await ser.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                                if (gattCharacteristicResult.Status == GattCommunicationStatus.Success)
                                {
                                    characteristics = gattCharacteristicResult.Characteristics;

                                    foreach (GattCharacteristic chr in characteristics)
                                    {
                                        Console.WriteLine("\t\tFound characteristics with UUID: " + chr.Uuid.ToString());
                                        characteristicList.Add(chr.Uuid.ToString());
                                        deviceCharacteristics.Add(chr);
                                    }


                                }
                                else
                                {
                                }
                            }
                            else
                            {
                                
                            }
                        }
                        catch (Exception ex)
                        {
                           
                        }

                    }
                }
                else
                {
                }
            }


            Guid serviceGuid = Guid.Parse("ae000001-c254-e084-6940-8f5de0ffd2b8");

            Console.WriteLine("Discover end");

        }

        private async Task<bool> RegisterCharacteristicChange(Guid characteristicUuid)
        {
            var chr = deviceCharacteristics.Where(i => i.Uuid == characteristicUuid).First();

            if (chr == null)
            {

            }

            try
            {

                GattCommunicationStatus status = await chr.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                }

                chr.ValueChanged += Chr_ValueChanged;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
        }

        private void Chr_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            IBuffer readBuffer = args.CharacteristicValue;

            byte[] receivedBytes = readBuffer.ToArray();

            characteristicDataHandler?.Invoke(sender.Uuid, receivedBytes);
        }

        private async Task<bool> RegisterLogCharacteristicChange()
        {
            GattCommunicationStatus status = await logChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Success)
            {
                // Server has been informed of clients interest.
            }

            logChar.ValueChanged += LogChar_ValueChanged; ;

            return true;
        }

        private void UnregisterLogCharacteristicChange()
        {
            logChar.ValueChanged -= LogChar_ValueChanged;
        }

        private static void LogChar_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
        }
    }
}
