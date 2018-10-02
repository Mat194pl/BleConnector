using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BleConnector.CSV.InputData
{
    class InputDataParser
    {
        // StreamReader used for reading lines from csv file
        StreamReader csvStreamReader;

        // Current line in csv file
        UInt32 currentLineNumber;

        // Current csv file path
        string currentFilePath;

        // Function for opening csv file
        public bool OpenFile(String filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File " + filePath + " doesn't exist");
                return false;
            }
            currentFilePath = filePath;
            currentLineNumber = 0;
            csvStreamReader = new StreamReader(filePath);
            return true;
        }

        // Get current csv file line number
        public UInt32 GetLineNumber()
        {
            return currentLineNumber;
        }

        // Get next dc load settings entry from csv file
        public InputDataEntry GetNextEntry()
        {
            InputDataEntry csvDataEntry = null;

            if (csvStreamReader == null)
            {
                return null;
            }

            if (csvStreamReader.EndOfStream)
            {
                Console.WriteLine("End of file");
                return null;
            }

            var csvLine = csvStreamReader.ReadLine();

            Regex lineCheckReg = new Regex("^((?<UUID>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12});((0x){0,1})(?<DataToSend>(([0-9A-Fa-f]{2})+)))$");
            Match lineRegexMatch = lineCheckReg.Match(csvLine);

            if (!lineRegexMatch.Success)
            {
                Console.WriteLine("File " + currentFilePath + " line: " + currentLineNumber + " contains data in no supported format. Supported format: yyyy-mm-dd HH-MM-SS; <Power>");
            }
            else
            {
                csvDataEntry = new InputDataEntry();

                csvDataEntry.UUID = new Guid(lineRegexMatch.Groups["UUID"].Value);
                string dataToSendString = lineRegexMatch.Groups["DataToSend"].Value;

                csvDataEntry.DataToSend = Enumerable.Range(0, dataToSendString.Length)
                                                    .Where(x => x % 2 == 0)
                                                    .Select(x => Convert.ToByte(dataToSendString.Substring(x, 2), 16))
                                                    .ToArray();              
            }

            currentLineNumber++;
            return csvDataEntry;
        }
    }
}
