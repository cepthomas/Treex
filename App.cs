using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Treex
{
    public class App
    {
        #region Fields
        readonly HashSet<string> excludeDirectories = [];
        readonly Dictionary<string, ConsoleColorEx> extColors = [];

        // TODO useful? readonly string logFileName;

        // Visuals. Unicode is default.
        readonly record struct Visuals (string Tee, string Ell, string Vert, string Hor);
        readonly Visuals Unicode = new("├── ", "└── ", "│   ", "    ");
        readonly Visuals Ascii = new("+---", @"\---", "|   ", "    ");
        readonly Visuals vis;

        // Default defaults. May be overridden from cl.
        readonly string startFolder = Environment.CurrentDirectory;
        readonly bool showFiles = true;
        readonly bool showSize = false;
        readonly bool ascii = true;
        readonly int maxDepth = 0;
        readonly ConsoleColor defaultColor = Console.ForegroundColor;
        readonly ConsoleColorEx dirColor = ConsoleColorEx.None;
        readonly ConsoleColorEx fileColor = ConsoleColorEx.None;
        readonly ConsoleColorEx exeColor = ConsoleColorEx.None;
        readonly ConsoleColorEx binColor = ConsoleColorEx.None;
        #endregion

        /// <summary>Build me one.</summary>
        public App(string[] args)
        {
            try
            {
                string appDir = MiscUtils.GetAppDataDir("Treex", "Ephemera");
                //logFileName = Path.Combine(appDir, "log.txt");

                ///// Init runtime values from default ini file. TODO or new one from cmd line?
                var inrdr = new IniReader(Path.Join(appDir, "treex_default.ini"));
                var section = inrdr.Contents["treex"];
                HashSet<string> imageFiles = [];
                HashSet<string> audioFiles = [];
                HashSet<string> executableFiles = [];
                HashSet<string> binaryFiles = [];

                foreach (var val in section.Values)
                {
                    switch (val.Key)
                    {
                        case "show_files": showFiles = bool.Parse(val.Value); break;
                        case "show_size": showSize = bool.Parse(val.Value); break;
                        case "ascii": ascii = bool.Parse(val.Value); break;
                        case "max_depth": maxDepth = int.Parse(val.Value); break;

                        case "image_files": imageFiles = ReadList(); break;
                        case "audio_files": audioFiles = ReadList(); break;
                        case "executable_files": executableFiles = ReadList(); break;
                        case "binary_files": binaryFiles = ReadList(); break;

                        case "exclude_directories": excludeDirectories = ReadList(false); break;

                        case "dir_color": dirColor = ReadColor(); break;
                        case "file_color": fileColor = ReadColor(); break;
                        case "exe_color": exeColor = ReadColor(); break;
                        case "bin_color": binColor = ReadColor(); break;

                        default: throw new IniSyntaxException($"Invalid section value: {val.Key}", 9999);
                    }

                    #region Parse helpers
                    HashSet<string> ReadList(bool fixext = true)
                    {
                        HashSet<string> res = [];
                        var parts = val.Value.SplitByToken(",");
                        parts.ForEach(p => res.Add(fixext && !p.StartsWith('.') ? "." + p.ToLower() : p.ToLower()));
                        return res;
                    }

                    ConsoleColorEx ReadColor()
                    {
                        if (Enum.TryParse(val.Value, true, out ConsoleColorEx pclr))
                        {
                            return pclr;
                        }
                        throw new IniSyntaxException($"Invalid color: {val.Key}", 9999);
                    }
                    #endregion
                }

                ///// Process command line options.
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    if (i == 0 && !arg.StartsWith('-'))
                    {
                        startFolder = arg;
                        if (!Directory.Exists(startFolder))
                        {
                            throw new ArgumentException($"Invalid path: {startFolder}");
                        }
                        continue;
                    }

                    switch (arg)
                    {
                        case "-?":
                            PrintUsage();
                            Environment.Exit(0);
                            break;

                        case "-s":
                            showSize = true;
                            break;

                        case "-f":
                            showFiles = true;
                            break;

                        case "-d":
                            maxDepth = int.Parse(args[++i]);
                            break;

                        case "-i":
                            List<string> iparts = args[++i].SplitByToken(",");
                            iparts.ForEach(p => excludeDirectories.Add(p));
                            break;

                        case "-u":
                            List<string> uparts = args[++i].SplitByToken(",");
                            uparts.ForEach(p => excludeDirectories.Remove(p));
                            break;

                        default:
                            throw new ArgumentException($"Invalid argument: {arg}");
                    }
                }

                ///// Final config fixups.
                vis = ascii ? Ascii : Unicode;

                imageFiles.ForEach(f => extColors[f] = binColor);
                audioFiles.ForEach(f => extColors[f] = binColor);
                binaryFiles.ForEach(f => extColors[f] = binColor);
                executableFiles.ForEach(f => extColors[f] = exeColor);

                ///// Do it, stewart /////
                Print(startFolder);
                Print(Environment.NewLine);
                PrintTree(startFolder, "", 0);
            }
            catch (IniSyntaxException ex)
            {
                Console.WriteLine($"Syntax error({ex.LineNum}): {ex.Message}");
                Environment.Exit(1);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"{ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType()}: {ex.Message}");
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Do one level of this branch. Checks exclusion filters.
        /// </summary>
        /// <param name="startDir"></param>
        /// <param name="prefix"></param>
        /// <param name="depth"></param>
        void PrintTree(string startDir, string prefix = "", int depth = 0)
        {
            if (maxDepth > 0 && depth >= maxDepth) return;

            var di = new DirectoryInfo(startDir);

            // Print files first.
            if (showFiles)
            {
                var files = di.GetFiles().ToList();
                files.Sort((x, y) => x.Name.CompareTo(y.Name));

                foreach (var file in files)
                {
                    var fn = file.FullName; // full path
                    var nm = file.Name;
                    var last = file == files.Last();

                    if (file == files.Last())
                    {
                        Print($"{prefix}{vis.Ell}");
                        PrintValue(file);
                    }
                    else
                    {
                        Print($"{prefix}{vis.Hor}");
                        PrintValue(file);
                    }
                }
            }

            // Then dirs.
            var dirs = di.GetDirectories().ToList();
            dirs.Sort((x, y) => x.Name.CompareTo(y.Name));

            foreach (var dir in dirs)
            {
                var fn = dir.FullName; // full path
                var nm = dir.Name;

                if (!excludeDirectories.Contains(nm))
                {
                    if (dir == dirs.Last())
                    {
                        Print($"{prefix}{vis.Hor}");
                        PrintValue(dir);
                        PrintTree(fn, $"{prefix}{vis.Hor}", depth + 1); // => recurse

                    }
                    else
                    {
                        Print($"{prefix}{vis.Tee}");
                        PrintValue(dir);
                        PrintTree(fn, $"{prefix}{vis.Vert}", depth + 1); // => recurse
                    }
                }
            }
        }

        /// <summary>
        /// Print a file info.
        /// </summary>
        /// <param name="fi"></param>
        void PrintValue(FileInfo fi)
        {
            var slen = "";
            if (showSize)
            {
                var bytes = new FileInfo(fi.FullName).Length;
                slen = bytes switch
                {
                    >= 1024 * 1024 => $" ({bytes / 1024 / 1024}M)",
                    >= 1024 => $" ({bytes / 1024}K)",
                    _ => $" ({bytes}B)"
                };
            }

            string ext = fi.Extension.ToLower();
            var clr = extColors.TryGetValue(ext, out ConsoleColorEx clrex) ? clrex : fileColor;
            Print($"{fi.Name}{slen}", clr);
            Print(Environment.NewLine);
        }

        /// <summary>
        /// Print a directory info.
        /// </summary>
        /// <param name="di"></param>
        void PrintValue(DirectoryInfo di)
        {
            Print($"{di.Name}", dirColor);
            Print(Environment.NewLine);
        }

        /// <summary>
        /// Low level write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="clr">Optional color</param>
        void Print(string text, ConsoleColorEx clr = ConsoleColorEx.None)
        {
            Console.ForegroundColor = clr == ConsoleColorEx.None ? defaultColor : (ConsoleColor)clr;
            Console.Write(text);
            Console.ForegroundColor = defaultColor;
        }

        /// <summary>
        /// Give some help.
        /// </summary>
        void PrintUsage()
        {
            Console.WriteLine("treex [dir] [-f] [-d N] [-s] [-?] [-i fld 1,fld2,...] [-u fld1,fld2,...]");
            Console.WriteLine("opts:  * indicates default in settings");
            Console.WriteLine("    dir: start folder or '.' if missing");
            Console.WriteLine("    -d num*: maxDepth (0 means all)");
            Console.WriteLine("    -f*: show files ");
            Console.WriteLine("    -s*: show size (file only)");
            Console.WriteLine("    -i fld1,fld2,...*: ignore folders  (add to default)");
            Console.WriteLine("    -u fld1,fld2,...*: unignore folders  (remove from default)");
            Console.WriteLine("    -? help");
        }
    }

    /*
var infos = di.GetFileSystemInfos().ToList();
infos.Sort((x, y) => x.Name.CompareTo(y.Name));
foreach (var fsi in infos)
{
    //var ext = fsi.Extension;
    var fn = fsi.FullName; // full path
    var nm = fsi.Name;
    var isDir = IsDirectory(fsi);
    var last = fsi == infos.Last();

    var exclude = (isDir && excludeDirectories.Contains(nm)) || (!isDir && !showFiles);

    switch (exclude, isDir, last)
    {
       case (true, _, _):
           continue;

       case (false, true, false): // dir
            Print($"{prefix}{vis.Tee}");
            PrintValue(fsi);
            PrintTree(fn, $"{prefix}{vis.Vert}", depth + 1); // => recurse
            break;

       case (false, true, true): // last dir
            Print($"{prefix}{vis.Hor}");
            PrintValue(fsi);
            PrintTree(fn, $"{prefix}{vis.Hor}", depth + 1); // => recurse
            break;

        case (false, false, false): // file
            Print($"{prefix}{vis.Hor}");
            PrintValue(fsi);
            break;

        case (false, false, true): // last file
            Print($"{prefix}{vis.Ell}");
            PrintValue(fsi);
            break;
    }
}
*/

    /*
/// <summary>
/// Print one file system entry. Selects color.
/// </summary>
/// <param name="fsi"></param>
void PrintValue(FileSystemInfo fsi)
{
    ConsoleColorEx clr;
    var slen = "";

    if (IsDirectory(fsi))
    {
       clr = dirColor;
    }
    else
    {
        if (showSize)
        {
            var bytes = new FileInfo(fsi.FullName).Length;
            slen = bytes switch
            {
                >= 1024 * 1024 => $" ({bytes / 1024 / 1024}M)",
                >= 1024 => $" ({bytes / 1024}K)",
                _ => $" ({bytes}B)"
            };
        }

        string ext = fsi.Extension.ToLower();
        clr = extColors.TryGetValue(ext, out ConsoleColorEx clrex) ? clrex : fileColor;
    }

    Print($"{fsi.Name}{slen}", clr);
    Print(Environment.NewLine);
}
*/



    // void WriteColored(string text, ConsoleColor color)
    // {
    //     SetColor(color);
    //     Write(text);
    //     SetColor(_defaultColor);
    // }

    // ConsoleColor GetColor(FileSystemInfo fsItem)
    // {
    //     if (IsDirectory(fsItem))
    //     {
    //        return _dirColor;
    //     }

    //     string ext = Path.GetExtension(fsItem.FullName).ToLower();
    //     if (ExeExtensions.Contains(ext))
    //     {
    //        return _exeColor;
    //     }

    //     return _fileColor;
    // }


    //[Serializable]
    //public class UserSettings : SettingsCore
    //{
    //    [DisplayName("File Names")]
    //    [Description("Include file names - default")]
    //    // [Category("NTerm")]
    //    [Browsable(true)]
    //    public bool IncludeFiles { get; set; } = true;

    //    [DisplayName("Show Size")]
    //    [Description("Show size - default")]
    //    [Browsable(true)]
    //    public bool ShowSize { get; set; } = true;

    //    [DisplayName("Iteration Depth")]
    //    [Description("How deep to look - default")]
    //    [Browsable(true)]
    //    public int MaxDepth { get; set; } = 3;

    //    [DisplayName("Ascii Characters")]
    //    [Description("Use ascii charactrs else unicode")]
    //    [Browsable(true)]
    //    public bool AsciiChars { get; set; } = true;

    //    // Show unix "hidden" files/dirs like .git, .vs, .gitignore
    //    [DisplayName("Include Hidden")]
    //    [Description("Include hidden files (.*)")]
    //    [Browsable(true)]
    //    public bool IncludeHidden { get; set; } = false;

    //    //// like bin, obj, ...
    //    //[DisplayName("Ignore Directories")]
    //    //[Description("Ignore directories - base")]
    //    //[Category("NTerm")]
    //    //[Browsable(true)]
    //    //public List<string> IgnoreDirectories { get; set; } = [];
    //}
}
