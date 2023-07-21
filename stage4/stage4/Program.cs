using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using MongoDB.Bson;
using MongoDB.Driver;

class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    static BsonDocument applicationUsageDocument;
    static Timer timer;
    static MongoClient client;
    static IMongoDatabase database;
    static IMongoCollection<BsonDocument> collection;

    static void Main()
    {
        string connectionString = "mongodb://localhost:27017"; // Replace with your MongoDB connection string
        string databaseName = "YourDatabase"; // Replace with your database name
        string collectionName = "ApplicationUsage"; // Replace with your collection name

        client = new MongoClient(connectionString);
        database = client.GetDatabase(databaseName);
        collection = database.GetCollection<BsonDocument>(collectionName);

        applicationUsageDocument = new BsonDocument();

        timer = new Timer();
        timer.Interval = 1000; // 1 second
        timer.Elapsed += TimerElapsed;
        timer.Start();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        timer.Stop();
        timer.Dispose();
    }

    static void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        IntPtr foregroundWindowHandle = GetForegroundWindow();
        string foregroundWindowTitle = GetWindowTitle(foregroundWindowHandle);

        // Check if the foreground window is different from the previous one
        if (!applicationUsageDocument.Contains(foregroundWindowTitle))
        {
            applicationUsageDocument.Add(foregroundWindowTitle, new BsonDocument
            {
                { "EntryTime", DateTime.Now },
                { "UsageDuration", new BsonInt64(0) }
            });
        }
        else
        {
            // Update the usage duration for the current application
            BsonDocument applicationInfo = applicationUsageDocument[foregroundWindowTitle].AsBsonDocument;
            TimeSpan currentUsageDuration = DateTime.Now - applicationInfo["EntryTime"].AsDateTime;
            applicationInfo["UsageDuration"] = new BsonInt64(currentUsageDuration.Ticks);
        }

        // Print the application usage information
        Console.Clear();
        Console.WriteLine("Application Usage:");

        foreach (BsonElement element in applicationUsageDocument)
        {
            string applicationTitle = element.Name;
            BsonDocument applicationInfo = element.Value.AsBsonDocument;
            DateTime entryTime = applicationInfo["EntryTime"].AsDateTime;
            TimeSpan usageDuration = TimeSpan.FromTicks(applicationInfo["UsageDuration"].AsInt64);

            Console.WriteLine($"Application: {applicationTitle}");
            Console.WriteLine($"Entry Time: {entryTime}");
            Console.WriteLine($"Usage Duration: {usageDuration}");
            Console.WriteLine();
        }

        // Insert or update the data in the MongoDB collection
        collection.ReplaceOne(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Empty), applicationUsageDocument, new UpdateOptions { IsUpsert = true });
    }

    static string GetWindowTitle(IntPtr handle)
    {
        int length = GetWindowTextLength(handle);
        StringBuilder titleBuilder = new StringBuilder(length + 1);
        GetWindowText(handle, titleBuilder, length + 1);
        string windowTitle = titleBuilder.ToString();
        return windowTitle;
    }
}
