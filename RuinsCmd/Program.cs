using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Klei;
using RuinsCmd;

namespace RuinsCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            Ruins.Ruins.verbose = true;
            string mode = args[0];
            if (mode == "mapgen")
            {
                if (args.Length != 3)
                {
                    throw new ArgumentException("'mapgen' requires 2 arguments");
                }
                var input = YamlIO.LoadFile<TemplateContainer>(args[1]);
                Console.WriteLine("Loaded input");
                var result = Ruins.Ruins.MakeRuins(input);
                Console.WriteLine("Created ruins");
                YamlIO.Save(result, args[2]);
                Console.WriteLine("Saved output to " + args[2]);
            }
            else if (mode == "upload")
            {
                if (args.Length != 2)
                {
                    throw new ArgumentException("'upload' requires 1 argument");
                }
                var template = YamlIO.LoadFile<TemplateContainer>(args[1]);
                Ruins.Net.Upload(template);
            }
            else if (mode == "download")
            {
                if (args.Length != 2)
                {
                    throw new ArgumentException("'download' requires 1 argument");
                }
                var template = Ruins.Net.Download();
                YamlIO.Save(template, args[1]);
            }
        }
    }
}
