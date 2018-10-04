using System;
using System.Threading.Tasks;

namespace BleConnector.BLE
{
    public delegate void CharacteristicDataReceivedHandler(Guid characteristicUuuid, byte[] receivedData);

    internal interface IDeviceConnector
    {
        Task ConnectToDevice(string Mac);

        bool IsConnected();

        bool IsDeviceFound();

        Task RegisterCharacteristicDataHandler(Guid charUuid, CharacteristicDataReceivedHandler receivedHandler);
    }
}