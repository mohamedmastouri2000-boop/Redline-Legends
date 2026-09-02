using System;

namespace RedlineLegends.Utilities
{
    /// <summary>
    /// Content ids are lowercase snake_case and never change once shipped, because save files
    /// store them. Display names are free to change and are never used as keys.
    /// </summary>
    public static class StableId
    {
        public static bool IsValid(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        public static void Require(string id, string owner)
        {
            if (!IsValid(id))
                throw new InvalidOperationException(owner + ": id '" + id + "' is not a valid stable id (lowercase a-z, 0-9, '_').");
        }
    }
}
