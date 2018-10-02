using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;

namespace BleConnector
{
    class Program
    {
        static DeviceConnector deviceConnector;
        static DeviceWatcher watcher;
        static string macToFind = "";
        static void Main(string[] args)
        {
            var options = new Options();
            string macArgInput = "";
            string inputDataFilePath = "";
            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (o.MacAddress != String.Empty)
                {
                    Console.WriteLine("MAC: {0}", o.MacAddress);
                    macArgInput = o.MacAddress;
                }

                if (o.InputDataFilePath != String.Empty)
                {
                    inputDataFilePath = o.InputDataFilePath;
                }
            }
            );


            // Check MAC address
            string macRegex = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";

            Regex regex = new Regex(macRegex);

            watcher = new DeviceWatcher();
            deviceConnector = new DeviceConnector();

            if (regex.IsMatch(macArgInput))
            {
                macToFind = macArgInput;
                watcher.SetMacFilterString(macArgInput);
                watcher.DeviceFoundEvent += found_device;
            }


            watcher.StartScanning();

            // Wait for connection
            while (!deviceConnector.IsDeviceFound)
            {

            }

            while (deviceConnector.ServiceList.Count == 0)
            {
                // Find service
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

            while(true)
            {
                CSV.InputData.InputDataEntry entry = inputDataParser.GetNextEntry();

                if (entry == null)
                {
                    break;
                }

                Console.WriteLine(entry);

                Task.Run(async () =>
                {
                    await deviceConnector.WriteDataToCharacteristic(entry.UUID, entry.DataToSend);
                }).GetAwaiter().GetResult();            
            }

            Task.Delay(1000);

            CSV.OutputData.CsvDataWriter.FlushDataToFiles();
        }

        static async void found_device()
        {
            watcher.StopScanning();
            if (!deviceConnector.IsConnected)
            {
                await deviceConnector.ConnectToDevice(macToFind);
                watcher.StartScanning();
            }
        }
    }
}
