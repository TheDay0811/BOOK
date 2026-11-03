using System;
using System.IO;
using System.Net.Sockets;
using Mongo2Go;

namespace BOOK.Services
{
    public static class MongoRunnerService
    {
        private static MongoDbRunner? _runner;
        public static string ActiveConnectionString { get; private set; } = "mongodb://127.0.0.1:27017";

        public static string EnsureRunning(string? connectionString, string contentRootPath)
        {
            if (!string.IsNullOrWhiteSpace(connectionString) && 
                !connectionString.Contains("127.0.0.1:27017") && 
                !connectionString.Contains("localhost:27017"))
            {
                // External MongoDB (e.g. MongoDB Atlas)
                ActiveConnectionString = connectionString;
                return connectionString;
            }

            // Check if MongoDB is already listening on port 27017
            if (IsPortOpen("127.0.0.1", 27017))
            {
                ActiveConnectionString = "mongodb://127.0.0.1:27017";
                return ActiveConnectionString;
            }

            try
            {
                var dataDir = Path.Combine(contentRootPath, ".mongodb_data");
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                _runner = MongoDbRunner.Start(
                    dataDirectory: dataDir
                );
                ActiveConnectionString = _runner.ConnectionString;
                return _runner.ConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoRunner] Could not start embedded Mongo2Go: {ex.Message}. Falling back to default URI.");
                ActiveConnectionString = "mongodb://127.0.0.1:27017";
                return ActiveConnectionString;
            }
        }

        private static bool IsPortOpen(string host, int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                if (!success) return false;
                client.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
