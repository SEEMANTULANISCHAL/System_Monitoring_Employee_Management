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

    static Dictionary<IntPtr, ApplicationInfo> runningApplications;
    static Timer timer;
    static MongoClient client;
    static IMongoDatabase database;
    static IMongoCollection<ApplicationInfo> collection;

    static void Main()
    {
        string connectionString = "mongodb://localhost:27017"; // Replace with your MongoDB connection string
        string databaseName = "YourDatabase"; // Replace with your database name
        string collectionName = "ApplicationUsage"; // Replace with your collection name

        client = new MongoClient(connectionString);
        database = client.GetDatabase(databaseName);
        collection = database.GetCollection<ApplicationInfo>(collectionName);

        runningApplications = new Dictionary<IntPtr, ApplicationInfo>();

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
        if (!runningApplications.ContainsKey(foregroundWindowHandle))
        {
            // Store the entry time for the new application
            ApplicationInfo application = new ApplicationInfo
            {
                Id = ObjectId.GenerateNewId(),
                EntryTime = DateTime.Now,
                MainWindowTitle = foregroundWindowTitle
            };
            runningApplications[foregroundWindowHandle] = application;
        }
        else
        {
            // Update the usage duration for the current application
            ApplicationInfo application = runningApplications[foregroundWindowHandle];
            application.UsageDuration = DateTime.Now - application.EntryTime;
        }

        // Print the application usage information
        Console.Clear();
        Console.WriteLine("Running Applications:");

        foreach (var kvp in runningApplications)
        {
            Console.WriteLine($"Application: {kvp.Value.MainWindowTitle}");
            Console.WriteLine($"Entry Time: {kvp.Value.EntryTime}");
            Console.WriteLine($"Usage Duration: {kvp.Value.UsageDuration}");
            Console.WriteLine();
        }

        // Insert the new data into the MongoDB collection
        collection.InsertMany(runningApplications.Values);
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

class ApplicationInfo
{
    public ObjectId Id { get; set; }
    public DateTime EntryTime { get; set; }
    public string MainWindowTitle { get; set; }
    public TimeSpan UsageDuration { get; set; }
}