namespace CreateFiles
{
    using Microsoft.Crm.SdkTypeProxy;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            int numberOfElements = 50;
            int numberOfFiles = 1000;

            try
            {
                string directoryName = "Files";

                for (int i = 1; i <= numberOfFiles; i++)                
                {
                    Console.WriteLine($"Start file {i}");
                    var accounts = GetItems(i, numberOfElements);

                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using (StreamWriter file = File.CreateText($"Files\\{i}.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(file, accounts);
                    }
                    Console.WriteLine($"End OK file {i}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"End KO");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.WriteLine("Press any key to end");
                Console.ReadKey();
            }
        }

        private static List<account> GetItems(int fileNumber, int numberOfElements)
        {
            var list = new List<account>();

            for (int i = 1; i <= numberOfElements ; i++)
            {
                list.Add(new account()
                {
                    name = $"Robert{fileNumber}"
                });
            }

            return list;
        }
    }
}
