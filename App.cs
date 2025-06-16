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
        /// <summary>Settings</summary>
        //  readonly UserSettings _settings = new();

        readonly HashSet<string> image_files = [];
        readonly HashSet<string> audio_files = [];
        readonly HashSet<string> executable_files = [];
        readonly HashSet<string> binary_files = [];
        readonly HashSet<string> exclude_directories = [];

        // Visuals. Unicode is default.
        readonly string tee = "├── ";
        readonly string ell = "└── ";
        readonly string vert = "│   ";
        readonly string hor = "    ";

        // Default defaults.
        readonly string startFolder = Environment.CurrentDirectory;
        readonly bool includeFiles = true;
        readonly bool showSize = false;
        readonly bool asciiChars = true;
        readonly bool includeHidden = false;
        readonly int maxDepth = 0;
        //exclude_directories = .vs, bin, obj, ibin, iobj, x64, lib, .svn, .git, .hg, CVS, .github, __pycache__
        //image_files = jpeg, jpg, gif, png, ico, bmp, tga, psd, ppm, pgm, webp, hdr
        //audio_files = flac, m4a, mid, mp3, sty, wav, rpp, repeaks
        //executable_files = a, bin, dll, dylib, exe, lib, o, obj, pyc, pyo, so, class, jar
        //binary_files = chm, ctf, db, dds, docx, eot, idb, msi, ncb, out, pcs, pdb, pdf, prs, psd, sdf, sst, suo, swf, ttf, xls, xlsx, zip

        // Color defaults. TODO from ini?
        readonly ConsoleColor defaultColor = Console.ForegroundColor;
        readonly ConsoleColor dirColor = ConsoleColor.Blue;
        readonly ConsoleColor fileColor = ConsoleColor.Yellow;
        readonly ConsoleColor exeColor = ConsoleColor.Green;
        #endregion

        /// <summary>Build me one.</summary>
        public App()
        {
            try
            {
                ///// Init runtime values from default ini file. TODO1 or new one from cmd line?
                var inrdr = new IniReader(Path.Join(MiscUtils.GetSourcePath(), "treex.ini"));
                var section = inrdr.Contents["treex"];

                foreach (var val in section.Values)
                {
                    switch (val.Key)
                    {
                        case "include_files": includeFiles = bool.Parse(val.Value); break;
                        case "show_size": showSize = bool.Parse(val.Value); break;
                        case "ascii_chars": asciiChars = bool.Parse(val.Value); break;
                        case "include_hidden": includeHidden = bool.Parse(val.Value); break;
                        case "max_depth": maxDepth = int.Parse(val.Value); break;
                        case "image_files": image_files = [.. val.Value.SplitByToken(",")]; break;
                        case "audio_files": audio_files = [.. val.Value.SplitByToken(",")]; break;
                        case "executable_files": executable_files = [.. val.Value.SplitByToken(",")]; break;
                        case "binary_files": binary_files = [.. val.Value.SplitByToken(",")]; break;
                        case "exclude_directories": exclude_directories = [.. val.Value.SplitByToken(",")]; break;
                        default: throw new ArgumentException($"Invalid Section Value: {val.Key}");
                    }
                }

                ///// Process command line options.
                var args = Environment.GetCommandLineArgs().ToList();
                bool go = true;

                for (int i = 0; i < args.Count; i++)
                {
                    var arg = args[i];

                    if (i == 0 && !arg.StartsWith('-'))
                    {
                        CheckPath(arg);
                        startFolder = arg;
                        continue;
                    }

                    switch (arg)
                    {
                        case "-?":
                            PrintUsage();
                            go = false;
                            break;

                        case "-s":
                            showSize = true;
                            break;

                        case "-f":
                            includeFiles = true;
                            break;

                        case "-h":
                            includeHidden = true;
                            break;

                        case "-d": // N
                            maxDepth = int.Parse(args[++i]);
                            break;

                        case "-i": // fld 1,fld2,...
                            // add to default if not there.
                            List<string> iparts = args[++i].SplitByToken(";");
                            break;

                        case "-u": // fld1,fld2,...
                            // remove from default.

                            break;

                        default:
                            throw new ArgumentException($"Invalid argument: {arg}");
                    }
                }

                ///// Final fixups.
                if (asciiChars)
                {
                    tee = "+---";
                    ell = @"\---";
                    vert = "|   ";
                    hor = "    ";
                }

                /////
                if (go)
                {
                    PrintTree(startFolder, "", 0);
                }
            }
            catch (IniSyntaxException ex)
            {
                Console.WriteLine($"Syntax error({ex.LineNum}): {ex.Message}");
                // print something
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! {ex.Message}");
                // print something
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        void PrintTree(string dir, string prefix = "", int depth = 0)
        {
            if (maxDepth > 0 && depth >= maxDepth) return;

            var di = new DirectoryInfo(dir);

            //public FileSystemInfo[] GetFileSystemInfos(string searchPattern)
            //public FileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
            //     Includes only the current directory in a search operation.
            //TopDirectoryOnly = 0,
            //     Includes the current directory and all its subdirectories in a search operation.
            //     This option includes reparse points such as mounted drives and symbolic links
            //     in the search.
            //AllDirectories = 1

            var infos = di.GetFileSystemInfos();

            foreach (var fsi in infos)
            {
                // fsi.Extension;
                // Full path of the directory/file
                var fn = fsi.FullName;
                var n = fsi.Name;
                var isDir = (fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                var hidden = n[0] == '.';
                var last = fsi == infos.Last();

                switch (hidden, isDir, last)
                {
                   case (true, _, _):
                       continue;

                   case (false, true, false):
                        Print(prefix + tee);
                        PrintValue(fsi);
                        // recurse =>
                        PrintTree(fn, prefix + vert, depth + 1);
                        break;

                   case (false, false, false):
                        Print(prefix + tee);
                        PrintValue(fsi);
                        break;

                   case (false, true, true):
                        Print(prefix + ell);
                        PrintValue(fsi);
                        PrintTree(fn, prefix + hor, depth + 1);
                        break;

                   case (false, false, true):
                        Print(prefix + ell);
                        PrintValue(fsi);
                        break;
                }
            }

            // var fsItems = di.GetFileSystemInfos()
            //    .Where(f => ShowAll || !f.Name.StartsWith("."))
            //    .OrderBy(f => f.Name)
            //    .ToList();

            // foreach (var fsItem in fsItems.Take(fsItems.Count() - 1))
            // {
            //     Print(prefix + _tee);
            //     PrintValue(fsItem);
            //     NL();

            //     if (IsDirectory(fsItem))
            //     {
            //         PrintTree(fsItem.FullName, prefix + _vert, depth + 1);
            //     }
            // }

            // // last item is handled
            // var lastFsItem = fsItems.LastOrDefault();
            // if (lastFsItem != null)
            // {
            //     Print(prefix + _ell);
            //     PrintValue(lastFsItem);
            //     NL();

            //     if (IsDirectory(lastFsItem))
            //     {
            //         PrintTree(lastFsItem.FullName, prefix + _hor, depth + 1);
            //     }
            // }
        }

        /// <summary>
        /// Print one file system entry.
        /// </summary>
        /// <param name="fsi"></param>
        void PrintValue(FileSystemInfo fsi)
        {
            ConsoleColor clr = defaultColor;

            if (IsDirectory(fsi))
            {
               clr = dirColor;
            }
            else
            {
                string ext = Path.GetExtension(fsi.FullName).ToLower();
                //if (ExeExtensions.Contains(ext))
                //{
                //   clr = _exeColor;
                //}
                //else
                //{
                //    clr = _fileColor;
                //}
            }

            Print(fsi.Name, clr, true);
        }

        /// <summary>
        /// Low level write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="nl">Default is to add a nl.</param>
        void Print(string text, ConsoleColor? clr = null, bool nl = false)
        {
            Console.ForegroundColor = clr ?? defaultColor;
            Console.Write(text);
            Console.ForegroundColor = defaultColor;

            if (nl)
            {
                Console.Write(Environment.NewLine);
            }
        }

        /// <summary>
        /// Give some help.
        /// </summary>
        void PrintUsage()
        {
            // treex [maybe dir] [-f] [-h] [-d N] [-s] [-e] [-?] [-i fld 1,fld2,...] [-u fld1;fld 2;...]
            // >>>
            // treex maybe dir -f -d N -s -e -? -i fld 1;fld2;...; -u fld1;fld 2;...

            // - cmd line opts -- * has default in settings
            // [dir] - start folder or '.' if missing
            // incl files -f*
            // maxDepth -d num* 0 means all
            // hidden -h*
            // show size -s*
            // ignore folder(s) -i fld1;fld2;...*  add to default
            // unignore folder(s) -u fld1;fld2;...  remove from list
            // TODO un/ignore file.exts? regex?
            // commands::
            // edit|settings -e
            // help -?
        }

        ////////// TODO1 this stuff //////

        void CheckPath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException($"Invalid path: {path}");
            }
        }


        public bool IsDirectory(FileSystemInfo fsItem)
        {
            return (fsItem.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }



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
