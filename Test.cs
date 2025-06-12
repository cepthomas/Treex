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
            try
            {
                var irdr = new IniReader("C:\\Dev\\Apps\\Treex\\treex.ini");

                foreach (var section in irdr.Contents.Keys)
                {
                    Console.WriteLine($"Section: {section}");
                    foreach (var item in irdr.Contents[section])
                    {
                        Console.WriteLine($"    {item.Key}={item.Value}");
                    }
                }
            }
            catch (IniSyntaxException ex)
            {
                Console.WriteLine($"Syntax error({ex.LineNum}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! {ex.Message}");
            }

            //var ini = new IniFile("C:\\Dev\\Apps\\Treex\\treex.ini");

            //IniStreamConfigurationProvider pp = new();
            //var ini2 = pp.Read("C:\\Dev\\Apps\\Treex\\treex.ini");
        }
    }
}
