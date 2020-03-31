using System;
using System.Collections.Generic;
using TemplateClasses;
using UnityEngine;
using static TemplateClasses.Prefab.Type;

namespace Ruins
{
    public static class Ruins
    {
        public static RuinsConfig config;

        class ScoredPrefab
        {
            public Prefab prefab;
            public double score;
        }

        public static TemplateContainer MakeRuins(TemplateContainer input)
        {
            var rng = new System.Random();
            var perlin = new PerlinNoise(rng.Next());
            var scored = new List<ScoredPrefab>();
            float xbase = (float)rng.NextDouble() * 1e3f;
            float ybase = (float)rng.NextDouble() * 1e3f;
            var hq = FindHeadquarters(input);
            foreach (var prefab in input.buildings)
            {
                if (Grid.CellCount > 0 && !Grid.IsValidBuildingCell(Grid.XYToCell(prefab.location_x, prefab.location_y)))
                {
                    continue;
                }
                else if (hq != null && Vector2.Distance(new Vector2(hq.location_x, hq.location_y), new Vector2(prefab.location_x, prefab.location_y)) < config.hq_radius)
                {
                    if (config.verbose)
                    {
                        Debug.Log("Ruins: skipping building " + prefab.id + " at (" + prefab.location_x + ", " + prefab.location_y + ") too close to Headquarters");
                    }
                    continue;
                }
                double perlin_score = PerlinSimplexNoise.noise(xbase + prefab.location_x * (float)config.perlin_zoom, ybase + prefab.location_y * (float)config.perlin_zoom) * config.perlin_factor;
                double building_id_score = ScoreBuildingId(prefab.id);
                double gaussian_score = GaussianRandom(rng, 0.0, config.gaussian_sigma);
                double score = perlin_score + building_id_score + gaussian_score;
                if (config.verbose)
                {
                    Debug.Log("Ruins: scored building " + prefab.id + " at (" + prefab.location_x + ", " + prefab.location_y + ") score " + score + " (perlin=" + perlin_score + ", building_id=" + building_id_score + ")");
                }
                if (score > double.NegativeInfinity)
                {
                    scored.Add(new ScoredPrefab { prefab = prefab, score = score });
                }
            }

            scored.Sort((ScoredPrefab a, ScoredPrefab b) => b.score.CompareTo(a.score));

            var template = new TemplateContainer();
            template.info = input.info;
            foreach (var item in scored)
            {
                if (template.buildings.Count >= config.max_buildings)
                {
                    break;
                }
                template.buildings.Add(item.prefab);
                if (config.verbose)
                {
                    Debug.Log("Ruins: selected building " + item.prefab.id + " at (" + item.prefab.location_x + ", " + item.prefab.location_y + ") score " + item.score);
                }
            }
            return template;
        }

        public static Prefab FindHeadquarters(TemplateContainer template)
        {
            return template.buildings.Find((x) => x.id == "Headquarters");
        }

        public static double GaussianRandom(System.Random rng, double mu = 0f, double sigma = 1f)
        {
            double num = rng.NextDouble();
            double num2 = rng.NextDouble();
            double num3 = Math.Sqrt(-2 * Math.Log(num) * Math.Sin(Math.PI * 2 * num2));
            return mu + sigma * num3;
        }

        public static double ScoreBuildingId(string id)
        {
            if (!config.building_scores.ContainsKey(id))
            {
                Debug.Log("Ruins: Missing score for building " + id);
                config.building_scores[id] = double.NegativeInfinity;
            }
            return config.building_scores[id];
        }

        public static TemplateContainer GetBuildings()
        {
            var template = new TemplateContainer();
            HashSet<GameObject> _excludeEntities = new HashSet<GameObject>();
            for (int i = 0; i < Components.BuildingCompletes.Count; i++)
            {
                BuildingComplete buildingComplete = Components.BuildingCompletes[i];
                if (_excludeEntities.Contains(buildingComplete.gameObject))
                {
                    continue;
                }
                Grid.CellToXY(Grid.PosToCell(buildingComplete), out int x, out int y);
                {
                    Orientation rotation = Orientation.Neutral;
                    var rotatable = buildingComplete.gameObject.GetComponent<Rotatable>();
                    if (rotatable != null)
                    {
                        rotation = rotatable.GetOrientation();
                    }
                    var primary_element = buildingComplete.GetComponent<PrimaryElement>();
                    if (primary_element == null)
                    {
                        continue;
                    }
                    Prefab prefab = new Prefab(buildingComplete.PrefabID().Name, Prefab.Type.Building, x, y, primary_element.ElementID, primary_element.Temperature, 0f, null, 0, rotation);
                    template.buildings.Add(prefab);
                    _excludeEntities.Add(buildingComplete.gameObject);
                }
            }
            // TODO power/gas/liquid/logic connections
            return template;
        }
    }
}
