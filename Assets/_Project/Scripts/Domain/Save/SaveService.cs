using System;
using System.Security.Cryptography;
using System.Text;
using RedlineLegends.Core;
using UnityEngine;

namespace RedlineLegends.Save
{
    public enum SaveLoadResult
    {
        NotLoaded,
        NewProfile,
        Loaded,
        LoadedAndMigrated,
        RestoredFromBackup,
        CorruptResetToNew
    }

    /// <summary>
    /// Owns the in-memory <see cref="SaveData"/> and the only path to disk. The file is a small
    /// envelope: version + JSON payload + HMAC signature. The signature blocks casual editing and
    /// detects truncation; a failed check falls back to the .bak copy before giving up.
    /// </summary>
    public sealed class SaveService
    {
        [Serializable]
        private sealed class Envelope
        {
            public int v;
            public string payload;
            public string sig;
        }

        // Obfuscation-grade key: enough to stop text-editor tampering, not meant to resist a
        // determined attacker (nothing server-authoritative depends on it in v1).
        private static readonly byte[] SignatureKey = Encoding.UTF8.GetBytes("RedlineLegends.v1.save.hmac:7f3a91c2");

        private readonly ISaveStore _store;
        private readonly SaveMigrationPipeline _migrations;
        private readonly string _fileName;
        private readonly SettingsData _defaultSettings;
        private readonly int _startingCredits;

        public SaveData Data { get; private set; }
        public bool IsLoaded => Data != null;
        public SaveLoadResult LastLoadResult { get; private set; } = SaveLoadResult.NotLoaded;

        public event Action Loaded;
        public event Action Saved;

        public SaveService(ISaveStore store, SaveMigrationPipeline migrations, string fileName,
            SettingsData defaultSettings, int startingCredits)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _migrations = migrations ?? new SaveMigrationPipeline();
            _fileName = string.IsNullOrEmpty(fileName) ? "profile.sav" : fileName;
            _defaultSettings = defaultSettings;
            _startingCredits = startingCredits;
        }

        public void Load()
        {
            SaveData data = null;
            var result = SaveLoadResult.NewProfile;

            if (_store.Exists(_fileName))
            {
                data = TryParse(SafeRead(() => _store.Read(_fileName)), out bool migrated);
                if (data != null)
                {
                    result = migrated ? SaveLoadResult.LoadedAndMigrated : SaveLoadResult.Loaded;
                }
                else
                {
                    GameLog.Warn("Save file failed validation; trying backup.");
                    _store.Quarantine(_fileName);
                    if (_store.BackupExists(_fileName))
                    {
                        data = TryParse(SafeRead(() => _store.ReadBackup(_fileName)), out _);
                        if (data != null) result = SaveLoadResult.RestoredFromBackup;
                    }
                    if (data == null) result = SaveLoadResult.CorruptResetToNew;
                }
            }

            if (data == null)
                data = SaveData.CreateNew(_defaultSettings, _startingCredits);

            data.EnsureIntegrity();
            Data = data;
            LastLoadResult = result;
            GameLog.Info("Save loaded: " + result);
            Loaded?.Invoke();

            // Persist immediately after a migration/restore so the next launch is clean.
            if (result == SaveLoadResult.LoadedAndMigrated || result == SaveLoadResult.RestoredFromBackup
                || result == SaveLoadResult.CorruptResetToNew || result == SaveLoadResult.NewProfile)
                Save();
        }

        public void Save()
        {
            if (Data == null)
            {
                GameLog.Warn("Save() called before Load().");
                return;
            }
            try
            {
                Data.Version = SaveData.CurrentVersion;
                Data.LastSavedUtcTicks = DateTime.UtcNow.Ticks;
                string payload = JsonUtility.ToJson(Data, false);
                var envelope = new Envelope { v = SaveData.CurrentVersion, payload = payload, sig = Sign(payload) };
                _store.Write(_fileName, JsonUtility.ToJson(envelope, false));
                Saved?.Invoke();
            }
            catch (Exception e)
            {
                GameLog.Exception(e);
            }
        }

        /// <summary>Wipes progress and starts a fresh profile (settings are kept).</summary>
        public void ResetProfile()
        {
            var settings = Data != null ? Data.Settings.Clone() : _defaultSettings;
            Data = SaveData.CreateNew(settings, _startingCredits);
            Data.EnsureIntegrity();
            LastLoadResult = SaveLoadResult.NewProfile;
            Loaded?.Invoke();
            Save();
        }

        private static string SafeRead(Func<string> read)
        {
            try { return read(); }
            catch (Exception e)
            {
                GameLog.Warn("Save read failed: " + e.Message);
                return null;
            }
        }

        private SaveData TryParse(string envelopeJson, out bool migrated)
        {
            migrated = false;
            if (string.IsNullOrEmpty(envelopeJson)) return null;
            Envelope envelope;
            try { envelope = JsonUtility.FromJson<Envelope>(envelopeJson); }
            catch (Exception) { return null; }
            if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return null;
            if (!string.Equals(Sign(envelope.payload), envelope.sig, StringComparison.Ordinal))
            {
                GameLog.Warn("Save signature mismatch.");
                return null;
            }

            string payload = envelope.payload;
            if (envelope.v < SaveData.CurrentVersion)
            {
                payload = _migrations.Migrate(payload, envelope.v, SaveData.CurrentVersion, out int reached);
                if (payload == null || reached != SaveData.CurrentVersion) return null;
                migrated = true;
            }
            else if (envelope.v > SaveData.CurrentVersion)
            {
                // Newer save from a future build; JsonUtility ignores unknown fields so we can still read it.
                GameLog.Warn("Save is from a newer version (" + envelope.v + "); loading best-effort.");
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(payload);
                return data;
            }
            catch (Exception e)
            {
                GameLog.Warn("Save payload parse failed: " + e.Message);
                return null;
            }
        }

        private static string Sign(string payload)
        {
            using (var hmac = new HMACSHA256(SignatureKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
