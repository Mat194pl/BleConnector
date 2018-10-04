using CommandLine;

namespace BleConnector
{
    internal class Options
    {
        [Option('m', "MAC address", Required = true, HelpText = "MAC address to be found")]
        public string MacAddress { get; set; }

        [Option('i', "Input data", Required = true, HelpText = "Path to file that contains data to be sent to BLE device")]
        public string InputDataFilePath { get; set; }

        [Option('c', "Configuration file", Required = true, HelpText = "Path to file that contains data to be sent to BLE device")]
        public string ConfigurationFilePath { get; set; }
    }
}