using System;
using System.Collections.Generic;
using RedlineLegends.Core;

namespace RedlineLegends.Save
{
    /// <summary>
    /// Upgrades the raw JSON of a save from <see cref="FromVersion"/> to FromVersion + 1.
    /// Working on the JSON string (not the typed object) lets a migration rename or restructure
    /// fields that no longer exist in the current SaveData class.
    /// </summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        string Migrate(string json);
    }

    /// <summary>Applies registered migrations in version order until the save is current.</summary>
    public sealed class SaveMigrationPipeline
    {
        private readonly Dictionary<int, ISaveMigration> _migrations = new Dictionary<int, ISaveMigration>();

        public void Register(ISaveMigration migration)
        {
            if (migration == null) throw new ArgumentNullException(nameof(migration));
            if (_migrations.ContainsKey(migration.FromVersion))
                throw new InvalidOperationException("A migration from version " + migration.FromVersion + " is already registered.");
            _migrations.Add(migration.FromVersion, migration);
        }

        /// <summary>Returns the migrated JSON, or null when a required migration step is missing.</summary>
        public string Migrate(string json, int fromVersion, int toVersion, out int reachedVersion)
        {
            reachedVersion = fromVersion;
            string current = json;
            while (reachedVersion < toVersion)
            {
                if (!_migrations.TryGetValue(reachedVersion, out var migration))
                {
                    GameLog.Error("No save migration registered from version " + reachedVersion + ".");
                    return null;
                }
                current = migration.Migrate(current);
                reachedVersion++;
            }
            return current;
        }
    }
}
