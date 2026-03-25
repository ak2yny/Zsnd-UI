using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Zsnd_UI.lib
{
    /// <summary>
    /// Zsound hash event definitions
    /// </summary>
    public static class Events
    {
        private const int MPowersMultiplier = 12;
        private const int RandomMultiiplier = 21;
        private static readonly string[] MPowers =
        [
            "CHARGE",
            "POWER",
            "IMPACT",
            "CHARGE_LOOP",
            "LOOP"
            // Rarer:
            // "RELEASE",
            // "THROW",
            // "END", "POWER_END" // +
            // "EXPLODE",
            // "INTRO",
            // "MAX", "MAX_LOOP"
            // "HOLD",
        ];
        private static readonly string[] M =
        [
            "DEATH",
            "FLYBEGIN",
            "FLYEND",
            "JUMP",
            "LAND",
            "PAIN",
            "PICKUP",
            "PUNCHED",
            "STRUGGLE",
            "THROW",
            // Rare:
            "LIFT",
            "LIFT_SHORT",
            "XTREME_LAUNCH",
            // Used in mods:
            "EXPLODE",
            "DRAW_GUN",
            "HOLSTER_GUN",
            "MUSIC",
            "STEP",
            "TELEPORT",
            "WEB_ZIP"
        ];
        public static string[] Voice { get; } = [
            "BORED",
            "CANTGO",
            "CMDATTACKANY",
            "CMDATTACKTARGET",
            "CMDFOLLOW",
            "EPITAPH",
            "LEVELUP",
            "LOWHEALTH",
            "NOPOWER",
            "NOWORK",
            "RESPAFFIRM",
            "STATS",
            "TAUNTKD",
            "THROWTAUNT",
            "TOOHEAVY",
            "VICTORY",
            "XTREME",
            // Enemies only:
            "ISEEYOU",
            "TAUNT",
            "YELL",
            // XML games:
            "BANTER_BISHOP",
            "BANTER_COLOSSUS",
            "BANTER_CYCLOPS",
            "BANTER_GAMBIT",
            "BANTER_ICEMAN",
            "BANTER_IRONMAN",
            "BANTER_JUGGERNAUT",
            "BANTER_MAGNETO",
            "BANTER_NIGHTCRAWLER",
            "BANTER_PHOENIX",
            "BANTER_ROGUE",
            "BANTER_SCARLETWITCH",
            "BANTER_STORM",
            "BANTER_SUNFIRE",
            "BANTER_TOAD",
            "BANTER_WOLVERINE",
            "BANTER_BLADE",
            "BANTER_CAP",
            "BANTER_DD",
            "BANTER_DEADPOOL",
            "BANTER_ELEKTRA",
            "BANTER_GHOSTRIDER",
            "BANTER_PANTHER",
            "BANTER_STRANGE",
            "BANTER_TORCH",
            "BOSSTAUNT",
            "CANTTALK",
            "INCOMING",
            "LAUGH",
            "LOCKED",
            "SIGHT",
            "SOLO_BEGIN",
            "SOLO_END",
            "XTREME2",
            "6_STATS"
        ];
        public static string[] Master { get; private set; }
        public static readonly string[] MRand = new string[(M.Length + MPowers.Length * MPowersMultiplier) * RandomMultiiplier];
        public static readonly string[] VRand = new string[Voice.Length * RandomMultiiplier];
        public const string MusSfxs = "ACX";
        //public static readonly string[] Music = new string[7];
        public static readonly string[] Music =
        [
            "A",
            "C",
            "X",
            "MUSIC/CUES/IN",  //
            "MUSIC/CUES/OUT", // covered by JsonHashes
            "STATSCREEN/AMB", //
            "OTHER" // Credits, Menu
        ];
        // "MUSIC/MENU/MENU_{AMB / MUSIC}" rare
        // "MUSIC/CREDITS{_ALT}" rare
        // "MUSIC/MUSIC_CUES/" Unknown, possibly unused

        public enum Category
        {
            OTHER,
            CHAR,
            CHARACTER,
            COMMON,
            MUSIC,
            VOICE
        };
        // Possible Enums To Add:
        // DEATH_STYLE/DS_{BIOMETAL|CARPET|CONCRETE|CONFETTI|ELECTRICAL|ELECTRIC|ENERGY|EXPLOSIVE|GLASS|ICE|(HOLLOW_METAL no sfx)|(METAL no sfx)|METAL_BIO|METAL_HOLLOW|METAL__HOLLOW|METAL|PLANTSNOW|PLATSNOW|STONE|SULPHER|TILE|UNDERWATER|UPHOLSTERY|WOODHOLLOW|WOOD}_{L|M|S}
        // DEATH_STYLE/T_{ATLANTIS|DOOM|GENOSHA|HELICARRIER|MANDARIN|MURDERWORLD|OMEGA|SHIAR|SKRULL}
        // DEATH_STYLE/DESTRUCTION/DS_STONE2 (only one)
        // MATERIAL/HIT/HIT_{BIOMETAL|CARPET|CONCRETE|DEBRIS|ENERGY|GLASS|GRASS|HOLLOW_METAL|HOLLOW_WOOD|ICE|LIQUID|NONE|PLASTIC|ROCK|SAND|SNOW|SOLID_METAL|TILE|TREE|WOOD}
        // MATERIAL/FOOTSTEP/FS_{BIOMETAL|CARPET|CONCRETE|DEBRIS|ENERGY|GLASS|GRASS|HOLLOWMETAL(mistake)|HOLLOW_METAL|HOLLOW_WOOD|ICE|LIQUID|NONE|NORMAL|PLASTIC|ROCK|SAND|SNOW|SOLID_METAL|STONE|TILE|TREE|WOOD}
        // MATERIAL/FOOTSTEP/LAND_{GROUND|HARD|METAL|ROCK|STONE|WOOD}
        // MATERIAL/FOOTSTEP/WATER
        // MATERIAL/FOOTSTEP/FOOTSTEPFS_WOOD
        // HERO_CHALLENGE/(27 UNIQUE)
        // MENU/(16 UNIQUE)
        // ZONE_SHARED/{(map name)|FAN|FX|MOVER|OBJECT|SPECIAL|TOWN}/(various)
        // For binding:
        public static readonly Category[] Categories = Enum.GetValues<Category>();

        public enum MenuPrefix
        {
            OTHER,
            TEAM,
            AN,
            BREAK,
            CHARACTER,
            GAME,
            MELEE,
            MUSIC
        };
        public static readonly string[] MenuPrefixes =
        [
        "",
        "TEAM_BONUS_",
        "MENUS/CHARACTER/AN_",
        "MENUS/CHARACTER/BREAK_",
        "MENUS/CHARACTER/", // stats etc.
        "MENUS/GAME/",
        "MENUS/MELEE/",
        "MENUS/MUSIC/" // only one
        ];
        // MELEE/{BLOCKED|DMG_{CRITICAL|KNOCKBACK|POPUP|STUN|TRIP}|HIT_{BLADE|COLD|CONCRETE|ELECTRIC|ENERGY|FIRE|HOLLOWMETAL|MENTAL|METAL|METAL_SOLID|NORMAL|SOLID_METAL}|JS_{}|KB_{}|LAND_{BODY|SMASH}|MOMENTUM_BAR|UNDER_MOMENTUM|WHOOSH{|_LIGHT|_HEAVY}|WOOSH_{AIR|GRAB|KICK|PUNCH|THROW}}
        // GAME/(59 UNIQUE)
        public static readonly MenuPrefix[] MenuTypes = Enum.GetValues<MenuPrefix>();

        private readonly record struct EventPair(string Input, string Event);
        private static readonly EventPair[] FilePrefixEventMap = new EventPair[42];

        static Events()
        {
            // 20 + 5 (ZsndEvents.MPowers.Length) * 12 = 80, * 21 = 1680 | 54 * 21 = 1134
            M.CopyTo(MRand, 0);
            Voice.CopyTo(VRand, 0);
            int mp = MPowers.Length;
            int ms = M.Length - mp;
            int m = MRand.Length / RandomMultiiplier;
            int v = Voice.Length;
            int r = RandomMultiiplier - 1;
            for (int i = 1; i <= MPowersMultiplier; i++)
            {
                for (int j = 0; j < mp; j++)
                    MRand[ms + i * mp + j] = $"P{i}_{MPowers[j]}";
            }
            Master = MRand[..m];
            // Master = MRand[0..(ms + mp + mp * MPowersMultiplier)];
            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < m; j++)
                    MRand[m + m * i + j] = $"{MRand[j]}/***RANDOM***/{i}";
                for (int j = 0; j < v; j++)
                    VRand[v + v * i + j] = $"{VRand[j]}/***RANDOM***/{i}";
            }
            int x = 0;
            for (int i = 0; i < 20; i++)
                FilePrefixEventMap[x++] = new(Voice[i], Voice[i]);
            // Skipping bored
            for (int i = 0; i < 12; i++)
                FilePrefixEventMap[x++] = new(M[i], i is 10 or 11 ? "PICKUP" : M[i]);
            // Skipping Mods and powers
            for (int i = 45; i < 54; i++)
                FilePrefixEventMap[x++] = new(Voice[i], Voice[i]);
            FilePrefixEventMap[x] = new("6_STATS", "STATS");
        }
        /// <summary>
        /// Attempts to map a <paramref name="file"/> name prefix to a corresponding event name based on predefined mappings.
        /// </summary>
        /// <returns>The event associated with the file prefix if a match is found and the remaining characters are valid; otherwise, <see langword="null"/>.</returns>
        public static string? FilePrefixToEvent(string file)
        {
            for (int i = 0; i < FilePrefixEventMap.Length; i++)
            {
                EventPair ep = FilePrefixEventMap[i];
                if (file.StartsWith(ep.Input, StringComparison.OrdinalIgnoreCase))
                {
                    for (int j = ep.Input.Length; j < file.Length; j++)
                    { char c = file[j]; if (!(char.IsDigit(c) || c is '_' or '-' or ' ')) { return null; } }
                    return ep.Event;
                }
            }
            return null;
        }
    }
    /// <summary>
    /// Zsound file name set-up for hashing (reverse generating)
    /// </summary>
    public class FileEvents
    {
        public bool MFirst { get; private set; }
        public string[] Znames { get; private set; } = new string[2];
        public string[] MusicEvents { get; private set; } = Events.Music;
        //private readonly string[] Znames = new string[2];

        public FileEvents() { }
        public FileEvents(string Zname) { Update(Zname); }
        /// <summary>
        /// Initialize the <see cref="Events.MRand"/>/<see cref="Events.VRand"/> priority, the <see cref="Events.Music"/> and <see cref="ZnameSuffixes"/> with the <paramref name="Zname"/>.
        /// </summary>
        /// <param name="Zname">The .zss/.zsm file name without extension in UPPERCASE.</param>
        /// <param name="BaseName">The optional base name of Zname (minus the _M/_V suffix) in UPPERCASE.</param>
        /// <remarks>Must be done before using <see cref="Hashing.ToStr"/> or <see cref="Hashing.EnsureHash"/>.</remarks>
        public void Update(string Zname, string? BaseName = null)
        {
            if (Zname.Length < 3) { return; }
            BaseName ??= Zname[0..^2];
            MFirst = Zname[^1] == 'M';
            Znames = [Zname, $"{BaseName}_{(MFirst ? 'V' : 'M')}"];
            for (int i = 0; i < Events.MusSfxs.Length; i++) { MusicEvents[i] = $"MUSIC/{BaseName}_{Events.MusSfxs[i]}"; }
        }
    }
    /// <summary>
    /// Zsound hash event conversion
    /// </summary>
    public static partial class Hashing
    {
        [GeneratedRegex(@"\/\*\*\*RANDOM\*\*\*\/\d+$", RegexOptions.IgnoreCase)]
        private static partial Regex Random();

        [GeneratedRegex(@"\d*_ALT$")]
        private static partial Regex AltSfx();

        [GeneratedRegex(@"(\d*_?\d*|\d*_\w\d?)$")]
        private static partial Regex DigitSfx();

        private static Dictionary<uint, string>? JsonHashes;
        private static readonly string[] CharPrefix = ["CHAR", "CHARACTER"];
        private static uint PJWTest;
        /// <summary>
        /// PJW hash generator from <paramref name="str"/>ing.
        /// </summary>
        /// <returns>Hash <see cref="uint"/> generated from <paramref name="str"/>ing.</returns>
        public static uint PJW(string str)
        {
            //const uint BitsInUnsignedInt = (uint)(4 * 8);                                     //32
            //const uint ThreeQuarters = (uint)(BitsInUnsignedInt * 3 / 4);                     //24
            //const uint OneEighth = (uint)(BitsInUnsignedInt / 8);                             //4
            //const uint HighBits = (uint)(0xFFFFFFFF) << (int)(BitsInUnsignedInt - OneEighth); //0xF0000000
            uint hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 4) + ((byte)str[i]);
                if ((PJWTest = hash & 0xF0000000) != 0)
                    hash = (hash ^ (PJWTest >> 24)) & (~0xF0000000);
            }
            return hash; // & 0x7FFFFFFF
        }
        /// <summary>
        /// PJW hash generator from upper-case instance of <paramref name="str"/>ing.
        /// </summary>
        public static uint PJWUPPER(string str)
        {
            return PJW(str.ToUpperInvariant());
        }
        /// <summary>
        /// Convert a <paramref name="HashNum"/> (number) to a hash string, depending on <paramref name="Fpath"/> and saved events.
        /// </summary>
        /// <param name="Fpath">The file path.</param>
        /// <returns>The hash <see cref="string"/>, if identified by <see cref="JsonHashes"/> or events, otherwise <paramref name="HashNum"/> as <see cref="string"/>.</returns>
        public static string ToStr(uint HashNum, string Fpath, FileEvents ev)
        {
            try
            {
                // For release: ZsndPath.CD, instead of Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!
                JsonHashes ??= System.Text.Json.JsonSerializer.Deserialize<Dictionary<uint, string>>(
                    File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                        "Assets", "zsnd_hashes.json")));
            }
            catch { } // leave JsonHashes null. Does it make sense to report somewhere?
            return Random().Replace(JsonHashes is not null
                && JsonHashes.TryGetValue(HashNum, out string? Hash)
                ? Hash : PJWReverse(HashNum, Fpath, ev), "");
        }
        /// <summary>
        /// Check whether the <paramref name="Hash"/> is a <see cref="uint"/> or a <see cref="string"/>.
        /// </summary>
        /// <param name="ev">The file events in UPPERCASE for the reverse generators.</param>
        /// <returns>The <paramref name="Hash"/> with replaced random suffix, if it's a <see cref="string"/>; otherwise, the attempted reverse generated hash as a <see cref="string"/>.</returns>
        public static string EnsureHash(string Hash, FileEvents ev) // , string Fpath = ""
        {
            return uint.TryParse(Hash, out uint HashNum)
                ? ToStr(HashNum, "", ev)
                : Random().Replace(Hash, "");
        }

        private static string PJWReverse(uint HashNum, string Fpath, FileEvents ev)
        {
            // if the hash wasn't listed, we try to reverse generate it
            string Fname = Path.GetFileNameWithoutExtension(Fpath).ToUpperInvariant();
            if (Fname != "")
            {
                string CleanFn = Fname.StartsWith("XTREME2") ? "XTREME2"
                    : AltSfx().Replace(DigitSfx().Replace(Fname, ""), "");
                string[] FNevents = new string[2 * 21];
                for (int i = -1; i < 20; i++)
                {
                    string random = (i == -1 ? "" : $"/***RANDOM***/{i}");
                    FNevents[2 + 2 * i] = $"{Fname}{random}"; FNevents[3 + 2 * i] = $"{CleanFn}{random}";
                }
                if (PJWReverseChar(HashNum, FNevents, ev) is string HFN1)
                    return HFN1;
                // try voice and pure file name (no random)
                if (PJWReverseChar(HashNum, [$"VOICE/{Fname}", CleanFn, Fname], ev) is string HFN2)
                    return HFN2;
            }
            // if the file name isn't the hash, try character events
            if (PJWReverseChar(HashNum, ev.MFirst ? Events.MRand : Events.VRand, ev) is string HChar1)
                return HChar1;
            if (PJWReverseChar(HashNum, ev.MFirst ? Events.VRand : Events.MRand, ev) is string HChar2)
                return HChar2;
            // if not character hash, try music events
            for (int e = 0; e < Events.MusSfxs.Length; e++)
                if (PJW(ev.MusicEvents[e]) == HashNum)
                    return ev.MusicEvents[e];
            if (Fname == "") { return HashNum.ToString(); }
            // if not music hash, try x_voice events
            for (int i = -1; i < 20; i++)
                for (int e = 0; e < 5; e++)
                {
                    string Hash = $"COMMON/{Events.MenuPrefixes[e]}{Fname}{(i == -1 ? "" : $"/***RANDOM***/{i}")}";
                    if (PJW(Hash) == HashNum)
                        return Hash;
                }
            return HashNum.ToString();
        }

        private static string? PJWReverseChar(uint HashNum, string[] Events, FileEvents ev)
        {
            for (int n = 0; n < 2; n++)
                for (int c = 0; c < 2; c++)
                    for (int e = 0; e < Events.Length; e++)
                    {
                        string Hash = $"{CharPrefix[c]}/{ev.Znames[n]}/{Events[e]}";
                        if (PJW(Hash) == HashNum)
                            return Hash;
                    }
            return null;
        }
        /// <summary>
        /// Generates a character hash string based on the specified <paramref name="file"/> name and character <paramref name="name"/>/ID (char. sound file name).
        /// </summary>
        /// <returns>"CHAR/{<paramref name="name"/>}/{Event/FilePrefix}", where 'Event' is used if an event was matched from the <paramref name="file"/> prefix; otherwise the 'FilePrefix' is used. Returns empty if <paramref name="file"/> is empty.</returns>
        public static string CharHashFromFile(string file, string name)
        {
            if (file.Length == 0) { return file; }
            if (Events.FilePrefixToEvent(file) is string ev) { return $"CHAR/{name}/{ev}"; }
            ReadOnlySpan<char> FileSpan = file;
            int i = FileSpan.Length - 1, e = Math.Max(0, FileSpan.Length - 6);
            for (; i > e; i--)
            {
                char c = FileSpan[i];
                if (!(char.IsDigit(c) || c is '_' or '-' or ' ')) { break; }
            }
            return $"CHAR/{name}/{FileSpan[..(i + 1)]}";
        }
        /// <summary>
        /// Add random suffixes to the duplicate <see cref="UISound.Hash"/>es in <paramref name="Sounds"/>. Hashes must be UPPERCASE already.
        /// </summary>
        //public static void AddRandomSuffix(List<UISound> Sounds)
        //{
        //    // Unfortunately randomizes hashes in the UI as well
        //    // Note: This is slightly faster than any single pass solutions
        //    Dictionary<string, int> Randoms = new(StringComparer.OrdinalIgnoreCase);
        //    HashSet<string> Uniques = new(StringComparer.OrdinalIgnoreCase);
        //    for (int i = 0; i < Sounds.Count; i++)
        //    {
        //        string Hash = Sounds[i].Hash;
        //        if (!Uniques.Add(Hash)) { Randoms[Hash] = 0; }
        //    }
        //    for (int i = 0; i < Sounds.Count; i++)
        //    {
        //        string Hash = Sounds[i].Hash;
        //        if (Randoms.TryGetValue(Hash, out int index))
        //        {
        //            Sounds[i].Hash = $"{Hash}/***RANDOM***/{index}";
        //            Randoms[Hash]++;
        //        }
        //    }
        //}
        /// <summary>
        /// Add random suffixes to the duplicate hashes in <paramref name="Sounds"/>. Hashes must be UPPERCASE already.
        /// </summary>
        /// <returns>A <see cref="Span{T}"/> of <see cref="uint"/>s with the converted hashes plus indices, sorted by hash.</returns>
        public static Span<uint> GetRandomizedHashTable(List<UISound> Sounds)
        {
            // Note: This is slightly faster than any single pass solutions
            int SoundCount = Sounds.Count;
            Span<uint> SoundHashes = new uint[SoundCount * 2];
            Span<HashPair> HashTable = System.Runtime.InteropServices.MemoryMarshal.Cast<uint, HashPair>(SoundHashes);
            Dictionary<string, int> Randoms = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> Uniques = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < SoundCount; i++)
            {
                string Hash = Sounds[i].Hash;
                if (!Uniques.Add(Hash)) { Randoms[Hash] = 0; }
            }
            for (int i = 0; i < SoundCount; i++)
            {
                string Hash = Sounds[i].Hash;
                HashTable[i] = new HashPair
                {
                    Hash = PJW(Randoms.TryGetValue(Hash, out _) ? $"{Hash}/***RANDOM***/{Randoms[Hash]++}" : Hash),
                    Index = (uint)i
                };
            }
            HashTable.Sort(static (x, y) => x.Hash.CompareTo(y.Hash));
            return SoundHashes;
        }
    }
}
