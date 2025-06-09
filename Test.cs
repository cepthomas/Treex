using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
//using Ephemera.NBagOfUis;


namespace Treex
{

    public class Test
    {
        #region Types
        //https://stackoverflow.com/questions/1988833/converting-color-to-consolecolor
        readonly record struct ConsoleColorInfo
        (
            /// <summary>In ConsoleColor enum</summary>
            int Index,
            /// <summary>From ConsoleColor enum</summary>
            string Name,
            /// <summary>See ref</summary>
            uint EgaBRGB,
            /// <summary>From code maybe</summary>
            uint ActualRGB,
            /// <summary>From ??</summary>
            uint Actual2RGB,
            /// <summary>Color.Name vaue</summary>
            uint SysColorOfName,
            /// <summary>Screen sample</summary>
            uint ScreenShotSample,
            /// <summary></summary>
            uint MySet
        );
        #endregion

        // use Actual2RGB except DarkYellow which is closer to ScreenShotSample
        // #BA8E23. This color is a darker, more subdued shade of yellow, often described as similar to mustard or ochre.
        // Other shades of dark yellow can be represented by hex codes like #9b870c, #8B8000, or #FFA600.  #d7c32a
        // 0xffd700 gold rrggbb

        readonly ConsoleColorInfo[] _consColors =
        [
            //                          EGA      Actual    Actual2   SysColor  ScrShot   My
            new(    0, "Black",         0x0000,  0x000000, 0x000000, 0x000000, 0x000000, 0x000000),
            new(    1, "DarkBlue",      0x0001,  0x000080, 0x000080, 0x00008B, 0x3465A4, 0x000080),
            new(    2, "DarkGreen",     0x0010,  0x008000, 0x008000, 0x006400, 0x4E9A06, 0x008000),
            new(    3, "DarkCyan",      0x0011,  0x008080, 0x008080, 0x008B8B, 0x06989A, 0x008080),
            new(    4, "DarkRed",       0x0100,  0x800000, 0x800000, 0x8B0000, 0xCC0000, 0x800000),
            new(    5, "DarkMagenta",   0x0101,  0x012456, 0x800080, 0x8B008B, 0x75507B, 0x800080),
            new(    6, "DarkYellow",    0x0110,  0xEEEDF0, 0x808000, 0x000000, 0xC4A000, 0xBA8E23),
            new(    7, "Gray",          0x0111,  0xC0C0C0, 0xC0C0C0, 0x808080, 0xD3D7CF, 0xC0C0C0),
            new(    8, "DarkGray",      0x1000,  0x808080, 0x808080, 0xA9A9A9, 0x555753, 0x808080),
            new(    9, "Blue",          0x1001,  0x0000FF, 0x0000FF, 0x0000FF, 0x729FCF, 0x0000FF),
            new(   10, "Green",         0x1010,  0x00FF00, 0x00FF00, 0x008000, 0x8AE234, 0x00FF00),
            new(   11, "Cyan",          0x1011,  0x00FFFF, 0x00FFFF, 0x00FFFF, 0x34E2E2, 0x00FFFF),
            new(   12, "Red",           0x1100,  0xFF0000, 0xFF0000, 0xFF0000, 0xEF2929, 0xFF0000),
            new(   13, "Magenta",       0x1101,  0xFF00FF, 0xFF00FF, 0xFF00FF, 0xAD7FA8, 0xFF00FF),
            new(   14, "Yellow",        0x1110,  0xFFFF00, 0xFFFF00, 0xFFFF00, 0xFCE94F, 0xFFFF00),
            new(   15, "White",         0x1111,  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xEEEEEC, 0xFFFFFF),
        ];



        public Test()
        {
            PrintConsoleColors();

            SystemColorFromAnsi();

            SystemColorToConsoleColor();

            ConsoleColorToSystemColor();
        }

        void PrintConsoleColors()
        {
            Console.WriteLine("");
            Console.WriteLine($"===== PrintConsoleColors ======");

            List<string> ross =
            [
                "You have freedom here.",
                "The only guide is your heart.",
                "We can always carry this a step further.",
                "There's really no end to this.",
                "Let's give him a friend too.",
                "Everybody needs a friend.",
                "Follow the lay of the land.",
                "It's most important.",
                "Only eight colors that you need.",
                "Now we can begin working on lots of happy little things.",
                "Even the worst thing we can do here is good.",
                "Let's do it again then, what the heck.",
                "Everything's not great in life, but we can still find beauty in it.",
                "Use what happens naturally, don't fight it.",
                "How do you make a round circle with a square knife? ",
                "That's your challenge for the day."
            ];

            var cvals = Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().ToList();

            for (int i = 0; i < cvals.Count; i++)
            {
                //Console.ForegroundColor = (ConsoleColor)i;
                Console.BackgroundColor = (ConsoleColor)i;
                //Console.BackgroundColor = (ConsoleColor)(cvals.Count - i - 1);
                Console.WriteLine($"FG:{Console.ForegroundColor} BG:{Console.BackgroundColor} {ross[i]}");
                Console.ResetColor();
            }
        }

        void SystemColorToConsoleColor()
        {
            Console.WriteLine("");
            Console.WriteLine($"===== SystemColorToConsoleColor ======");

            List<Color> colors =
            [
                Color.Black,
                Color.DarkBlue,
                Color.DarkGreen,
                Color.DarkCyan,
                Color.DarkRed,
                Color.DarkMagenta,
                Color.Gold, // was DarkYellow,
                Color.Gray,
                Color.DarkGray,
                Color.Blue,
                Color.Green,
                Color.Cyan,
                Color.Red,
                Color.Magenta,
                Color.Yellow,
                Color.White
            ];

            List<string> html = [];
            html.Add("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
            foreach (var clr in colors)
            {
                var conclr1 = GetConsoleColor1(clr);
                var conclr2 = GetConsoleColor2(clr);
                //html.Add($"<span style=\"font-size: 2.0em; background-color: #{clr.ToArgb()}; color: #ffffff; \">System:{clr.Name}</span><span style=\"font-size: 2.0em; background-color: #{conclr.}; color: #ffffff; \">Console:{conclr}</span><br>");
                html.Add($"<span style=\"font-size: 2.0em; background-color: {clr.Name}; color: #ffffff; \">System:{clr.Name}   Console1:{conclr1}   Console2:{conclr2}</span><br>");
            }
            html.Add("</body></html>");

            File.WriteAllLines(@"C:\Dev\Treex\SystemColorToConsoleColor.html", html);
        }



        void ConsoleColorToSystemColor()
        {
            Console.WriteLine("");
            Console.WriteLine($"===== ConsoleColorToSystemColor ======");

            // string tohex(int num)
            // {
            //     string s = string.Format("6x", num);

            //     //string.format("0x{0:X8}", string_to_modify), which yields "0x00000C20".

            //     return s;
            // }

            List<string> html = [];
            html.Add("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
            for (int i = 0; i < _consColors.Count(); i++)
            {
                var cc = _consColors[i];
                html.Add($"<span style=\"font-size: 20pt; background-color: #ffffff; color: #000000;\">{cc.Name} |");
                html.Add($"<span style=\"background-color: #{cc.ActualRGB:x6}; color: #ffffff;\">ActualRGB |");
                html.Add($"<span style=\"background-color: #{cc.Actual2RGB:x6}; color: #ffffff;\">Actual2RGB |");
                html.Add($"<span style=\"background-color: #{cc.ScreenShotSample:x6}; color: #ffffff;\">ScreenShotSample |");
                html.Add($"<span style=\"background-color: #{cc.MySet:x6}; color: #ffffff;\">MySet |");
                html.Add($"<br>");


                //html.Add($"<span style=\"font-size: 2.0em; background-color: #{cc.ActualRGB:0x}; color: #ffffff; >ActualRGB:{cc.Name}  background-color: #{cc.Actual2RGB:0x}; color: #ffffff; >Actual2RGB:{cc.Name}   background-color: #{cc.ScreenShotSample:0x}; color: #ffffff; \">ScreenShotSample:{cc.Name}   </span><br>");
            }
            html.Add("</body></html>");
            File.WriteAllLines(@"C:\Dev\Treex\ConsoleColorToSystemColor.html", html);


            //readonly record struct ConsoleColorInfo
            //(
            //    /// <summary>In ConsoleColor enum</summary>
            //    int Index,
            //    /// <summary>From ConsoleColor enum</summary>
            //    string Name,
            //    /// <summary>See ref</summary>
            //    uint EgaBRGB,
            //    /// <summary>From code maybe</summary>
            //    uint ActualRGB,
            //    /// <summary>From ??</summary>
            //    uint Actual2RGB,
            //    /// <summary>Color.Name vaue</summary>
            //    uint SysColorOfName,
            //    /// <summary>Screen sample</summary>
            //    uint ScreenShotSample
            //);







            // Actual: default console color table:
            // Actual2: ??

            // Index Name                 EGA Brgb  Actual   Actual2   Sys Color of name           Screen shot sample #FFRRGGBB
            //     0 Black                    0000  #000000  #000000   #000000    #000000
            //     1 DarkBlue                 0001  #000080  #000080   #00008B    #3465A4
            //     2 DarkGreen                0010  #008000  #008000   #006400    #4E9A06
            //     3 DarkCyan                 0011  #008080  #008080   #008B8B    #06989A
            //     4 DarkRed                  0100  #800000  #800000   #8B0000    #CC0000
            //     5 DarkMagenta              0101  #012456  #800080   #8B008B    #75507B
            //     6 DarkYellow               0110  #EEEDF0  #808000   #000000    #C4A000
            //     7 Gray                     0111  #C0C0C0  #C0C0C0   #808080    #D3D7CF
            //     8 DarkGray                 1000  #808080  #808080   #A9A9A9    #555753
            //     9 Blue                     1001  #0000FF  #0000FF   #0000FF    #729FCF
            //    10 Green                    1010  #00FF00  #00FF00   #008000    #8AE234
            //    11 Cyan                     1011  #00FFFF  #00FFFF   #00FFFF    #34E2E2
            //    12 Red                      1100  #FF0000  #FF0000   #FF0000    #EF2929
            //    13 Magenta                  1101  #FF00FF  #FF00FF   #FF00FF    #AD7FA8
            //    14 Yellow                   1110  #FFFF00  #FFFF00   #FFFF00    #FCE94F
            //    15 White                    1111  #FFFFFF  #FFFFFF   #FFFFFF    #EEEEEC
        }



        // readonly CommandDescriptor[] _commands =
        // [
        //     new("help",     '?',  "available commands",            "",                      UsageCmd),
        //     new("info",     'i',  "system information",            "",                      InfoCmd),
        //     new("exit",     'q',  "exit the application",          "",                      ExitCmd),
        //     new("run",      'r',  "toggle running the script",     "",                      RunCmd),
        //     new("position", 'p',  "set position or tell current",  "[pos]",                 PositionCmd),
        //     new("loop",     'l',  "set loop or tell current",      "[start end]",           LoopCmd),
        //     new("rewind",   'w',  "rewind loop",                   "",                      RewindCmd),
        //     new("tempo",    't',  "get or set the tempo",          "[40-240]",              TempoCmd),
        //     new("monitor",  'm',  "toggle monitor midi traffic",   "[r=rcv|s=snd|o=off]",   MonCmd),
        //     new("kill",     'k',  "stop all midi",                 "",                      KillCmd),
        //     new("reload",   's',  "reload current script",         "",                      ReloadCmd)
        // ];



        void SystemColorFromAnsi()
        {
            Console.WriteLine("");
            Console.WriteLine($"===== ConvertFromAnsi ======");

            // ESC = \u001b
            // One of: ESC[IDm  ESC[38;5;IDm  ESC[48;5;IDm  ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm


            var (fg, bg) = Ansi.ColorFromAnsi("bad string");
            Console.WriteLine($"{fg.Name} {bg.Name}");

            (fg, bg) = Ansi.ColorFromAnsi("\u001b[34m");
            Console.WriteLine($"{fg} {bg}");

            (fg, bg) = Ansi.ColorFromAnsi("\u001b[45m");
            Console.WriteLine($"{fg} {bg}");

            // system
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[38;5;12m");
            Console.WriteLine($"{fg} {bg}");

            // id
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[38;5;122m");
            Console.WriteLine($"{fg} {bg}");

            // grey
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[38;5;249m");
            Console.WriteLine($"{fg} {bg}");

            // id bg
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[48;5;231m");
            Console.WriteLine($"{fg} {bg}");

            //ESC[38;2;R;G;Bm
            // rgb
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[38;2;204;39;187m");
            Console.WriteLine($"{fg} {bg}");

            // rgb invert
            (fg, bg) = Ansi.ColorFromAnsi("\u001b[48;2;19;0;222m");
            Console.WriteLine($"{fg} {bg}");
        }



        /////////////////////////////////////////////////////////////////////////
        ////////////////////////// stolen ///////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////

        ConsoleColor GetConsoleColor1(Color color)
        {
            if (color.GetSaturation() < 0.5)
            {
                // we have a grayish color
                switch ((int)(color.GetBrightness() * 3.5))
                {
                    case 0: return ConsoleColor.Black;
                    case 1: return ConsoleColor.DarkGray;
                    case 2: return ConsoleColor.Gray;
                    default: return ConsoleColor.White;
                }
            }

            int hue = (int)Math.Round(color.GetHue() / 60, MidpointRounding.AwayFromZero);
            if (color.GetBrightness() < 0.4)
            {
                // dark color
                switch (hue)
                {
                    case 1: return ConsoleColor.DarkYellow;
                    case 2: return ConsoleColor.DarkGreen;
                    case 3: return ConsoleColor.DarkCyan;
                    case 4: return ConsoleColor.DarkBlue;
                    case 5: return ConsoleColor.DarkMagenta;
                    default: return ConsoleColor.DarkRed;
                }
            }

            // bright color
            switch (hue)
            {
                case 1: return ConsoleColor.Yellow;
                case 2: return ConsoleColor.Green;
                case 3: return ConsoleColor.Cyan;
                case 4: return ConsoleColor.Blue;
                case 5: return ConsoleColor.Magenta;
                default: return ConsoleColor.Red;
            }
        }


        ConsoleColor GetConsoleColor2(Color c)
        //public ConsoleColor FromColor(Color c)
        {
            int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0; // Bright bit
            index |= (c.R > 64) ? 4 : 0; // Red bit
            index |= (c.G > 64) ? 2 : 0; // Green bit
            index |= (c.B > 64) ? 1 : 0; // Blue bit
            return (ConsoleColor)index;
        }



        // Public Shared Function ColorToConsoleColor(cColor As Color) As ConsoleColor
        //         Dim cc As ConsoleColor
        //         If Not System.Enum.TryParse(Of ConsoleColor)(cColor.Name, cc) Then
        //             Dim intensity = If(Color.Gray.GetBrightness() < cColor.GetBrightness(), 8, 0)
        //             Dim r = If(cColor.R >= &H80, 4, 0)
        //             Dim g = If(cColor.G >= &H80, 2, 0)
        //             Dim b = If(cColor.B >= &H80, 1, 0)

        //             cc = CType(intensity + r + g + b, ConsoleColor)
        //         End If
        //         Return cc
        //     End Function


    }
}
