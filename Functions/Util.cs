using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Zsnd_UI.Functions
{
    public static class Util
    {
        private static IntPtr CachedWindowHandle
        {
            get
            {
                if (field == IntPtr.Zero) { field = WinRT.Interop.WindowNative.GetWindowHandle(App.MWindow); }
                return field;
            }
        } = IntPtr.Zero;
        // WIP: Make save and load more alike (possibly move to Zsnd);
        /// <summary>
        /// Open file dialogue.
        /// </summary>
        public static async Task<Windows.Storage.StorageFile?> LoadDialogue(params System.Collections.Generic.IEnumerable<string> formats)
        {
            Windows.Storage.Pickers.FileOpenPicker filePicker = new();
            foreach (string format in formats) { filePicker.FileTypeFilter.Add(format); }
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, CachedWindowHandle);
            return await filePicker.PickSingleFileAsync();
        }
        /// <summary>
        /// Save dialogue.
        /// </summary>
        public static async Task<Windows.Storage.StorageFile?> SaveDialogue()
        {
            Windows.Storage.Pickers.FileSavePicker savePicker = new();
            savePicker.FileTypeChoices.Add("ZSND File", [".zsm", ".zss", ".enm", ".ens"]);
            savePicker.FileTypeChoices.Add("ZSND Configuration File", [".json"]);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, CachedWindowHandle);
            return await savePicker.PickSaveFileAsync();
        }
        /// <summary>
        /// Run an <paramref name="exe" /> through CMD, if it exists. Output is converted to boolean.
        /// </summary>
        /// <returns><see langword="True" />, if the <paramref name="exe" /> exists and if the output was returned (not <see langword="null"/>); otherwise <see langword="false"/>.</returns>
        public static bool RunExeInCmd(string exe, string args)
        {
            return File.Exists(exe.EndsWith(".exe") ? exe : $"{exe}.exe") && RunDosCommnand(exe, args) is not null;
        }
        /// <summary>
        /// Run a MS Dos Command <paramref name="cmd" /> <paramref name="vars" />
        /// </summary>
        /// <seealso cref="http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=457996"/>
        /// <param name="cmd">Executable to run (runs in console by default) - use "cmd" for command prompt and pass commands as arguments</param>
        /// <param name="vars">Arguments passed to the executable</param>
        /// <returns>The <see cref="Process.StandardOutput" />, if <see cref="Process" /> started, returned an output/error exit code, and didn't throw an exception; otherwise <see langword="null"/>.</returns>
        public static string? RunDosCommnand(string cmd, string vars)
        {
            ProcessStartInfo sinf = new(cmd, vars)
            {
                // The following commands are needed to redirect the standard output. This means that it will be redirected to the Process.StandardOutput StreamReader.
                RedirectStandardOutput = true,
                UseShellExecute = false,
                // Do not create that ugly black window, please...
                CreateNoWindow = true
            };
            // Now we create a process, assign its ProcessStartInfo
            Process p = new() { StartInfo = sinf };
            // We can now start the process and, if the process starts successfully, return the output string...
            try { return p.Start() && p.StandardOutput.ReadToEnd() is string SO && p.ExitCode == 0 ? SO : null; }
            catch { return null; }
        }
        /// <summary>
        /// Use 7-zip to try to extract an <paramref name="Archive"/> (any file path). Currently hard coded to extract to <paramref name="ExtName"/> within the temp folder.
        /// </summary>
        /// <returns>The full path in the temp folder, if 7-zip was found and could extract the file as an archive; otherwise <see langword="null"/>.</returns>
        //public static string? Run7z(string Archive, string ExtName)
        //{
        //    string ExtPath = Path.Combine(ZsndPath.Temp, ExtName);
        //    return RunExeInCmd(File.Exists("7z.exe") ? "7z" : Path.Combine(ZsndPath.CD, "OHSGUI", "7z.exe"), $"x \"{Archive}\" -o\"{ExtPath}\" -y")
        //        ? ExtPath
        //        : null;
        //}
        /// <summary>
        /// Run an elevated command <paramref name="ecmd" /> <paramref name="vars" /> in the current directory, for OHS.
        /// </summary>
        /// <returns>Exit code, if the command/process started; otherwise 5.</returns>
        //public static int RunElevated(string ecmd, string vars)
        //{
        //    string cmd = "cmd";
        //    string ev = $"/c \"set __COMPAT_LAYER=RUNASINVOKER && \"{ecmd} {vars}";
        //    ProcessStartInfo sinf = new(cmd, ev) { CreateNoWindow = true };
        //    Process p = new() { StartInfo = sinf };
        //    if (p.Start())
        //    {
        //        p.WaitForExit();
        //        return p.ExitCode;
        //    }
        //    return 5;
        //}
        // ÙI HELPERS:
        /// <summary>
        /// Finds parent in visual tree
        /// </summary>
        /// <returns>The first item of type <see cref="{T}"/> found in the ancestor visual tree if any; otherwise <see langword="null"/>.</returns>
        public static T? FindFirstParent<T>(Microsoft.UI.Xaml.DependencyObject child) where T : Microsoft.UI.Xaml.DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) { return parent; }
                child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }
        /// <summary>
        /// Finds child in visual tree
        /// </summary>
        /// <returns>The first item of type <see cref="{T}"/> found in the ancestor visual tree if any; otherwise <see langword="null"/>.</returns>
        public static T? FindFirstChild<T>(Microsoft.UI.Xaml.DependencyObject parent) where T : Microsoft.UI.Xaml.DependencyObject
        {
            int childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                Microsoft.UI.Xaml.DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T obj) { return obj; }
                if (FindFirstChild<T>(child) is T cobj) { return cobj; }
            }
            return null;
        }
        /// <summary>
        /// Hex edit the file stream <paramref name="fs"/> at <paramref name="Position"/> to <paramref name="NewValue"/>.
        /// </summary>
        /// <remarks>Exceptions: File.IO (and more)</remarks>
        //public static void HexEdit(long Position, string NewValue, FileStream fs)
        //{
        //    byte[] bytes = Encoding.ASCII.GetBytes(NewValue);
        //    fs.Position = Position;
        //    fs.Write(bytes, 0, bytes.Length);
        //}
        /// <summary>
        /// Hex edit the file in <paramref name="FilePath"/> and replace all occurrences of <paramref name="OldValues"/> with <paramref name="NewValue"/>, if any.
        /// If only one string is in <paramref name="OldValues"/>, only the first occurrence is replaced, unless specified with <paramref name="AllOccurrences"/>.
        /// </summary>
        /// <returns><see langword="True"/>, if hex-edited; otherwise <see langword="false"/>.</returns>
        //public static bool HexEdit(string[] OldValues, string NewValue, string FilePath, bool AllOccurrences = false)
        //{
        //    if (0 < NewValue.Length && NewValue.Length <= OldValues.Min(static v => v.Length))
        //    {
        //        if (OldValues.Length > 1) { AllOccurrences = true; }
        //        try
        //        {
        //            byte[] New = Encoding.ASCII.GetBytes(NewValue);
        //            byte[] ZeroPad = new byte[OldValues.Max(static v => v.Length) - New.Length];
        //            Span<byte> Bytes = File.ReadAllBytes(FilePath).AsSpan();
        //            using FileStream fs = new(FilePath, FileMode.Open);
        //            for (int i = 0; i < OldValues.Length; i++)
        //            {
        //                Span<byte> ByteVal = Encoding.ASCII.GetBytes(OldValues[i]).AsSpan();
        //                for (long p = 0; p <= Bytes.Length - ByteVal.Length; p++)
        //                {
        //                    if (Bytes.Slice((int)p, ByteVal.Length).SequenceEqual(ByteVal))
        //                    {
        //                        fs.Position = p;
        //                        fs.Write(New, 0, New.Length);
        //                        fs.Write(ZeroPad, 0, ByteVal.Length - New.Length);
        //                        if (!AllOccurrences) { return true; }
        //                        p += ByteVal.Length - 1;
        //                    }
        //                }
        //            }
        //            return true;
        //        }
        //        catch { return false; }
        //    }
        //    return false;
        //}
    }
}
