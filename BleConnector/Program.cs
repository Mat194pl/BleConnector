using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BleConnector.BLE;
using CommandLine;

namespace BleConnector
{
    internal class Program
    {
        private static DeviceConnector deviceConnector;
        private static DeviceWatcher watcher;
        private static string macToFind = "";

        private static void Main(string[] args)
        {
            var options = new Options();
            string macArgInput = "";
            string inputDataFilePath = "";
            string configurationFilePath = "";
            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (o.MacAddress != String.Empty)
                {
                    Console.WriteLine("MAC: {0}", o.MacAddress);
                    macArgInput = o.MacAddress;
                }
                else
                {
                    PrintWrongMacArgumentInfo();
                    Environment.Exit(-1);
                }

                if (!File.Exists(o.InputDataFilePath))
                {
                    PrintWrongInputFileArgumentInfo((o.InputDataFilePath));
                    Environment.Exit(-1);
                }
                else
                {
                    inputDataFilePath = o.InputDataFilePath;
                }

                if (!File.Exists(o.ConfigurationFilePath))
                {
                    PrintWrongInputFileArgumentInfo(o.ConfigurationFilePath);
                    Environment.Exit(-1);
                }
                else
                {
                    configurationFilePath = o.ConfigurationFilePath;
                }
            }
            );

            watcher = new DeviceWatcher();
            deviceConnector = new DeviceConnector();

            // Check MAC address
            string macRegex = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
            Regex regex = new Regex(macRegex);

            if (regex.IsMatch(macArgInput))
            {
                macToFind = macArgInput;
                watcher.SetMacFilterString(macArgInput);
                watcher.DeviceFoundEvent += FoundDeviceEventHandler;
            }
            else
            {
                PrintWrongMacArgumentInfo();
                Environment.Exit(-1);
            }

            watcher.StartScanning();

            // Wait for connection
            while (!deviceConnector.IsDeviceFound())
            {
            }

            while (deviceConnector.ServiceList.Count == 0)
            {
                // Discover services and characteristics
                Task.Run(async () =>
                {
                    await deviceConnector.DiscoverServices();
                }).GetAwaiter().GetResult();
            }

            Console.WriteLine("Load configuration file");
            AppConf.Configuration configuration = AppConf.DeserializeXml("altlight_plug_conf.xml");
            bool checkResult = AppConf.ApplyConfigurationConditions(configuration, deviceConnector);

            Console.WriteLine("Check result: " + checkResult);

            Console.WriteLine("Load input data " + inputDataFilePath);
            CSV.InputData.InputDataParser inputDataParser = new CSV.InputData.InputDataParser();
            inputDataParser.OpenFile(inputDataFilePath);

            while (true)
            {
                CSV.InputData.InputDataEntry entry = inputDataParser.GetNextEntry();

                // If entry is null, then this must be end of file
                if (entry == null)
                {
                    break;
                }

                Console.WriteLine(entry);

                // Send data to specified uuid
                Task.Run(async () =>
                {
                    await deviceConnector.WriteDataToCharacteristic(entry.UUID, entry.DataToSend);
                }).GetAwaiter().GetResult();

                // Wait some time, TODO: Find the way to send data without losing it and not using fixed interval 
                Task.Run(async () =>
                {
                    await Task.Delay(400);
                }).GetAwaiter().GetResult();
            }

            // Wait some time after sending last part of input data
            Task.Run(async () =>
            {
                await Task.Delay(1000);
            }).GetAwaiter().GetResult();

            // Flush received data to .csv files
            CSV.OutputData.CsvDataWriter.FlushDataToFiles();
        }

        private static void PrintWrongMacArgumentInfo()
        {
            Console.WriteLine("Wrong MAC address format, try XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX");
        }

        private static void PrintWrongInputFileArgumentInfo(string filePath)
        {
            Console.WriteLine("Wrong input data file path: " + filePath);
        }

        private static void PrintWrongConfigurationFileArgumentInfo(string filePath)
        {
            Console.WriteLine("Wrong configuration file path: " + filePath);
        }

        private static async void FoundDeviceEventHandler()
        {
            watcher.StopScanning();
            // If device is still not connected, try to connect to it and start scanning again
            if (!deviceConnector.IsConnected())
            {
                await deviceConnector.ConnectToDevice(macToFind);
                watcher.StartScanning();
            }
        }
    }
}