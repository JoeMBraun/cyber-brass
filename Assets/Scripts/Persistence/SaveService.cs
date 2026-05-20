using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CyberBrass.Core;

namespace CyberBrass.Persistence
{
    /// <summary>
    /// SaveService provides high-level APIs for loading, caching, modifying, and flushing player save data.
    /// Handles caching to prevent redundant database hits and implements a dirty-flag flushing model.
    /// Registers itself with the ServiceLocator upon initialization.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveService : MonoBehaviour
    {
        private static SaveService _instance;
        private ISaveRepository _repository;

        // Current cached state
        public PlayerProfile ActiveProfile { get; private set; }
        private readonly Dictionary<int, Loadout> _cachedLoadouts = new Dictionary<int, Loadout>();
        private readonly List<Unlock> _cachedUnlocks = new List<Unlock>();
        private readonly Dictionary<string, UserSetting> _cachedSettings = new Dictionary<string, UserSetting>();

        private bool _isDirty;
        private bool _isSaving;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Use SqliteSaveRepository as the concrete repository implementation
            _repository = new SqliteSaveRepository("cyberbrass.db");

            ServiceLocator.Register<SaveService>(this);
        }

        private async void Start()
        {
            await InitializeServiceAsync();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<SaveService>();
                _instance = null;
            }
        }

        /// <summary>
        /// Initializes the underlying save repository and creates tables if necessary.
        /// Loads or creates the default profile (Id = 1) and caches settings.
        /// </summary>
        public async Task InitializeServiceAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[SaveService] Initializing save system...");
            await _repository.InitializeAsync();

            // Load default player profile (Id = 1)
            ActiveProfile = await _repository.GetProfileAsync(1);
            if (ActiveProfile == null)
            {
                Debug.Log("[SaveService] No profile found. Creating a new default profile...");
                ActiveProfile = new PlayerProfile
                {
                    Id = 1,
                    Name = "CyberShooter",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    TotalPlaytime = 0f,
                    Currency = 0
                };
                await _repository.SaveProfileAsync(ActiveProfile);
            }

            // Populate settings cache
            List<UserSetting> settings = await _repository.GetSettingsAsync(1);
            foreach (var setting in settings)
            {
                _cachedSettings[setting.Key] = setting;
            }

            // Populate unlocks cache
            List<Unlock> unlocks = await _repository.GetUnlocksAsync(1);
            _cachedUnlocks.Clear();
            _cachedUnlocks.AddRange(unlocks);

            IsInitialized = true;
            Debug.Log($"[SaveService] Save system initialized for profile: {ActiveProfile.Name}");
        }

        /// <summary>
        /// Adds currency to the active profile and marks the save data as dirty.
        /// </summary>
        /// <param name="amount">Amount of currency to add.</param>
        public void AddCurrency(int amount)
        {
            if (!IsInitialized) return;

            ActiveProfile.Currency += amount;
            _isDirty = true;
            Debug.Log($"[SaveService] Added {amount} currency. Total: {ActiveProfile.Currency}");
        }

        /// <summary>
        /// Checks if an item/weapon is unlocked.
        /// </summary>
        /// <param name="itemId">Unique item identifier.</param>
        /// <returns>True if the item is unlocked, false otherwise.</returns>
        public bool IsUnlocked(string itemId)
        {
            if (!IsInitialized) return false;

            return _cachedUnlocks.Exists(u => u.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Unlocks an item/weapon and marks the data as dirty.
        /// </summary>
        /// <param name="itemId">Unique item identifier.</param>
        public async Task UnlockItemAsync(string itemId)
        {
            if (!IsInitialized) return;

            if (IsUnlocked(itemId)) return;

            var unlock = new Unlock
            {
                ProfileId = ActiveProfile.Id,
                ItemId = itemId,
                UnlockedAt = DateTime.UtcNow.ToString("o")
            };

            _cachedUnlocks.Add(unlock);
            await _repository.SaveUnlockAsync(unlock);
            Debug.Log($"[SaveService] Unlocked item: {itemId}");
        }

        /// <summary>
        /// Retrieves a user setting by its key.
        /// </summary>
        /// <param name="key">The key of the setting.</param>
        /// <param name="defaultValue">Fallback value if the setting doesn't exist.</param>
        /// <returns>The setting value as a string.</returns>
        public string GetSetting(string key, string defaultValue)
        {
            if (_cachedSettings.TryGetValue(key, out var setting))
            {
                return setting.Value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Saves or updates a setting in the cache and repository.
        /// </summary>
        /// <param name="key">Setting key.</param>
        /// <param name="value">Setting value.</param>
        public async Task SaveSettingAsync(string key, string value)
        {
            if (!IsInitialized) return;

            if (_cachedSettings.TryGetValue(key, out var setting))
            {
                if (setting.Value == value) return;
                setting.Value = value;
            }
            else
            {
                setting = new UserSetting
                {
                    ProfileId = ActiveProfile.Id,
                    Key = key,
                    Value = value
                };
                _cachedSettings[key] = setting;
            }

            await _repository.SaveSettingAsync(setting);
            Debug.Log($"[SaveService] Setting updated - {key}: {value}");
        }

        /// <summary>
        /// Flushes any pending dirty profile updates (like play time or currency) to disk.
        /// </summary>
        /// <param name="force">If true, ignores dirty check and forces write.</param>
        public async Task FlushChangesAsync(bool force = false)
        {
            if (!IsInitialized || _isSaving) return;
            if (!_isDirty && !force) return;

            _isSaving = true;
            Debug.Log("[SaveService] Flushing dirty cache modifications to disk...");

            try
            {
                await _repository.SaveProfileAsync(ActiveProfile);
                _isDirty = false;
                Debug.Log("[SaveService] Flush complete.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Flush failed: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Increments play time. Normally called by a timer or update cycle.
        /// </summary>
        /// <param name="seconds">Seconds elapsed.</param>
        public void IncrementPlaytime(float seconds)
        {
            if (!IsInitialized) return;

            ActiveProfile.TotalPlaytime += seconds;
            _isDirty = true;
        }

        /// <summary>
        /// Records a completed run directly to the database.
        /// </summary>
        public async Task RecordRunAsync(string levelName, float duration, int kills, int deaths)
        {
            if (!IsInitialized) return;

            var run = new RunHistory
            {
                ProfileId = ActiveProfile.Id,
                Level = levelName,
                Duration = duration,
                Kills = kills,
                Deaths = deaths,
                CompletedAt = DateTime.UtcNow.ToString("o")
            };

            await _repository.SaveRunHistoryAsync(run);
            Debug.Log($"[SaveService] Recorded run on level {levelName}: {kills} kills, {deaths} deaths, {duration}s duration");
        }
    }
}
