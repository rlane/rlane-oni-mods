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
            if (false) {
                var input = YamlIO.LoadFile<TemplateContainer>(args[0]);
                Console.WriteLine("Loaded input");
                var result = Ruins.Ruins.MakeRuins(input);
                Console.WriteLine("Created ruins");
                YamlIO.Save(result, "tmp/ruins-out.yaml");
                Console.WriteLine("Saved output to tmp/ruins-out.yaml");
            }

            if (true)
            {
                Ruins.Ruins.Upload(new TemplateContainer());
            }
        }
    }
}
