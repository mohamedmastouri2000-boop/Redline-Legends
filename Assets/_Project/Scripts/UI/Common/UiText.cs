namespace RedlineLegends.UI
{
    /// <summary>
    /// Text helpers restricted to glyphs the bundled Liberation Sans SDF font contains (Latin-1).
    /// Star/pip glyphs are drawn with ASCII until an icon font or sprite asset is added.
    /// </summary>
    public static class UiText
    {
        public static string Stars(int earned, int max = 3)
        {
            if (earned < 0) earned = 0;
            if (earned > max) earned = max;
            return new string('*', earned) + new string('-', max - earned);
        }

        public static string Pips(int filled, int max)
        {
            if (filled < 0) filled = 0;
            if (filled > max) filled = max;
            return new string('|', filled) + new string('.', max - filled);
        }
    }
}
