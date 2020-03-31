using Klei;
using System;
using System.Collections.Generic;
using System.IO;


namespace Ruins
{
    public class RuinsConfig
    {
        public Dictionary<string, double> building_scores { get; set; }
        public bool verbose { get; set; }
        public string server { get; set; }
        public double perlin_zoom { get; set; }
        public double perlin_factor { get; set; }
        public double hq_radius { get; set; }
        public int max_buildings { get; set; }
        public double gaussian_sigma { get; set; }

        public static RuinsConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            var result = YamlIO.LoadFile<RuinsConfig>(path);
            if (result.building_scores == null)
                throw new ArgumentException("Missing field building_scores");
            return result;
        }
    }
}
