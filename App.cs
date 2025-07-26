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

        // Visuals. Unicode is default.
        readonly record struct Visuals (string Tee, string Ell, string Pipe, string Empty);
        readonly Visuals Unicode = new("├── ", "└── ", "│   ", "    ");
        readonly Visuals Ascii = new("+---", @"\---", "|   ", "    ");
        readonly Visuals vis;

        // Default defaults. May be overridden from cl.
        readonly string startFolder = Environment.CurrentDirectory;
        readonly bool showFiles = true;
        readonly bool showSize = false;
        readonly bool ascii = true;
        readonly bool color = true;
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
            int code = 0;

            try
            {
                // Init runtime values from ini file.
                var inrdr = new IniReader(Path.Join(Environment.ExpandEnvironmentVariables("DEV_BIN_PATH"), "treex.ini"));
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
                                                var startFolder = args[args.Length - 2];

                // treex [-f] [-d N] [-s] [-?] [-i fld 1,fld2,...] [-u fld1,fld2,...] [dir]
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    switch (arg)
                    {
                        case "-s":
                            showSize = true;
                            break;

                        case "-f":
                            showFiles = true;
                            break;

                        case "-c":
                            color = false;
                            break;

                        case "-d":
                            maxDepth = int.Parse(args[++i]);
                            break;

                        case "-e":
                            List<string> iparts = args[++i].SplitByToken(",");
                            iparts.ForEach(p => excludeDirectories.Add(p));
                            break;

                        case "-i":
                            List<string> uparts = args[++i].SplitByToken(",");
                            uparts.ForEach(p => excludeDirectories.Remove(p));
                            break;

                        case "-?":
                            PrintUsage();
                            Environment.Exit(0);
                            break;

                        default:
                            // If last, check for valid startFolder.
                            if (i == args.Length - 1)
                            {
                                if (!Directory.Exists(arg))
                                {
                                    throw new ArgumentException($"Invalid folder: {arg}");
                                }
                                var startFolder = arg;
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid argument: {arg}");
                            }
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
                code = 1;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"{ex.Message}");
                code = 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType()}: {ex.Message}");
                code = 3;
            }

            Environment.Exit(code);
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

            ///// Collect contents first.
            var files = showFiles ? di.GetFiles().ToList() : [];
            files.Sort((x, y) => x.Name.CompareTo(y.Name));

            var dirs = di.GetDirectories().Where(d => !excludeDirectories.Contains(d.Name)).ToList();
            dirs.Sort((x, y) => x.Name.CompareTo(y.Name));

            ///// Print files.
            foreach (var file in files)
            {
                var fn = file.FullName; // full path
                var nm = file.Name;
                var last = file == files.Last();

                if (dirs.Count > 0)
                {
                    Print($"{prefix}{vis.Pipe}");
                    PrintValue(file);
                }
                else
                {
                    Print($"{prefix}{vis.Empty}");
                    PrintValue(file);
                }
            }

            ///// Then dirs.
            foreach (var dir in dirs)
            {
                var fn = dir.FullName; // full path
                var nm = dir.Name;

                if (dir == dirs.Last())
                {
                    Print($"{prefix}{vis.Ell}");
                    PrintValue(dir);
                    PrintTree(fn, $"{prefix}{vis.Empty}", depth + 1); // => recurse

                }
                else
                {
                    Print($"{prefix}{vis.Tee}");
                    PrintValue(dir);
                    PrintTree(fn, $"{prefix}{vis.Pipe}", depth + 1); // => recurse
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
                    >= 1024 * 1024 => $" ({bytes / 1024 / 1024}m)",
                    >= 1024 => $" ({bytes / 1024}k)",
                    _ => $" ({bytes}b)"
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
            if (color)
            {
                Console.ForegroundColor = clr == ConsoleColorEx.None ? defaultColor : (ConsoleColor)clr;
                Console.Write(text);
                Console.ForegroundColor = defaultColor;
            }
            else
            {
                Console.Write(text);
            }
        }

        /// <summary>Give some help.</summary>
        void PrintUsage()
        {
            Console.WriteLine("treex [-c] [-f] [-d N] [-s] [-?] [-e fld 1,fld2,...] [-i fld1,fld2,...] [dir]");
            Console.WriteLine("opts:  * indicates default in settings");
            Console.WriteLine("    dir: start folder or current if missing");
            Console.WriteLine("    -c: color output off");
            Console.WriteLine("    -d num*: maxDepth (0 means all)");
            Console.WriteLine("    -f*: show files");
            Console.WriteLine("    -s*: show size (file only)");
            Console.WriteLine("    -e fld1,fld2,...*: exclude directory(s)  (adds to default)");
            Console.WriteLine("    -i fld1,fld2,...*: unexclude directory(s)  (removes to default)");
            Console.WriteLine("    -? help");
        }

        /// <summary>start here.</summary>
        static void Main(string[] args)
        {
            var app = new App(args);
        }
    }
}
