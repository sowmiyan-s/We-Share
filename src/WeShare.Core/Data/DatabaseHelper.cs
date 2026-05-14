using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using WeShare.Core.Models;

namespace WeShare.Core.Data
{
    public class DatabaseHelper
    {
        private readonly string _dbPath;

        public DatabaseHelper(string? dbPath = null)
        {
            if (dbPath == null)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string folder = System.IO.Path.Combine(appData, "WeShare");
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                _dbPath = System.IO.Path.Combine(folder, "WeShare.db");
            }
            else
            {
                _dbPath = dbPath;
            }
            InitializeDatabase();
        }

        private async Task<SqliteConnection> OpenConnectionAsync()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            return conn;
        }

        private async void InitializeDatabase()
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Transfers (
                        FileId           TEXT PRIMARY KEY,
                        SessionId        TEXT,
                        FileName         TEXT,
                        FilePath         TEXT,
                        PeerName         TEXT DEFAULT '',
                        TotalBytes       INTEGER,
                        TransferredBytes INTEGER,
                        Status           INTEGER,
                        Direction        INTEGER DEFAULT 0,
                        Timestamp        TEXT DEFAULT ''
                    );
                ";
                await cmd.ExecuteNonQueryAsync();

                // Ensure new columns exist in older databases (safe migration)
                await TryAddColumnAsync(conn, "Transfers", "PeerName",  "TEXT DEFAULT ''");
                await TryAddColumnAsync(conn, "Transfers", "Direction", "INTEGER DEFAULT 0");
                await TryAddColumnAsync(conn, "Transfers", "Timestamp", "TEXT DEFAULT ''");
            }
            catch (Exception ex) { Console.WriteLine($"[DB] Init failed: {ex.Message}"); }
        }

        private static async Task TryAddColumnAsync(SqliteConnection conn, string table, string column, string definition)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* column already exists */ }
        }

        // ── Write ──────────────────────────────────────────────────────────────
        public async Task SaveTransferAsync(FileTransferState state)
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO Transfers
                    (FileId, SessionId, FileName, FilePath, PeerName,
                     TotalBytes, TransferredBytes, Status, Direction, Timestamp)
                    VALUES
                    ($id, $sessionId, $name, $path, $peer,
                     $total, $transferred, $status, $dir, $ts)
                ";
                cmd.Parameters.AddWithValue("$id",          state.FileId);
                cmd.Parameters.AddWithValue("$sessionId",   state.SessionId ?? string.Empty);
                cmd.Parameters.AddWithValue("$name",        state.FileName);
                cmd.Parameters.AddWithValue("$path",        state.FilePath);
                cmd.Parameters.AddWithValue("$peer",        state.PeerName ?? string.Empty);
                cmd.Parameters.AddWithValue("$total",       state.TotalBytes);
                cmd.Parameters.AddWithValue("$transferred", state.TransferredBytes);
                cmd.Parameters.AddWithValue("$status",      (int)state.Status);
                cmd.Parameters.AddWithValue("$dir",         (int)state.Direction);
                cmd.Parameters.AddWithValue("$ts",          state.Timestamp.ToString("o"));
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { Console.WriteLine($"[DB] Save failed: {ex.Message}"); }
        }

        // ── Read ───────────────────────────────────────────────────────────────
        public async Task<List<FileTransferState>> GetAllTransfersAsync()
        {
            var list = new List<FileTransferState>();
            try
            {
                using var conn = await OpenConnectionAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT FileId, SessionId, FileName, FilePath, PeerName, " +
                                  "TotalBytes, TransferredBytes, Status, Direction, Timestamp " +
                                  "FROM Transfers ORDER BY Timestamp DESC";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var ts = reader.IsDBNull(9) ? DateTime.MinValue
                             : DateTime.TryParse(reader.GetString(9), out var dt) ? dt : DateTime.MinValue;

                    list.Add(new FileTransferState
                    {
                        FileId           = reader.GetString(0),
                        SessionId        = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        FileName         = reader.GetString(2),
                        FilePath         = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        PeerName         = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        TotalBytes       = reader.GetInt64(5),
                        TransferredBytes = reader.GetInt64(6),
                        Status           = (TransferStatus)reader.GetInt32(7),
                        Direction        = reader.IsDBNull(8) ? TransferDirection.Received : (TransferDirection)reader.GetInt32(8),
                        Timestamp        = ts
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[DB] Read failed: {ex.Message}"); }
            return list;
        }

        public async Task DeleteTransferAsync(string fileId)
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Transfers WHERE FileId = $id";
                cmd.Parameters.AddWithValue("$id", fileId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { Console.WriteLine($"[DB] Delete failed: {ex.Message}"); }
        }

        public async Task ClearHistoryAsync()
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Transfers";
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { Console.WriteLine($"[DB] Clear failed: {ex.Message}"); }
        }
    }
}
