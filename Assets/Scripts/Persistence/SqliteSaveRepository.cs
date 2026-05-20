using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SQLite;

namespace CyberBrass.Persistence
{
    /// <summary>
    /// SQLite implementation of ISaveRepository using sqlite-net-pcl.
    /// Handles physical connection to the DB file and executes CRUD queries asynchronously.
    /// </summary>
    public class SqliteSaveRepository : ISaveRepository
    {
        private SQLiteAsyncConnection _connection;
        private readonly string _dbPath;

        /// <summary>
        /// Creates a repository instance targeting the specified database path.
        /// </summary>
        /// <param name="dbName">The name of the database file (e.g. "cyberbrass.db").</param>
        public SqliteSaveRepository(string dbName)
        {
            // Resolve DB path in Application.persistentDataPath for cross-platform compliance
            _dbPath = Path.Combine(Application.persistentDataPath, dbName);
            Debug.Log($"[SqliteSaveRepository] Database path configured at: {_dbPath}");
        }

        /// <summary>
        /// Initializes the database and creates tables asynchronously if they don't already exist.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _connection = new SQLiteAsyncConnection(_dbPath);

                // Create tables
                await _connection.CreateTableAsync<PlayerProfile>();
                await _connection.CreateTableAsync<Loadout>();
                await _connection.CreateTableAsync<Unlock>();
                await _connection.CreateTableAsync<UserSetting>();
                await _connection.CreateTableAsync<RunHistory>();

                Debug.Log("[SqliteSaveRepository] SQLite tables initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Failed to initialize database: {ex.Message}");
                throw;
            }
        }

        public async Task<PlayerProfile> GetProfileAsync(int profileId)
        {
            try
            {
                return await _connection.Table<PlayerProfile>().Where(p => p.Id == profileId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error loading PlayerProfile {profileId}: {ex.Message}");
                return null;
            }
        }

        public async Task SaveProfileAsync(PlayerProfile profile)
        {
            try
            {
                // In single player, profile.Id will usually be 1. InsertOrReplace (upsert) handles updates seamlessly.
                await _connection.InsertOrReplaceAsync(profile);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error saving PlayerProfile: {ex.Message}");
                throw;
            }
        }

        public async Task<Loadout> GetLoadoutAsync(int profileId, int slot)
        {
            try
            {
                return await _connection.Table<Loadout>()
                    .Where(l => l.ProfileId == profileId && l.Slot == slot)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error loading loadout slot {slot} for profile {profileId}: {ex.Message}");
                return null;
            }
        }

        public async Task SaveLoadoutAsync(Loadout loadout)
        {
            try
            {
                await _connection.InsertOrReplaceAsync(loadout);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error saving Loadout: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Unlock>> GetUnlocksAsync(int profileId)
        {
            try
            {
                return await _connection.Table<Unlock>()
                    .Where(u => u.ProfileId == profileId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error loading unlocks for profile {profileId}: {ex.Message}");
                return new List<Unlock>();
            }
        }

        public async Task SaveUnlockAsync(Unlock unlock)
        {
            try
            {
                // Avoid duplicates by checking or using InsertOrReplace if keys are set
                await _connection.InsertOrReplaceAsync(unlock);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error saving Unlock: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserSetting>> GetSettingsAsync(int profileId)
        {
            try
            {
                return await _connection.Table<UserSetting>()
                    .Where(s => s.ProfileId == profileId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error loading settings for profile {profileId}: {ex.Message}");
                return new List<UserSetting>();
            }
        }

        public async Task SaveSettingAsync(UserSetting setting)
        {
            try
            {
                await _connection.InsertOrReplaceAsync(setting);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error saving setting: {ex.Message}");
                throw;
            }
        }

        public async Task<List<RunHistory>> GetRunHistoryAsync(int profileId)
        {
            try
            {
                return await _connection.Table<RunHistory>()
                    .Where(r => r.ProfileId == profileId)
                    .OrderByDescending(r => r.CompletedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error loading run history for profile {profileId}: {ex.Message}");
                return new List<RunHistory>();
            }
        }

        public async Task SaveRunHistoryAsync(RunHistory run)
        {
            try
            {
                await _connection.InsertAsync(run);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SqliteSaveRepository] Error saving RunHistory: {ex.Message}");
                throw;
            }
        }
    }
}
