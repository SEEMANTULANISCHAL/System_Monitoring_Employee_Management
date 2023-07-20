using System;
using Microsoft.Win32;
using System.Timers;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Management;
class Program
{
    static void Main(string[] args)
    {
        // Connect to MongoDB using Compass connection string

        string connectionString = "mongodb://localhost:27017/log";
        MongoClient client = new MongoClient(connectionString);
        IMongoDatabase database = client.GetDatabase("log");
        IMongoCollection<SoftwareComponent> collection = database.GetCollection<SoftwareComponent>("Stage2.1");
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");


        var serialNumber = string.Empty;
        var biosAttributes = new BsonDocument();
        foreach (ManagementObject obj in searcher.Get())
        {

            string biosSerialNumber = obj["SerialNumber"]?.ToString();


            Console.WriteLine("BIOS Serial Number: " + biosSerialNumber);
            Console.WriteLine();

            // Store the BIOS attributes in the biosAttributes document

            biosAttributes.Add("SerialNumber", biosSerialNumber);
            serialNumber = biosSerialNumber;
        }
        // Create a timer to poll for registry changes every 1 second
        Timer timer = new Timer(1000);
        timer.Elapsed += (sender, e) =>
        {
            // Check if the registry key has changed
            if (RegistryKeyChanged())
            {
                // Retrieve the updated software component information from the registry
                string displayName = GetRegistryValue("DisplayName");
                string version = GetRegistryValue("DisplayVersion");
                string publisher = GetRegistryValue("Publisher");

                // Check if any of the values are null or empty
                if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(publisher))
                {
                    // Create a SoftwareComponent object
                    SoftwareComponent softwareComponent = new SoftwareComponent
                    {
                        DisplayName = displayName,
                        Version = version,
                        Publisher = publisher
                    };

                    // Insert or update the software component in the MongoDB collection
                    var filter = Builders<SoftwareComponent>.Filter.Eq("DisplayName", displayName);
                    collection.ReplaceOne(filter, softwareComponent, new ReplaceOptions { IsUpsert = true });
                }
            }
        };

        // Start the timer
        timer.Start();

        Console.WriteLine("Registry change monitoring has started. Press any key to stop...");
        Console.ReadLine();

        // Stop the timer
        timer.Stop();

        Console.WriteLine("Registry change monitoring has stopped.");
        Console.ReadKey();
    }

    // Check if the registry key has changed by comparing the last write time
    static bool RegistryKeyChanged()
    {
        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        DateTime lastWriteTime = GetRegistryKeyLastWriteTime(registryKey);
        return lastWriteTime != DateTime.MinValue;
    }

    // Get the last write time of the registry key
    static DateTime GetRegistryKeyLastWriteTime(RegistryKey registryKey)
    {
        object value = registryKey.GetValue("DisplayName");
        if (value != null)
        {
            RegistryValueKind valueKind = registryKey.GetValueKind("DisplayName");
            if (valueKind == RegistryValueKind.String)
            {
                return (DateTime)registryKey.GetValue("LastWriteTime");
            }
        }
        return DateTime.MinValue;
    }

    // Get the value of a specific registry key value
    static string GetRegistryValue(string valueName)
    {
        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        return registryKey?.GetValue(valueName)?.ToString();
    }
    class SoftwareComponent
    {
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
    }

}