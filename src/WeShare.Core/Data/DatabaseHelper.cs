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
            // Default to app directory; caller can override for testability
            _dbPath = dbPath ?? System.IO.Path.Combine(
                AppContext.BaseDirectory, "WeShare.db");
            InitializeDatabase();
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private void InitializeDatabase()
        {
            using var conn = OpenConnection();
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

                -- Add columns that may be missing in older DB files
                -- (SQLite ignores ALTER TABLE ADD COLUMN if the column already exists via error; we use separate statements)
            ";
            cmd.ExecuteNonQuery();

            // Ensure new columns exist in older databases (safe migration)
            TryAddColumn(conn, "Transfers", "PeerName",  "TEXT DEFAULT ''");
            TryAddColumn(conn, "Transfers", "Direction", "INTEGER DEFAULT 0");
            TryAddColumn(conn, "Transfers", "Timestamp", "TEXT DEFAULT ''");
        }

        private static void TryAddColumn(SqliteConnection conn, string table, string column, string definition)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                cmd.ExecuteNonQuery();
            }
            catch { /* column already exists */ }
        }

        // ── Write ──────────────────────────────────────────────────────────────
        public void SaveTransfer(FileTransferState state)
        {
            using var conn = OpenConnection();
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
            cmd.ExecuteNonQuery();
        }

        // ── Read ───────────────────────────────────────────────────────────────
        public List<FileTransferState> GetAllTransfers()
        {
            var list = new List<FileTransferState>();
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT FileId, SessionId, FileName, FilePath, PeerName, " +
                              "TotalBytes, TransferredBytes, Status, Direction, Timestamp " +
                              "FROM Transfers ORDER BY Timestamp DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
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
            return list;
        }

        public void DeleteTransfer(string fileId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Transfers WHERE FileId = $id";
            cmd.Parameters.AddWithValue("$id", fileId);
            cmd.ExecuteNonQuery();
        }

        public void ClearHistory()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Transfers";
            cmd.ExecuteNonQuery();
        }
    }
}
