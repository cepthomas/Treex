using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


// TODO ignore folders is only extensions for now, add regexp or wildcards later.

namespace Treex // TODO1
{
    public class App //: IDisposable
    {
        #region Fields
        /// <summary>Settings</summary>
      //  readonly UserSettings _settings = new();
        #endregion

        // Color defaults. TODO from ini?
        ConsoleColor _defaultColor = Console.ForegroundColor;
        ConsoleColor _dirColor = ConsoleColor.Blue;
        ConsoleColor _fileColor = ConsoleColor.Yellow;
        ConsoleColor _exeColor = ConsoleColor.Green;

        // Default defaults.
        bool _includeFiles = true;
        bool _showSize = false;
        bool _asciiChars = true;
        bool _includeHidden = false;
        int _maxDepth = 0;
        HashSet<string> _ignoreDirs = [];

        // Unicode is default.
        string _tee =  "├── ";
        string _ell =  "└── ";
        string _vert = "│   ";
        string _hor =  "    ";

        /// <summary>Build me one.</summary>
        public App()
        {
            //var appDir = MiscUtils.GetAppDataDir("Treex", "Ephemera");
            //_settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            string? startFolder = Environment.CurrentDirectory;

            // Init runtime values from ini file.
            try
            {
                // Load the default ini file (from ??) - TODO1 or new one from cmd line?
                var inrdr = new IniReader(Path.Join(MiscUtils.GetSourcePath(), "treex.ini"));
                var section = inrdr.Contents["treex"];

                foreach (var val in section.Values)
                {
                    switch (val.Key)
                    {
                        case "include_files": _includeFiles = bool.Parse(val.Value); break;
                        case "show_size": _showSize = bool.Parse(val.Value); break;
                        case "ascii_chars": _asciiChars = bool.Parse(val.Value); break;
                        case "_include_hidden": _includeHidden = bool.Parse(val.Value); break;

                        case "max_depth": _maxDepth = int.Parse(val.Value); break;

                        case "image_files":
                            var image_files = val.Value.SplitByToken(",");


                            break;


                        default:
                            throw new ArgumentException($"Invalid Section Value: {val.Key}");
                    }
                    //max_depth = 3


                    //; like bin, obj, ...
                    //exclude_directories = .vs, bin, obj, ibin, iobj, x64, lib, .svn, .git, .hg, CVS, .github, __pycache__

                    //image_files = jpeg, jpg, gif, png, ico, bmp, tga, psd, ppm, pgm, webp, hdr

                    //audio_files = flac, m4a, mid, mp3, sty, wav, rpp, repeaks

                    //executable_files = a, bin, dll, dylib, exe, lib, o, obj, pyc, pyo, so, class, jar

                    //binary_files = chm, ctf, db, dds, docx, eot, idb, msi, ncb, out, pcs, pdb, pdf, prs, psd, sdf, sst, suo, swf, ttf, xls, xlsx, zip
                }




                //_includeFiles = _settings.IncludeFiles;
                //_showSize = _settings.ShowSize;
                ////_asciiChars = _settings.AsciiChars;
                //_maxDepth = _settings.MaxDepth;
                //_hidden = _settings.IncludeHidden;
                //_ignoreDirs = _settings.IgnoreDirectories.ToHashSet();

            }
            catch (IniSyntaxException ex)
            {
                Console.WriteLine($"Syntax error({ex.LineNum}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! {ex.Message}");
            }



            // Process command line.
            var args = Environment.GetCommandLineArgs().ToList();
            // foreach (var arg in args)
            // {
            //     Console.WriteLine($"[{arg}]");
            // }
            // Environment.Exit(0);

            // Process args.
            bool go = true;
            bool valid = true; //args.Count >= 1 && args[0].Contains("Treex.dll");
            // int ind = 1;

            try
            {
                for (int i = 1; i < args.Count; i++)
                {
                    var arg = args[i];
                    if (i == 1 && !arg.StartsWith('-'))
                    {
                        startFolder = arg;
                    }
                    else
                    {
                        switch (arg)
                        {
                            case "-?":
                                PrintUsage();
                                go = false;
                                break;

                            //case "-e":
                            //    var edres = SettingsEditor.Edit(_settings, "Treex", 500);
                            //    _settings.Save();
                            //    go = false;
                            //    break;

                            case "-s":
                                _showSize = true;
                                break;

                            case "-f":
                                _includeFiles = true;
                                break;

                            case "-h":
                                _hidden = true;
                                break;

                            case "-d": // N
                                _maxDepth = int.Parse(args[++i]);
                                break;

                            case "-i": // fld 1;fld2;...;
                                // add to default if not there.
                                List<string> iparts = args[++i].SplitByToken(";");

                                break;

                            case "-u": // fld1;fld 2;...
                                // remove from default.

                                break;

                            default:
                                throw new ArgumentException($"Invalid argument: {arg}");
                                // valid = false;
                        }
                    }
                }



            }
            catch (Exception e)
            {
                valid = false;
                // print something
                Environment.Exit(1);
            }



            if (go)
            {
                // Check supplied args.
                if (startFolder is null)
                {
                    startFolder = Environment.CurrentDirectory;
                }
                else
                {
                    CheckPath(startFolder);
                }

                // Check ignore paths.

                // Check maxDepth.

                if (_asciiChars)
                {
                    _tee = "+---";
                    _ell = @"\---";
                    _vert = "|   ";
                    _hor = "    ";
                }

            }

            Environment.Exit(0);

        }


        void CheckPath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException($"Invalid path: {path}");
            }
        }

        void PrintUsage()
        {
            // treex [maybe dir] [-f] [-h] [-d N] [-s] [-e] [-?] [-i fld 1;fld2;...;] [-u fld1;fld 2;...]
            // >>>
            // treex maybe dir -f -d N -s -e -? -i fld 1;fld2;...; -u fld1;fld 2;...

            // - cmd line opts -- * has default in settings
            // [dir] - start folder or '.' if missing
            // incl files -f*
            // maxDepth -d num* 0 means all
            // hidden -h*
            // show size -s*
            // ignore folder(s) -i fld1;fld2;...*
            // unignore folder(s) -u fld1;fld2;...
            // TODO un/ignore file.exts? regex?
            // commands::
            // edit|settings -e
            // help -?


        }


        void PrintTree(string dir, string prefix = "", int depth = 0)
        {
            if (depth >= _maxDepth) return;

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
                //fsi.Extension;
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
                        Print(prefix + _tee);
                        PrintValue(fsi);
                        // NL();
                        // recurse
                        PrintTree(fn, prefix + _vert, depth + 1);
                        break;

                   case (false, false, false):
                        Print(prefix + _tee);
                        PrintValue(fsi);
                        // NL();
                        break;

                   case (false, true, true):
                        Print(prefix + _ell);
                        PrintValue(fsi);
                        // NL();
                        PrintTree(fn, prefix + _hor, depth + 1);

                        break;

                   case (false, false, true):
                        Print(prefix + _ell);
                        PrintValue(fsi);
                        // NL();

                        break;
                }

                // if (hidden)
                // {
                //     continue;
                // }

                // if (isDir)
                // {
                //     if (_ignoreDirs.Contains(n))
                //     {
                //         continue;
                //     }
                // }
                // else if (_includeFiles)
                // {
                // }

                // if ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                // {
                // }
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
        /// Write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="nl">Default is to add a nl.</param>
        void Print(string text, ConsoleColor? clr = null, bool nl = false)
        {
            Console.ForegroundColor = clr ?? _defaultColor;
            Console.Write(text);
            Console.ForegroundColor = _defaultColor;

            if (nl)
            {
                Console.Write(Environment.NewLine);
            }
        }

        void PrintValue(FileSystemInfo fsi)
        {
            ConsoleColor clr = _defaultColor;

            if (IsDirectory(fsi))
            {
               clr = _dirColor;
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

        public bool IsDirectory(FileSystemInfo fsItem)
        {
            return (fsItem.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }

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
