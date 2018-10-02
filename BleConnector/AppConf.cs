using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BleConnector
{
    public class AppConf
    {
        [Serializable]
        public class Characteristic
        {
            [System.Xml.Serialization.XmlElement("UUID")]
            public string UUID { get; set; }

            [System.Xml.Serialization.XmlElement("Name")]
            public string Name { get; set; }

            [System.Xml.Serialization.XmlElement("IsReadable")]
            public string IsReadable { get; set; }

            [System.Xml.Serialization.XmlElement("IsWriteable")]
            public string IsWriteable { get; set; }
        }

        [Serializable()]
        [System.Xml.Serialization.XmlRoot("Services")]
        public class Service
        {
            [System.Xml.Serialization.XmlElement("UUID")]
            public string UUID { get; set; }

            [XmlArray("Characteristics")]
            [XmlArrayItem("Characteristic", typeof(Characteristic))]
            public Characteristic[] Characteristic { get; set; }
        }


        [Serializable()]
        [System.Xml.Serialization.XmlRoot("Configuration")]
        public class Configuration
        {
            [XmlArray("Services")]
            [XmlArrayItem("Service", typeof(Service))]
            public Service[] Service { get; set; }
        }

        public static Configuration DeserializeXml(String filePath)
        {
            Configuration configuration = null;

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

            StreamReader reader = new StreamReader(filePath);
            configuration = (Configuration)serializer.Deserialize(reader);
            reader.Close();

            return configuration;
        }

        public static bool ApplyConfigurationConditions(Configuration configuration, DeviceConnector connector)
        {
            foreach (var service in configuration.Service)
            {
                Console.Write("\tCheck service: " + service.UUID + " ... ");
                if (!connector.ServiceList.Contains(service.UUID))
                {
                    Console.WriteLine("missing");
                    continue;
                }

                Console.WriteLine("present");

                foreach(var characteristic in service.Characteristic)
                {
                    Console.Write("\t\tCheck characteristic: " + characteristic.UUID + " ... ");
                    if (!connector.characteristicList.Contains(characteristic.UUID))
                    {
                        Console.WriteLine("missing");
                        continue;
                    }

                    if (characteristic.IsReadable.Equals("True"))
                    {
                        Task.Run(async () =>
                        {
                            await connector.RegisterCharacteristicDataHandler(new Guid(characteristic.UUID), CSV.OutputData.CsvDataWriter.WriteCharacteristicDataToFile);
                        }).GetAwaiter().GetResult();
                    }

                    Console.WriteLine("present");
                }

            }


            return true;
        }
    }
}
