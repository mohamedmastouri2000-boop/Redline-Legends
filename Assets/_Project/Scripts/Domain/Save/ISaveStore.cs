using System;
using System.IO;
using RedlineLegends.Core;
using UnityEngine;

namespace RedlineLegends.Save
{
    /// <summary>Raw persistence. Abstracted so tests and a future cloud backup can substitute storage.</summary>
    public interface ISaveStore
    {
        bool Exists(string fileName);
        bool BackupExists(string fileName);
        string Read(string fileName);
        string ReadBackup(string fileName);
        /// <summary>Atomic write: content lands in a temp file, previous file becomes the backup.</summary>
        void Write(string fileName, string content);
        void Delete(string fileName);
        /// <summary>Keeps a copy of an unreadable file for diagnostics instead of destroying it.</summary>
        void Quarantine(string fileName);
    }

    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string _root;

        public FileSaveStore(string rootDirectory = null)
        {
            _root = string.IsNullOrEmpty(rootDirectory) ? Application.persistentDataPath : rootDirectory;
            Directory.CreateDirectory(_root);
        }

        private string PathFor(string fileName) => Path.Combine(_root, fileName);
        private string BackupPathFor(string fileName) => PathFor(fileName) + ".bak";
        private string TempPathFor(string fileName) => PathFor(fileName) + ".tmp";

        public bool Exists(string fileName) => File.Exists(PathFor(fileName));
        public bool BackupExists(string fileName) => File.Exists(BackupPathFor(fileName));

        public string Read(string fileName) => File.ReadAllText(PathFor(fileName));
        public string ReadBackup(string fileName) => File.ReadAllText(BackupPathFor(fileName));

        public void Write(string fileName, string content)
        {
            string path = PathFor(fileName);
            string temp = TempPathFor(fileName);
            string backup = BackupPathFor(fileName);

            File.WriteAllText(temp, content);
            if (File.Exists(path))
            {
                // File.Replace is atomic on the platforms we ship; it also produces the backup.
                File.Replace(temp, path, backup, true);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        public void Delete(string fileName)
        {
            string path = PathFor(fileName);
            if (File.Exists(path)) File.Delete(path);
            string backup = BackupPathFor(fileName);
            if (File.Exists(backup)) File.Delete(backup);
        }

        public void Quarantine(string fileName)
        {
            string path = PathFor(fileName);
            if (!File.Exists(path)) return;
            string target = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(path, target);
            }
            catch (Exception e)
            {
                GameLog.Warn("Save quarantine failed: " + e.Message);
            }
        }
    }
}
