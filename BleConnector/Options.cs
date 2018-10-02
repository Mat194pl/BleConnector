using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace BleConnector
{
    class Options
    {
        [Option('m', "MAC address", Required = false, HelpText = "MAC address to be found")]
        public string MacAddress { get; set; }

        [Option('i', "Input data", Required = false, HelpText = "Path to file that contains data to be sent to BLE device")]
        public string InputDataFilePath { get; set; }
    }
}
