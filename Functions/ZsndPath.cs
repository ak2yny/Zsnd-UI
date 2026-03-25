using System;
using System.IO;

namespace Zsnd_UI.Functions
{
    /// <summary>
    /// File and directory path related functions.
    /// </summary>
    internal static class ZsndPath
    {
        /// <summary>
        /// Current directory (exe or 'start in')
        /// </summary>
        public static readonly string CD = Directory.GetCurrentDirectory();
        //public static readonly string Temp = Path.GetTempPath();
        //public static readonly string Activision = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Activision");
        // Game path patterns:
        //private static readonly string[] GameFolders =
        //[
        //    "actors",
        //    "automaps",
        //    "conversations",
        //    "data",
        //    "dialogs",
        //    "effects",
        //    "hud",
        //    "maps",
        //    "models",
        //    "motionpaths",
        //    "movies",
        //    "packages",
        //    "plugins",
        //    "scripts",
        //    "shaders",
        //    "skybox",
        //    "sounds",
        //    "subtitles",
        //    "texs",
        //    "textures",
        //    "ui"
        //];
        private static readonly Func<string, int> IndexOfLastDirectorySeparator = Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar
            ? static path => path.LastIndexOf(Path.DirectorySeparatorChar)
            : static path => path.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        /// <summary>
        /// Creates all directories and subdirectories of the parent directory for the specified <paramref name="FilePath"/> unless they already exist.
        /// </summary>
        /// <remarks>Exceptions: System.IO (various); <paramref name="FilePath"/> denotes a root directory</remarks>
        /// <returns>The normalized parent directory path as a string, regardless of whether a parent directory of the specified path already exists.</returns>
        public static string CreateDirectory(string FilePath)
        { return Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!).FullName; }
        /// <summary>
        /// Returns the parent directory string for the specified <paramref name="path"/>. Does not trim and splits at '/'.
        /// </summary>
        /// <returns>The portion of <paramref name="path"/>, up to the last forward slash, not including.
        /// If the span does not contain a forward slash, the original <paramref name="path"/> is returned.</returns>
        public static ReadOnlySpan<char> GetParentPath(ReadOnlySpan<char> path)
        {
            int i = path.LastIndexOf('/');
            return i == -1 ? path : path[0..i];
        }
        /// <summary>
        /// Creates a path to a hidden '.Headerless' directory inside the specified <paramref name="path"/> directory.
        /// </summary>
        /// <remarks>Exceptions: System.IO (various)</remarks>
        /// <returns>The full, normalized path to '<paramref name="path"/>/.Headerless/<paramref name="filename"/>'.</returns>
        public static string GetHeaderlessPath(string path, string filename)
        {
            DirectoryInfo HLP = Directory.CreateDirectory(Path.Combine(path, ".Headerless"));
            HLP.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            return Path.Combine(HLP.FullName, filename);
        }
        /// <summary>
        /// Creates a path to a hidden '.Headerless' directory alongside the specified <paramref name="path"/> and creates the directories if necessary.
        /// </summary>
        /// <remarks>Exceptions: System.IO (various)</remarks>
        /// <returns>The full, normalized path to a file of the same name as <paramref name="path"/> in the '.Headerless' directory.</returns>
        public static string GetHeaderlessPath(string path)
        {
            // Path should be semi normalized (no // or ../.)
            int i = IndexOfLastDirectorySeparator(path);
            DirectoryInfo HLP = Directory.CreateDirectory(Path.Combine(path[..i], ".Headerless"));
            HLP.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            return Path.Combine(HLP.FullName, path[(i + 1)..]);
        }
        /// <summary>
        /// Expands the base <paramref name="path"/> to "<paramref name="path"/>/packages/generated/characters/<paramref name="Pkg"/>".
        /// </summary>
        /// <returns>The combined file system path (or <paramref name="Pkg"/> if <paramref name="Pkg"/>'s rooted).</returns>
        //public static string Packages(string path, string Pkg = "") => Path.Combine(path, "packages", "generated", "characters", Pkg);
        /// <summary>
        /// Get the saves folder for the current tab/game (folder with 'Save' in it). Must be checked for existence.
        /// </summary>
        //public static string SaveFolder => Path.Combine(Activision, Shared.GUI.IsMua ? "Marvel Ultimate Alliance" : "X-Men Legends 2");
        /// <summary>
        /// Checks, whether the <paramref name="Source"/> file exists and if so, creates a copy with a ".bak" extension in the same directory.
        /// </summary>
        /// <returns><see langword="True"/> if the backup is created successfully or the source file doesn't exist; otherwise <see langword="false"/>.</returns>
        public static bool Backup(string Source)
        {
            try
            {
                if (File.Exists(Source)) { File.Copy(Source, GetVacant(Source, ".bak")); }
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Construct a full file name of a <paramref name="DirectoryPath"/> file that doesn't exist.
        /// </summary>
        /// <returns><paramref name="DirectoryPath"/> if it doesn't exist, otherwise <paramref name="DirectoryPath"/>+<see cref="DateTime.Now"/>.</returns>
        public static string GetVacant(string DirectoryPath, int i = 0) =>
            Directory.Exists(DirectoryPath)
                ? i == 0
                ? GetVacant($"{DirectoryPath}-{DateTime.Now:yyMMdd-HHmmss}", i + 1)
                : GetVacant($"{(i == 1 ? DirectoryPath : DirectoryPath[..^2])}-{i}", i + 1)
                : DirectoryPath;
        /// <summary>
        /// Construct a full file name of a <paramref name="PathWithoutExt"/>+<paramref name="Ext"/>ension file that doesn't exist.
        /// </summary>
        /// <returns><paramref name="PathWithoutExt"/>+<paramref name="Ext"/> if it doesn't exist, otherwise <paramref name="PathWithoutExt"/>+<see cref="DateTime.Now"/>+<paramref name="Ext"/>.</returns>
        public static string GetVacant(string PathWithoutExt, string Ext, int i = 0) =>
            File.Exists($"{PathWithoutExt}{Ext}")
                ? i == 0
                ? GetVacant($"{PathWithoutExt}-{DateTime.Now:yyMMdd-HHmmss}", Ext, i + 1)
                : GetVacant($"{(i == 1 ? PathWithoutExt : PathWithoutExt[..^2])}-{i}", Ext, i + 1)
                : $"{PathWithoutExt}{Ext}";
        /// <summary>
        /// Expand the <paramref name="Input"/> path to a full path within the current directory (app dir). <paramref name="Input"/> may be a full or relative path.
        /// </summary>
        /// <returns>Full normalized path to "[<see cref="CD"/> = current directory]\<paramref name="Input"/>", or <paramref name="Input"/> if rooted.</returns>
        //public static string GetRooted(string Input) => Path.Combine(CD, Input);
        /// <summary>
        /// Gets the name of the specified <paramref name="path"/> string (incl. extension if it's a file path with extension).
        /// </summary>
        /// <returns>The characters after the last non-trailing directory separator character (or from the beginning) in <paramref name="path"/>. Trailing directory separator characters are trimmed.</returns>
        //public static string GetName(string path) => Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        /// <summary>
        /// Get the parent directory information for the specified <paramref name="path"/>.
        /// </summary>
        /// <remarks>Effectively removes the last segment (the characters from the last non-trailing directory separator).</remarks>
        /// <returns>Normalized parent directory of <paramref name="path"/>, or <see langword="null"/> if <paramref name="path"/> denotes a root directory or is <see langword="null"/>.
        /// Returns <see cref="string.Empty"/> if <paramref name="path"/> does not contain directory information.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="PathTooLongException"/>
        public static string? GetParent(string? path)
        {
            return string.IsNullOrEmpty(path) ? null
                : Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        /// <summary>
        /// Combine the <paramref name="Input"/> path(s) to a <see cref="string"/>, adding a <see cref="Path.DirectorySeparatorChar"/> between each member.
        /// </summary>
        /// <returns>A relative path starting with the first <paramref name="Input"/> member (which may be rooted).</returns>
        //public static string Join(params IEnumerable<string> Input) => string.Join(Path.DirectorySeparatorChar Input
        //    .Select(static s => s.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        /// <summary>
        /// Gets the existing MO2 "mods" folder or mods folder of other organizers.
        /// </summary>
        /// <returns>Parent of <see cref="OHSsettings.GameInstallPath"/>, if it's not the game folder and exists and is named "mods" or <see cref="OHSsettings.ExeName"/> is an oranizer .exe or <see cref="GUIsettings.IsMo2"/>; otherwise <see langword="null"/>.</returns>
        //public static string? ModsFolder => !File.Exists($"{Shared.OHS.GameInstallPath}/{DefaultExe}")
        //    && Directory.Exists(Shared.OHS.GameInstallPath)
        //    && GetParent(Shared.OHS.GameInstallPath) is string Mods
        //    && (InternalSettings.KnownModOrganizerExes.Contains(Shared.OHS.ExeName, StringComparer.OrdinalIgnoreCase)
        //    || Path.GetFileName(Mods) == "mods" || Shared.GUI.IsMo2)
        //    ? Mods
        //    : null;
        /// <summary>
        /// Get mod folders.
        /// </summary>
        /// <returns>Matching folders, if <see cref="ModsFolder"/> returns a valid organizer folder; otherwise an empty enumerable.</returns>
        /// <exception cref="IOException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="System.Security.SecurityException"/>
        /// <exception cref="PathTooLongException"/> // also argument exceptions, if invalid characters, etc.
        //public static IEnumerable<string> ModFolders => ModsFolder is string MO2 ? Directory.EnumerateDirectories(MO2) : [];
        /// <summary>
        /// Enumerates all directories with a package folder from the game folder and MO2 mod folders, according to the settings.
        /// </summary>
        /// <remarks>Note: <see cref="OHSsettings.GameInstallPath"/> can have a duplicate in <see cref="ModFolders"/>, but Distinct is not used to keep it lazy.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> with matching directories (can be empty).</returns>
        //public static IEnumerable<string> FoldersWpkg => ((IEnumerable<string>)[Shared.OHS.GameInstallPath, GetActualGamePath])
        //    .Concat(ModFolders).Where(static d => Directory.Exists(Packages(d)));
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> (for both) and separate <paramref name="file" /> name. May fail silently.
        /// </summary>
        //public static void CopyToGameRel(string SourceFolder, string RelativePath, string file) => CopyToGameRel(SourceFolder, RelativePath, file, file);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (from setting) using a <paramref name="RelativePath"/> (for both) and <paramref name="Source" /> and <paramref name="Target" /> filenames to rename simultaneously. May fail silently.
        /// </summary>
        //public static void CopyToGameRel(string SourceFolder, string RelativePath, string Source, string Target) => CopyFileToGame(Path.Combine(SourceFolder, RelativePath, Source), RelativePath, Target);
        /// <summary>
        /// Copy from <paramref name="SourceFolder" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using a separate <paramref name="file" /> name. May fail silently.
        /// </summary>
        //public static void CopyToGame(string? SourceFolder, string RelativePath, string file)
        //{
        //    if (!string.IsNullOrWhiteSpace(SourceFolder))
        //    {
        //        CopyFileToGame(Path.Combine(SourceFolder, file), RelativePath, file);
        //    }
        //}
        /// <summary>
        /// Copy <paramref name="SourceFile" /> (full path) to game folder (<paramref name="RelativePath"/> inside game folder from setting), using a separate <paramref name="Target" /> filename to rename simultaneously. May fail silently.
        /// </summary>
        //public static void CopyFileToGame(string SourceFile, string RelativePath, string Target)
        //{
        //    try { File.Copy(SourceFile, Path.Combine(Directory.CreateDirectory($"{Shared.UI.GameOrModPath}/{RelativePath}").FullName, Target), true); }
        //    catch { }
        //}
        /// <summary>
        /// Recursively copy a complete <paramref name="Source"/> folder with all contents to a <paramref name="Target"/> path (paths must be normalized). Existing files are replaced.
        /// </summary>
        /// <returns><see langword="True"/>, if no exceptions occur; otherwise <see langword="false"/>.</returns>
        //public static bool CopyFilesRecursively(string Source, string Target)
        //{
        //    try
        //    {
        //        foreach (string dirPath in Directory.EnumerateDirectories(Source, "*", SearchOption.AllDirectories))
        //        {
        //            _ = Directory.CreateDirectory(dirPath.Replace(Source, Target));
        //        }
        //        foreach (string SourceFile in Directory.EnumerateFiles(Source, "*", SearchOption.AllDirectories))
        //        {
        //            File.Copy(SourceFile, SourceFile.Replace(Source, Target), true);
        //        }
        //        return true;
        //    }
        //    catch { return false; }
        //}
        /// <summary>
        /// Detect if GameInstallPath is the game folder or a mod folder. If it's a mod folder, prepare a new mod in <paramref name="Source"/> with <paramref name="TargetName"/> and optional <paramref name="InstallationFile"/> info.
        /// </summary>
        /// <remarks>Exceptions: System.IO exceptions (only if it's an MO2 mod folder without meta file).</remarks>
        /// <returns>The the target path either to game files or the new mod folder. Returns <see langword="null"/> if none detected.</returns>
        //public static string? GetModTarget(string Source, string TargetName, string InstallationFile = "")
        //{
        //    string Target = Shared.OHS.GameInstallPath;
        //    bool IsGIP = Path.IsPathFullyQualified(Target) && File.Exists($"{Target}/{DefaultExe}");
        //    if (!IsGIP && ModsFolder is string Mods)
        //    {
        //        Target = GetVacant(Path.Combine(Mods, TargetName));
        //        string MetaP = Path.Combine(Source, "meta.ini");
        //        if (!File.Exists(MetaP))
        //        {
        //            string[] meta =
        //            [
        //                "[General]",
        //                $"gameName={Game.PadRight(4, '1')}",
        //                "modid=0",
        //                $"version=d{DateTime.Now:yyyy.M.d}",
        //                "newestVersion=",
        //                "category=0",
        //                "nexusFileStatus=1",
        //                $"installationFile={InstallationFile.Replace("\\", "/")}",
        //                "repository=Nexus"
        //            ];
        //            File.WriteAllLines(MetaP, meta);
        //        }
        //    }
        //    return IsGIP || Target != Shared.OHS.GameInstallPath ? Target : null;
        //}
    }
}
