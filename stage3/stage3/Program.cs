using System;
using System.Data.SQLite;
using MongoDB.Bson;
using MongoDB.Driver;

class Program
{
    static void Main(string[] args)
    {
        // Connect to MongoDB using Compass connection string
        string connectionString = "mongodb://localhost:27017/log";
        MongoClient client = new MongoClient(connectionString);
        IMongoDatabase database = client.GetDatabase("log");
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("chrome");

        // Chrome History SQLite database path
        string chromeHistoryPath = @"C:\Users\Project1\AppData\Local\Google\Chrome\User Data\Default\History";

        // Connect to Chrome's History database
        using (SQLiteConnection connection = new SQLiteConnection($"Data Source={chromeHistoryPath};Version=3;New=False;Compress=True;"))
        {
            connection.Open();

            // Query Chrome's History database for URLs
            string query = "SELECT url, title, last_visit_time FROM urls";
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string url = reader["url"].ToString();
                        string title = reader["title"].ToString();
                        long visitTimeFileTime = Convert.ToInt64(reader["last_visit_time"]);

                        // Convert FILETIME to DateTime
                        DateTime visitTime = DateTime.FromFileTimeUtc(visitTimeFileTime);

                        // Create a BsonDocument with URL information
                        BsonDocument urlDocument = new BsonDocument
                        {
                            { "URL", url },
                            { "Title", title },
                            { "VisitTime", visitTime }
                        };

                        // Insert the URL document into the MongoDB collection
                        collection.InsertOne(urlDocument);
                    }
                }
            }
        }

        Console.WriteLine("Chrome history has been retrieved and stored in MongoDB.");
        Console.ReadLine();
    }
}