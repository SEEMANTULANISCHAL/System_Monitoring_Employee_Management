using System;
using System.Collections.Generic;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Management;

class Program
{
    static void Main(string[] args)
    {
        // Connect to MongoDB using Compass connection string
        string connectionString = "mongodb://localhost:27017/log";
        MongoClient client = new MongoClient(connectionString);
        IMongoDatabase database = client.GetDatabase("log");
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("stage2.0");

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

        // Query the software component information from the Windows Registry
        RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        if (softwareKey != null)
        {
            BsonDocument softwareComponentsDocument = new BsonDocument();
            int counter = 1;

            // Add the BIOS serial number as the first element in the softwareComponentsDocument
            softwareComponentsDocument.Add("BIOS_SerialNumber", serialNumber);

            foreach (string subKeyName in softwareKey.GetSubKeyNames())
            {
                using (RegistryKey subKey = softwareKey.OpenSubKey(subKeyName))
                {
                    string displayName = subKey.GetValue("DisplayName")?.ToString();
                    string version = subKey.GetValue("DisplayVersion")?.ToString();
                    string publisher = subKey.GetValue("Publisher")?.ToString();
                    string licensePeriod = subKey.GetValue("LicensePeriod")?.ToString();
                    Console.WriteLine(displayName);
                    Console.WriteLine(version);
                    Console.WriteLine(publisher);
                    // Check if any of the values are null
                    if (displayName != null && version != null && publisher != null)
                    {
                        string uniqueKey = $"{displayName} ({counter})";

                        // Create a BsonDocument with software component fields
                        BsonDocument softwareComponent = new BsonDocument
                        {
                            { "DisplayName", displayName },
                            { "Version", version },
                            { "Publisher", publisher }
                        };

                        // Use the uniqueKey as the key in the softwareComponentsDocument
                        softwareComponentsDocument[uniqueKey] = softwareComponent;
                        counter++;
                    }
                }
            }

            // Insert the software components document into the MongoDB collection
            collection.InsertOne(softwareComponentsDocument);
        }

        Console.WriteLine("Software component information has been retrieved and stored in MongoDB as a single document with a single ID using the display name as the key.");
        Console.ReadLine();
    }

    static string GetUniqueKey(string displayName, int counter)
    {
        return $"{displayName} ({counter})";
    }
}
