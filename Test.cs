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
        public Test()
        {
            PrintConsoleColors();

            SystemColorFromAnsi();

            SystemColorToConsoleColor();

            ConsoleColorToSystemColor();
        }

    }
}
