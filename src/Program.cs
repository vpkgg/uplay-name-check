using System;
using System.IO;
using System.Threading.Tasks;

namespace UplayNameChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Program.MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            Log.Info("UplayNameChecker v2 by Vpkgg");
            R6dbAPI api = new R6dbAPI();
            
            if (!File.Exists("names.txt"))
                Log.Fatal("names.txt not found.");

            Log.Info("Reading names.txt...");
            string[] namesToCheck;

            try
            {
                namesToCheck = File.ReadAllLines("names.txt");
            }
            catch (Exception ex)
            {
                Log.Error("Could not read names.txt: " + ex.Message);
                return;
            }

            R6dbAPI r6db = new R6dbAPI();

            await Task.Delay(100);
            Log.Info("Starting to check...\n");

            foreach (var name in namesToCheck)
            {
                bool available = await r6db.IsNameAvailable(name);
                Log.Critical($"{name} is {(available ? "" : "not ")}available.");

                if (available)
                    File.AppendAllLines("availableNames.txt", new string[] { name });

                await Task.Delay(50); //give the api a little bit to breathe :)
            }
            Console.WriteLine();
            Log.Info("Written results to availableNames.txt.");
            Console.ReadLine();
        }
    }
}
