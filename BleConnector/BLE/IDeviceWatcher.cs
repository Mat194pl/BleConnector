namespace BleConnector.BLE
{
    public delegate void DeviceFoundEventHandler();

    internal interface IDeviceWatcher
    {
        void StartScanning();

        void StopScanning();

        void SetMacFilterString(string Mac);

        event DeviceFoundEventHandler DeviceFoundEvent;
    }
}