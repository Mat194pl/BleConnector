using System;

namespace BleConnector.CSV.InputData
{
    internal class InputDataEntry
    {
        public Guid UUID;
        public byte[] DataToSend;

        public override string ToString()
        {
            return "UUID: " + UUID + ", DataToSend:" + DataToSend;
        }
    }
}