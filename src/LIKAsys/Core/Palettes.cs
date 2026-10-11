using System;
using System.Collections.Generic;

namespace LIKAsys.Core
{
    /// <summary>
    /// Colour sets for the capsule themes. One colour per reading, so a card reads like
    /// an RGB build instead of nine rows of the same blue. Two themes that use different
    /// palettes do not look like each other, which is the whole point of having them.
    /// </summary>
    public static class Palettes
    {
        /// <summary>The eleven readings, in the order they usually appear.</summary>
        public static readonly string[] Keys =
        {
            "cpu", "gpu", "ram", "disk", "diskio", "fps", "low", "frame", "net", "ping", "uptime"
        };

        private static readonly Dictionary<string, string[]> Sets =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // the look in the reference: cool blue cpu, green gpu, violet ram, hot orange fps
            ["neon"] = new[]
            {
                "#2E9BFF", "#2BE07A", "#B14BFF", "#FF49C3", "#FF7A2F",
                "#FF9A1F", "#FFD23F", "#19E8FF", "#19E8FF", "#5BE8C0", "#8AA0BF"
            },
            // everything cold, for people who hate a rainbow
            ["ice"] = new[]
            {
                "#5AC8FF", "#49E0E8", "#7E9BFF", "#9A86FF", "#58D6C0",
                "#43C8FF", "#8FD4FF", "#6FE3D2", "#4FD1FF", "#7BC0FF", "#9FB6D6"
            },
            // warm metal, amber and copper
            ["fire"] = new[]
            {
                "#FF8A3D", "#FF5B4A", "#FFC247", "#FF7A5C", "#FF9E52",
                "#FFD93D", "#FFB020", "#FF6F3C", "#FFA94D", "#FFCF6B", "#E0A96D"
            },
            // soft, low saturation, for a bright desktop
            ["pastel"] = new[]
            {
                "#7FB6F2", "#86D9A8", "#C0A4F0", "#F2A6C8", "#F0B995",
                "#F5C86B", "#F5DD8A", "#8FD8E6", "#8AC6E8", "#9BDCC6", "#B8C4D4"
            },
            // one hue, different strengths. The quiet capsule theme.
            ["mono"] = new[]
            {
                "#E8F1FF", "#C8D8F0", "#A9C0E0", "#8EA8CE", "#7793BC",
                "#F0F6FF", "#D4E2F5", "#B6C9E4", "#9CB2D0", "#8AA0BF", "#73879F"
            },
            // the Kosovo flag, blue and gold
            ["kosova"] = new[]
            {
                "#2456A6", "#D4A017", "#3A72C8", "#E8B52E", "#2456A6",
                "#D4A017", "#E8B52E", "#3A72C8", "#2456A6", "#D4A017", "#8AA0BF"
            },
        };

        /// <summary>Names of every palette, for the picker.</summary>
        public static IEnumerable<string> Names => Sets.Keys;

        public static bool Has(string palette) =>
            !string.IsNullOrEmpty(palette) && Sets.ContainsKey(palette);

        /// <summary>
        /// Colour for one reading. Unknown palette or unknown key simply returns null,
        /// and the caller keeps the plain accent, so a typo can never blank out a row.
        /// </summary>
        public static string Color(string palette, string key)
        {
            if (string.IsNullOrEmpty(palette) || string.IsNullOrEmpty(key)) return null;
            if (!Sets.TryGetValue(palette, out var arr)) return null;

            for (int i = 0; i < Keys.Length && i < arr.Length; i++)
                if (string.Equals(Keys[i], key, StringComparison.OrdinalIgnoreCase))
                    return arr[i];
            return null;
        }
    }
}
