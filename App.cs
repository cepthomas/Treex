using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        readonly Visuals visUnicode = new("├── ", "└── ", "│   ", "    ");
        readonly Visuals visAscii = new("+---", @"\---", "|   ", "    ");
        readonly Visuals vis;

        // Default defaults. May be overridden from cl.
        readonly bool showFiles = false;
        readonly bool showSize = false;
        readonly bool unicode = false;
        readonly bool color = false;
        readonly int maxDepth = 0;
        readonly ConsoleColorEx dirColor = ConsoleColorEx.None;
        readonly ConsoleColorEx errColor = ConsoleColorEx.None;
        #endregion

        /// <summary>Build me one.</summary>
        public App(string[] args)
        {
            // Default start location.
            string startDir = Environment.CurrentDirectory;

            // Init runtime values from ini file if available.
            try
            {
                var exe = Environment.GetEnvironmentVariable("TOOLS_PATH");
                var inrdr = new IniReader();
                inrdr.ParseFile(Path.Join(exe, "treex.ini"));
                var section = inrdr.GetValues("treex");

                foreach (var val in section)
                {
                    switch (val.Key)
                    {
                        case "show_files": showFiles = bool.Parse(val.Value); break;
                        case "show_size": showSize = bool.Parse(val.Value); break;
                        case "unicode": unicode = bool.Parse(val.Value); break;
                        case "max_depth": maxDepth = int.Parse(val.Value); break;
                        case "use_color": color = bool.Parse(val.Value); break;

                        case "dir_color":
                            if (!Enum.TryParse(val.Value, true, out dirColor))
                            { throw new IniSyntaxException($"Invalid color for {val.Key}", -1); }
                            break;

                        case "err_color":
                            if (!Enum.TryParse(val.Value, true, out errColor))
                            { throw new IniSyntaxException($"Invalid color for {val.Key}", -1); }
                            break;

                        case "exclude_directories":
                            var dparts = val.Value.SplitByToken(",");
                            dparts.ForEach(p => excludeDirectories.Add(p));
                            break;

                        case string s when s.Contains("_files"):
                            var fparts = val.Value.SplitByToken(",");
                            if (fparts.Count < 2 || !Enum.TryParse(fparts[0], true, out ConsoleColorEx pclr))
                            { throw new IniSyntaxException($"Invalid section value for {val.Key}", -1); }
                            fparts.Take(1..).ForEach(p => extColors.Add(p.Replace(".", ""), pclr));
                            break;

                        default: throw new IniSyntaxException($"Invalid section value for {val.Key}", -1);
                    }
                }
            }
            catch (IniSyntaxException ex)
            {
                PrintLine($"IniSyntaxException at {ex.LineNum}: {ex.Message}", errColor);
                Environment.Exit(1);
            }
            catch (FileNotFoundException)
            {
                PrintLine($">>>>> Ini file not found, using defaults");
            }
            catch (Exception ex) // other
            {
                PrintLine($"{ex.GetType()}: {ex.Message}", errColor);
                Environment.Exit(2);
            }

            // Process/overlay  command line options.
            try
            {
                // treex [-f] [-c] [-m N] [-d] [-s] [-i fld 1,fld2,...] [-u fld1,fld2,...] [-h] [dir]

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
                            color = true;
                            break;

                        case "-u":
                            unicode = true;
                            break;

                        case "-m":
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

                        case "-h":
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
                                startDir = arg;
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid argument: {arg}");
                            }
                            break;
                    }
                }

                ///// Final config fixups.
                vis = unicode ? visUnicode : visAscii;

                ///// Do it stewart /////
                PrintLine(startDir);
                PrintTree(startDir, "", 0);
            }
            catch (ArgumentException ex)
            {
                PrintLine($"ArgumentException: {ex.Message}", errColor);
                Environment.Exit(3);
            }
            catch (Exception ex)
            {
                PrintLine($"{ex.GetType()}: {ex.Message}", errColor);
                Environment.Exit(4);
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

            try
            {
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
                        PrintFile($"{prefix}{vis.Pipe}", file);
                    }
                    else
                    {
                        PrintFile($"{prefix}{vis.Empty}", file);
                    }
                }

                ///// Then dirs.
                foreach (var dir in dirs)
                {
                    var fn = dir.FullName; // full path
                    var nm = dir.Name;

                    if (dir == dirs.Last())
                    {
                        PrintDir($"{prefix}{vis.Ell}", dir);
                        PrintTree(fn, $"{prefix}{vis.Empty}", depth + 1); // => recurse
                    }
                    else
                    {
                        PrintDir($"{prefix}{vis.Tee}", dir);
                        PrintTree(fn, $"{prefix}{vis.Pipe}", depth + 1); // => recurse
                    }
                }
            }
            catch (Exception ex)
            {
                // For some exceptions just log and continue.
                if (ex is UnauthorizedAccessException)
                {
                    PrintLine(ex.Message, errColor);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Print file info.
        /// </summary>
        /// <param name="fi"></param>
        void PrintFile(string prefix, FileInfo fi)
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

            string ext = fi.Extension.ToLower().Replace(".", "");
            var clr = extColors.TryGetValue(ext, out ConsoleColorEx clrex) ? clrex : ConsoleColorEx.None;
            Print($"{prefix}");
            PrintLine($"{fi.Name}{slen}", clr);
        }

        /// <summary>
        /// Print a directory info.
        /// </summary>
        /// <param name="di"></param>
        void PrintDir(string prefix, DirectoryInfo di)
        {
            Print($"{prefix}");
            PrintLine($"{di.Name}", dirColor);
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
                var fgColor = Console.ForegroundColor;
                Console.ForegroundColor = clr == ConsoleColorEx.None ? fgColor : (ConsoleColor)clr;
                Console.Write(text);
                Console.ForegroundColor = fgColor;
            }
            else
            {
                Console.Write(text);
            }
        }

        /// <summary>
        /// Low level write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="clr">Optional color</param>
        void PrintLine(string text, ConsoleColorEx clr = ConsoleColorEx.None)
        {
            Print(text, clr);
            Console.Write(Environment.NewLine);
        }

        /// <summary>Give some help.</summary>
        void PrintUsage()
        {
            PrintLine("treex [-c] [-f] [-d N] [-s] [-h] [dir]");
            PrintLine("opts:");
            PrintLine("    dir: start folder (default is current)");
            PrintLine("    -c: color output (default is off=monochrome)");
            PrintLine("    -u: unicode output otherwise ascii (default is ascii)");
            PrintLine("    -d num: maxDepth (default is 0 which means all)");
            PrintLine("    -f: show files (default is just dirs)");
            PrintLine("    -s: show file size (default is false)");
            PrintLine("    -e fld1,fld2,...: exclude directory(s)  (adds to config)");
            PrintLine("    -i fld1,fld2,...: unexclude directory(s)  (removes from config)");
            PrintLine("    -h help");
        }

        /// <summary>Start here.</summary>
        static void Main(string[] args)
        {
            var _ = new App(args);
        }
    }
}
