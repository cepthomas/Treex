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
        readonly bool showDirs = false;
        readonly bool showSize = false;
        readonly bool ascii = true;
        readonly bool color = true;
        readonly int maxDepth = 0;
//        readonly ConsoleColor defaultColor = Console.ForegroundColor;
        readonly ConsoleColorEx dirColor = ConsoleColorEx.None;
        //readonly ConsoleColorEx fileColor = ConsoleColorEx.None;
        //readonly ConsoleColorEx exeColor = ConsoleColorEx.None;
        //readonly ConsoleColorEx binColor = ConsoleColorEx.None;
        readonly ConsoleColorEx errColor = ConsoleColorEx.None;


        #endregion

        /// <summary>Build me one.</summary>
        public App(string[] args)
        {
            int code = 0;

            try
            {
                // Default start location.
                string startDir = Environment.CurrentDirectory;
startDir = "C:\\Dev\\Misc";

                // Init runtime values from ini file. Implement or rename TOOLS_PATH here and in csproj file.
                var exe = Environment.GetEnvironmentVariable("TOOLS_PATH");
                var inrdr = new IniReader(Path.Join(exe, "treex.ini"));
                var section = inrdr.Contents["treex"];
                //HashSet<string> imageFiles = [];
                //HashSet<string> audioFiles = [];
                //HashSet<string> executableFiles = [];
                //HashSet<string> binaryFiles = [];

                //Dictionary<string, ConsoleColorEx> extColors = [];

                foreach (var val in section.Values)
                {
                    switch (val.Key)
                    {
                        case "show_dirs": showDirs = bool.Parse(val.Value); break;
                        case "show_size": showSize = bool.Parse(val.Value); break;
                        case "ascii": ascii = bool.Parse(val.Value); break;
                        case "max_depth": maxDepth = int.Parse(val.Value); break;

                        //case "image_files": imageFiles = ReadList(); break;
                        //case "audio_files": audioFiles = ReadList(); break;
                        //case "executable_files": executableFiles = ReadList(); break;
                        //case "binary_files": binaryFiles = ReadList(); break;

                        case "exclude_directories":
                            var dparts = val.Value.SplitByToken(",");
                            dparts.ForEach(p => excludeDirectories.Add(p));// res.Add(fixext && !p.StartsWith('.') ? "." + p.ToLower() : p.ToLower()));
                          //  excludeDirectories = ReadList(false);
                          break;

                        case "dir_color": dirColor = ReadColor(); break;
//                        case "file_color": fileColor = ReadColor(); break;
  //                      case "exe_color": exeColor = ReadColor(); break;
    //                    case "bin_color": binColor = ReadColor(); break;
                        case "err_color": errColor = ReadColor(); break;


                        case string s when s.Contains("_files"):
                            var fparts = val.Value.SplitByToken(",");
                            if (fparts.Count < 2 || !Enum.TryParse(fparts[0], true, out ConsoleColorEx pclr))
                            {
                                throw new IniSyntaxException($"Invalid section value for {val.Key}", -1);
                            }
                            fparts.Take(1..).ForEach(p => extColors.Add(p.Replace(".", ""), pclr));
                            break;

                        default: throw new IniSyntaxException($"Invalid section value for {val.Key}", -1);
                    }

                    #region Parse helpers
                    //HashSet<string> ReadListX(bool fixext = true)
                    //{
                    //    HashSet<string> res = [];
                    //    var parts = val.Value.SplitByToken(",");
                    //    parts.ForEach(p => res.Add(fixext && !p.StartsWith('.') ? "." + p.ToLower() : p.ToLower()));
                    //    return res;
                    //}

                    ConsoleColorEx ReadColor()
                    {
                        if (Enum.TryParse(val.Value, true, out ConsoleColorEx pclr))
                        {
                            return pclr;
                        }
                        throw new IniSyntaxException($"Invalid color for {val.Key}", -1);
                    }
                    #endregion
                }

                ///// Process command line options.
                // treex [-f] [-d N] [-s] [-?] [-i fld 1,fld2,...] [-u fld1,fld2,...] [dir]
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    switch (arg)
                    {
                        case "-s":
                            showSize = true;
                            break;

                        case "-d":
                            showDirs = true;
                            break;

                        case "-c":
                            color = false;
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
                vis = ascii ? visAscii : visUnicode;

                //imageFiles.ForEach(f => extColors[f] = binColor);
                //audioFiles.ForEach(f => extColors[f] = binColor);
                //binaryFiles.ForEach(f => extColors[f] = binColor);
                //executableFiles.ForEach(f => extColors[f] = exeColor);

                ///// Do it, stewart /////
                Print(startDir);
            //    Print(Environment.NewLine);
                PrintTree(startDir, "", 0);
            }
            catch (IniSyntaxException ex)
            {
                Print($"IniSyntaxException at {ex.LineNum}: {ex.Message}", errColor);
                code = 1;
            }
            catch (ArgumentException ex)
            {
                Print($"ArgumentException: {ex.Message}", errColor);
                code = 2;
            }
            catch (Exception ex)
            {
                Print($"{ex.GetType()}: {ex.Message}", errColor);
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

            try
            {
                var di = new DirectoryInfo(startDir);

                ///// Collect contents first.
                var files = showDirs ? [] : di.GetFiles().ToList();
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
                  //      Print($"{prefix}{vis.Pipe}");
                        PrintFile($"{prefix}{vis.Pipe}", file);
                    }
                    else
                    {
                  //      Print($"{prefix}{vis.Empty}");
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
                 //       Print($"{prefix}{vis.Ell}");
                        PrintDir($"{prefix}{vis.Ell}", dir);
                        PrintTree(fn, $"{prefix}{vis.Empty}", depth + 1); // => recurse

                    }
                    else
                    {
                 //       Print($"{prefix}{vis.Tee}");
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
                    Print(ex.Message, errColor);
              //      Print(Environment.NewLine);
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
            Print($"{prefix}{fi.Name}{slen}", clr);
     //       Print(Environment.NewLine);
        }

        /// <summary>
        /// Print a directory info.
        /// </summary>
        /// <param name="di"></param>
        void PrintDir(string prefix, DirectoryInfo di)
        {
            Print($"{prefix}{di.Name}", dirColor);
      //      Print(Environment.NewLine);
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
            Console.Write(Environment.NewLine);
        }

        /// <summary>Give some help.</summary>
        void PrintUsage()
        {
            Print("treex [-c] [-f] [-d N] [-s] [-?] [-e fld 1,fld2,...] [-i fld1,fld2,...] [dir]");
            Print("opts:  * indicates default in settings");
            Print("    dir: start folder or current if missing");
            Print("    -c: color output off");
            Print("    -d num*: maxDepth (0 means all)");
            Print("    -f*: show files");
            Print("    -s*: show size (file only)");
            Print("    -e fld1,fld2,...*: exclude directory(s)  (adds to default)");
            Print("    -i fld1,fld2,...*: unexclude directory(s)  (removes to default)");
            Print("    -? help");
            
            //for (int i = (int)ConsoleColorEx.Black; i <= (int)ConsoleColorEx.White; i++)
            //{
            //    Print($"{(ConsoleColorEx)i}", (ConsoleColorEx)i);
            //}
        }

        /// <summary>Start here.</summary>
        static void Main(string[] args)
        {
            var _ = new App(args);
        }
    }
}
