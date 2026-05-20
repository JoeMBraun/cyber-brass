using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;

namespace CyberBrass.Persistence
{
    #region Database Models

    /// <summary>
    /// Stores the player's core identity, stats, and accumulated currency.
    /// </summary>
    public class PlayerProfile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Primary Key (typically 1 for single player profile)
        public string Name { get; set; }
        public string CreatedAt { get; set; }
        public float TotalPlaytime { get; set; }
        public int Currency { get; set; }
    }

    /// <summary>
    /// Defines weapon slots and active weapon IDs for the player's profile.
    /// </summary>
    public class Loadout
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ProfileId { get; set; }
        public int Slot { get; set; }
        public string PrimaryWeapon { get; set; }
        public string SecondaryWeapon { get; set; }
        public string ModsJson { get; set; }
    }

    /// <summary>
    /// Tracks which items/weapons have been unlocked.
    /// </summary>
    public class Unlock
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ProfileId { get; set; }
        public string ItemId { get; set; }
        public string UnlockedAt { get; set; }
    }

    /// <summary>
    /// A flexible key-value store for user/game settings (e.g., FOV, audio levels, keybinds).
    /// </summary>
    public class UserSetting
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ProfileId { get; set; }
        [Indexed]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Records details of completed or failed runs for statistics and progression tracking.
    /// </summary>
    public class RunHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ProfileId { get; set; }
        public string Level { get; set; }
        public float Duration { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public string CompletedAt { get; set; }
    }

    #endregion

    /// <summary>
    /// Interface that abstracts the saving and loading operations of the persistence layer.
    /// Allows swapping implementation between SQLite and other providers (e.g. cloud saves).
    /// </summary>
    public interface ISaveRepository
    {
        /// <summary>
        /// Initializes the storage (creates tables if they do not exist).
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Retrieves the player's profile.
        /// </summary>
        Task<PlayerProfile> GetProfileAsync(int profileId);

        /// <summary>
        /// Saves or updates the player's profile.
        /// </summary>
        Task SaveProfileAsync(PlayerProfile profile);

        /// <summary>
        /// Retrieves the loadout for a specific slot.
        /// </summary>
        Task<Loadout> GetLoadoutAsync(int profileId, int slot);

        /// <summary>
        /// Saves or updates a loadout.
        /// </summary>
        Task SaveLoadoutAsync(Loadout loadout);

        /// <summary>
        /// Retrieves all unlocks for a profile.
        /// </summary>
        Task<List<Unlock>> GetUnlocksAsync(int profileId);

        /// <summary>
        /// Adds a new item unlock.
        /// </summary>
        Task SaveUnlockAsync(Unlock unlock);

        /// <summary>
        /// Retrieves all settings for a profile.
        /// </summary>
        Task<List<UserSetting>> GetSettingsAsync(int profileId);

        /// <summary>
        /// Saves or updates a specific setting.
        /// </summary>
        Task SaveSettingAsync(UserSetting setting);

        /// <summary>
        /// Retrieves the run history list for statistics.
        /// </summary>
        Task<List<RunHistory>> GetRunHistoryAsync(int profileId);

        /// <summary>
        /// Adds a run history entry.
        /// </summary>
        Task SaveRunHistoryAsync(RunHistory run);
    }
}
