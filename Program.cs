using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace PushToEMRS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool writeToLogFile = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("WriteToLogFile"));
            if (writeToLogFile)
            {
                byte[] inputBuffer = new byte[4096];
                Stream inputStream = Console.OpenStandardInput(inputBuffer.Length);
                Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, inputBuffer.Length));

                //delete the log file if exist (Do not remove this code)
                if (System.IO.File.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\APILog.txt"))
                {
                    System.IO.File.Delete(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\APILog.txt");
                }
                Console.WriteLine("Log File Location" + Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                //Open log file folder
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    FileName = "explorer.exe"
                };
                Process.Start(startInfo);


            }
            PushContactsToEMRS pushContactsToEMRS = new PushContactsToEMRS();
            await pushContactsToEMRS.StartProcessAsync();//comment this line if you dont want to execute

        }
    }
}
