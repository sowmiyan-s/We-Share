using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbPath = System.IO.Path.Combine(appData, "WeShare", "WeShare.db");
        Console.WriteLine($"Database Path: {dbPath}");
        if (!System.IO.File.Exists(dbPath))
        {
            Console.WriteLine("Database file does not exist.");
            return;
        }

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT FileId, FileName, FilePath, TotalBytes, Status, Direction, Timestamp FROM Transfers";

        using var reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
        {
            count++;
            Console.WriteLine($"Record {count}:");
            Console.WriteLine($"  FileId: {reader.GetString(0)}");
            Console.WriteLine($"  FileName: {reader.GetString(1)}");
            Console.WriteLine($"  FilePath: {reader.GetString(2)}");
            Console.WriteLine($"  TotalBytes: {reader.GetInt64(3)}");
            Console.WriteLine($"  Status: {reader.GetInt32(4)}");
            Console.WriteLine($"  Direction: {reader.GetInt32(5)}");
            Console.WriteLine($"  TimestampRaw: {reader.GetString(6)}");
            var ts = DateTime.TryParse(reader.GetString(6), out var dt) ? dt : DateTime.MinValue;
            Console.WriteLine($"  ParsedTimestampKind: {ts.Kind}");
            Console.WriteLine($"  ToLocalTime: {ts.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        }
        Console.WriteLine($"Total records read: {count}");
    }
}
