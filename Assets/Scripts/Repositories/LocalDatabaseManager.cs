using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

namespace HorizonFaser.Repositories
{
    public class LocalDatabaseManager
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public LocalDatabaseManager()
        {
            _dbPath = Path.Combine(Application.persistentDataPath, "HorizonFaserOffline.db");
            _connectionString = $"URI=file:{_dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(_dbPath))
                {
                    SqliteConnection.CreateFile(_dbPath);
                }

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS SyncQueueItem (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ActionType TEXT NOT NULL,
                                PayloadJson TEXT NOT NULL,
                                CreatedAt DATETIME NOT NULL,
                                IsProcessed INTEGER DEFAULT 0
                            );";
                        command.ExecuteNonQuery();
                    }
                }
                Debug.Log($"[LocalDatabaseManager] Database initialized at: {_dbPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalDatabaseManager] SQLite init error: {ex.Message}");
            }
        }

        // --- Operaciones de Sync Queue ---
        
        public Task EnqueueSyncActionAsync(string actionType, string payload)
        {
            return Task.Run(() => 
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO SyncQueueItem (ActionType, PayloadJson, CreatedAt, IsProcessed) VALUES (@actionType, @payload, @createdAt, 0)";
                        command.Parameters.Add(new SqliteParameter("@actionType", actionType));
                        command.Parameters.Add(new SqliteParameter("@payload", payload));
                        command.Parameters.Add(new SqliteParameter("@createdAt", DateTime.UtcNow.ToString("o")));
                        command.ExecuteNonQuery();
                    }
                }
                Debug.Log($"[LocalDatabaseManager] Enqueued sync action: {actionType}");
            });
        }

        public Task<List<SyncQueueItem>> GetPendingSyncActionsAsync()
        {
            return Task.Run(() => 
            {
                var list = new List<SyncQueueItem>();
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Id, ActionType, PayloadJson, CreatedAt, IsProcessed FROM SyncQueueItem WHERE IsProcessed = 0 ORDER BY CreatedAt ASC";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new SyncQueueItem
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ActionType = reader["ActionType"].ToString(),
                                    PayloadJson = reader["PayloadJson"].ToString(),
                                    CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                                    IsProcessed = false
                                });
                            }
                        }
                    }
                }
                return list;
            });
        }

        public Task MarkSyncActionProcessedAsync(int id)
        {
            return Task.Run(() => 
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE SyncQueueItem SET IsProcessed = 1 WHERE Id = @id";
                        command.Parameters.Add(new SqliteParameter("@id", id));
                        command.ExecuteNonQuery();
                    }
                }
            });
        }
    }

    public class SyncQueueItem
    {
        public int Id { get; set; }
        public string ActionType { get; set; }
        public string PayloadJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsProcessed { get; set; }
    }
}
