using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BleConnector.CSV.InputData
{
    class InputDataEntry
    {
        public Guid UUID;
        public byte[] DataToSend;

        public override string ToString()
        {
            return "UUID: " + UUID + ", DataToSend:" + DataToSend;
        }
    }
}
