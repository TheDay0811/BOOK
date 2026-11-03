using System;
using System.IO;
using System.Net.Sockets;
using Mongo2Go;

namespace BOOK.Services
{
    public static class MongoRunnerService
    {
        private static MongoDbRunner? _runner;

        public static string EnsureRunning(string? connectionString, string contentRootPath)
        {
            if (!string.IsNullOrWhiteSpace(connectionString) && 
                !connectionString.Contains("127.0.0.1:27017") && 
                !connectionString.Contains("localhost:27017"))
            {
                // External MongoDB (e.g. MongoDB Atlas)
                return connectionString;
            }

            // Check if MongoDB is already listening on port 27017
            if (IsPortOpen("127.0.0.1", 27017))
            {
                return "mongodb://127.0.0.1:27017";
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
                return _runner.ConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoRunner] Could not start embedded Mongo2Go: {ex.Message}. Falling back to default URI.");
                return "mongodb://127.0.0.1:27017";
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
