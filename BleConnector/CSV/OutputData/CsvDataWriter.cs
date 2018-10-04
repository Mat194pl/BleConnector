using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BleConnector.CSV.OutputData
{
    internal class CsvDataWriter
    {
        // Dictionary contains StringBuilder object for each UUID from data was received
        private static Dictionary<Guid, Tuple<StringBuilder, string>> usedFiles = new Dictionary<Guid, Tuple<StringBuilder, string>>();
        private static readonly object SyncObject = new object();

        public static void WriteCharacteristicDataToFile(Guid characteristicUuid, byte[] dataToWrite)
        {
            Console.WriteLine("Received data from UUID: " + characteristicUuid.ToString() + " of length: " + dataToWrite.Length + " to file.");

            lock (SyncObject)
            {
                if (!usedFiles.ContainsKey(characteristicUuid))
                {
                    usedFiles.Add(characteristicUuid, new Tuple<StringBuilder, string>(new StringBuilder(), characteristicUuid.ToString() + ".csv"));
                }

                var file = usedFiles[characteristicUuid];

                StringBuilder data_string = new StringBuilder(dataToWrite.Length * 2);

                foreach (byte b in dataToWrite)
                {
                    data_string.AppendFormat("{0:x2}", b);
                }

                DateTime dateTime = DateTime.Now;
                string dateTimeString = dateTime.Year.ToString("D4") + "-" + dateTime.Month.ToString("D2") + "-" + dateTime.Day.ToString("D2")
                    + " " + dateTime.Hour.ToString("D2") + ":" + dateTime.Minute.ToString("D2") + ":" + dateTime.Second.ToString("D2");
                file.Item1.AppendLine(string.Format("{0};{1}", dateTimeString, data_string.ToString()));
            }
        }

        public static void FlushDataToFiles()
        {
            foreach (Tuple<StringBuilder, string> file in usedFiles.Values)
            {
                File.AppendAllText(file.Item2, file.Item1.ToString());
                file.Item1.Clear();
            }
        }
    }
}