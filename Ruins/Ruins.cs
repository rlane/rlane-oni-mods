using System.Collections.Generic;
using TemplateClasses;
using UnityEngine;
using static TemplateClasses.Prefab.Type;

namespace Ruins
{
    public static class Ruins
    {
        public static bool verbose = false;

        class ScoredPrefab
        {
            public Prefab prefab;
            public double score;
        }

        public static TemplateContainer MakeRuins(TemplateContainer input)
        {
            var rng = new System.Random();
            int max_buildings = 2000;  // TODO make confgurable
            var perlin = new PerlinNoise(rng.Next());
            var scored = new List<ScoredPrefab>();
            float xbase = (float)rng.NextDouble() * 1e3f;
            float ybase = (float)rng.NextDouble() * 1e3f;
            var perlin_scale = 0.01f;
            var hq_radius = 10.0;
            var hq = FindHeadquarters(input);
            foreach (var prefab in input.buildings)
            {
                if (Grid.CellCount > 0 && !Grid.IsValidBuildingCell(Grid.XYToCell(prefab.location_x, prefab.location_y)))
                {
                    continue;
                }
                else if (hq != null && Vector2.Distance(new Vector2(hq.location_x, hq.location_y), new Vector2(prefab.location_x, prefab.location_y)) < hq_radius)
                {
                    if (verbose)
                    {
                        Debug.Log("Ruins: skipping building " + prefab.id + " at (" + prefab.location_x + ", " + prefab.location_y + ") too close to Headquarters");
                    }
                    continue;
                }
                double perlin_score = PerlinSimplexNoise.noise(xbase + prefab.location_x * perlin_scale, ybase + prefab.location_y * perlin_scale) * 5;
                double building_id_score = ScoreBuildingId(prefab.id);
                double gaussian_score = Util.GaussianRandom(0.0f, 0.1f);
                double score = perlin_score + building_id_score + gaussian_score;
                if (verbose)
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
                if (template.buildings.Count >= max_buildings)
                {
                    break;
                }
                template.buildings.Add(item.prefab);
                if (verbose)
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

        static Dictionary<string, double> building_id_scores = new Dictionary<string, double>
        {
            // Tile variants.
            {"Tile",  1.0},
            {"MeshTile",  1.0},
            {"GasPermeableMembrane",  1.0},
            {"GlassTile",  1.0},
            {"MetalTile",  1.0},
            {"PlasticTile",  1.0},

            // Tile-like buildings.
            {"FarmTile",  0.5},
            {"HydroponicFarm", 0.5},
            {"WireRefinedBridgeHighWattage", 0.5},
            {"WireBridgeHighWattage", 0.5},
            {"InsulationTile",  0.4},
            {"FloorSwitch", 0.5},
            {"TravelTubeWallBridge", 0.5},

            // Backwall buildings
            {"ExteriorWall",  0.3},  // Drywall
            {"ThermalBlock",  0.7},  // Tempshift plate

            // Buildings often found in duplicant living quarters.
            {"DiningTable",  0.5},
            {"WaterCooler", 0.5},
            {"MedicalCot", 0.5},
            {"CeilingLight", 0.5},
            {"FloorLamp", 0.5},
            {"LuxuryBed", 0.5},
            {"Outhouse", 0.5},
            {"MassageTable", 0.5},

            // Big buildings
            {"MetalRefinery", 0.1},
            {"OilRefinery", 0.1},
            {"AdvancedResearchCenter", 0.1},
            {"CosmicResearchCenter", 0.1},
            {"Desalinator", 0.1},
            {"GlassForge", 0.1},
            {"GourmetCookingStation", 0.1},
            {"OxyliteRefinery", 0.1},
            {"SuitFabricator", 0.1},
            {"SupermaterialRefinery", 0.1},
            {"Telescope", 0.1},
            {"RockCrusher", 0.1},
            {"LiquidConditioner", 0.1},

            // No adjustment for these buildings.
            {"Apothecary", 0.0},
            {"BottleEmptierGas", 0.0},
            {"Compost", 0.0},
            {"EggCracker", 0.0},
            {"GasBottler", 0.0},
            {"LiquidMiniPump", 0.0},
            {"Polymerizer", 0.0},
            {"PowerControlStation", 0.0},
            {"ResearchCenter", 0.0},
            {"WaterPurifier", 0.0},
            {"AirConditioner", 0.0},
            {"AutoMiner", 0.0},
            {"Gantry", 0.0},
            {"PowerTransformerSmall", 0.0},
            {"CookingStation", 0.0},
            {"Electrolyzer", 0.0},
            {"MineralDeoxidizer", 0.0},
            {"ParkSign", 0.0},
            {"HydrogenGenerator", 0.0},
            {"MethaneGenerator", 0.0},
            {"PetroleumGenerator", 0.0},
            {"Refrigerator", 0.0},
            {"FlushToilet", 0.0},
            {"LiquidPumpingStation", 0.0},
            {"Shower", 0.0},
            {"WashSink", 0.0},
            {"JetSuitLocker", 0.0},
            {"SteamTurbine2", 0.0},
            {"Generator", 0.0},
            {"CreatureFeeder", 0.0},
            {"ManualGenerator", 0.0},
            {"RanchStation", 0.0},
            {"EggIncubator", 0.0},
            {"Kiln", 0.0},
            {"ObjectDispenser", 0.0},
            {"BottleEmptier", 0.0},
            {"CreatureDeliveryPoint", 0.0},
            {"BatterySmart", 0.0},
            {"LiquidReservoir", 0.0},
            {"LiquidPump", 0.0},
            {"PowerTransformer", 0.0},
            {"SuitLocker", 0.0},
            {"GasReservoir", 0.0},
            {"PlanterBox", 0.0},
            {"GasPump", 0.0},
            {"SolidTransferArm", 0.0},
            {"LiquidHeater", 0.0},
            {"CO2Scrubber", 0.0},
            {"FarmStation", 0.0},

            // Uninteresting frequent buildings.
            {"PressureDoor", -0.5},
            {"Door", -0.5},
            {"ManualPressureDoor", -0.5},
            {"AirFilter", -0.5},

            // Buildings that don't preserve important settings.
            {"FlowerVaseHanging", -0.5},
            {"Canvas", -0.5},
            {"CanvasWide", -0.5},
            {"CanvasTall", -0.5},
            {"MarbleSculpture", -0.5},
            {"RationBox", -0.5},
            {"ItemPedestal", -0.5},

            // Buildings out-of-place when not connected to conduits/etc.
            {"SolidVent", -0.5},
            {"LiquidVent", -0.5},
            {"LiquidValve", -0.5},
            {"GasConduitRadiant", -0.5},
            {"HighWattageWire", -0.5},
            {"WireRefinedHighWattage", -0.5},
            {"LiquidConduitTemperatureSensor", -0.5},
            {"LiquidConduitElementSensor", -0.5},
            {"StorageLocker", -0.5},
            {"GasConduitTemperatureSensor", -0.5},
            {"GasLogicValve", -0.5},
            {"SolidLogicValve", -0.5},
            {"LiquidFilter", -0.5},
            {"LogicPowerRelay", -0.5},
            {"GasValve", -0.5},
            {"GasVent", -0.5},
            {"SolidConduitOutbox", -0.5},
            {"LiquidLogicValve", -0.5},
            {"Switch", -0.5},
            {"SolidConduitInbox", -0.5},
            {"LogicElementSensorGas", -0.5},
            {"LogicPressureSensorGas", -0.5},
            {"LogicCritterCountSensor", -0.5},
            {"LogicTemperatureSensor", -0.5},
            {"LogicGateAND", -0.5},
            {"LogicGateBUFFER", -0.5},
            {"LogicGateFILTER", -0.5},
            {"GasVentHighPressure", -0.5},
            {"LogicGateNOT", -0.5},
            {"LogicPressureSensorLiquid", -0.5},
            {"GasFilter", -0.5},
            {"LogicTimeOfDaySensor", -0.5},
            {"LogicSwitch", -0.5},
            {"LiquidConduitRadiant", -0.5},
            {"TravelTubeEntrance", -0.5},

            // Extremely frequent, uninteresting buildings.
            {"Ladder", -1.0},
            {"FirePole", -1.0},
            {"GasConduit", -1.0},
            {"InsulatedGasConduit", -1.0},
            {"GasConduitBridge", -1.0},
            {"LiquidConduit", -1.0},
            {"InsulatedLiquidConduit", -1.0},
            {"LiquidConduitBridge", -1.0},
            {"LogicWire", -1.0},
            {"LogicWireBridge", -1.0},
            {"SolidConduit", -1.0},
            {"SolidConduitBridge", -1.0},
            {"Wire", -1.0},
            {"WireBridge", -1.0},
            {"WireRefined", -1.0},
            {"WireRefinedBridge", -1.0},
            {"TravelTube", -1.0},

            // Never place rockets.
            {"SteamEngine", double.NegativeInfinity},
            {"OxidizerTank", double.NegativeInfinity},
            {"CargoBay", double.NegativeInfinity},
            {"LiquidFuelTank", double.NegativeInfinity},
            {"KeroseneEngine", double.NegativeInfinity},
            {"ResearchModule", double.NegativeInfinity},
            {"CommandModule", double.NegativeInfinity},

            // Never place POI buildings.
            {"TilePOI", double.NegativeInfinity},
            {"POIFacilityDoor", double.NegativeInfinity},
            {"FacilityBackWallWindow", double.NegativeInfinity},
            {"POIBunkerExteriorDoor", double.NegativeInfinity},
            {"POIDoorInternal", double.NegativeInfinity},

            // Other buildings to never place.
            {"OilWellCap", double.NegativeInfinity},
            {"Headquarters", double.NegativeInfinity},
            {"SuitMarker", double.NegativeInfinity},
            {"JetSuitMarker", double.NegativeInfinity},
            {"MonumentBottom", double.NegativeInfinity},
            {"MonumentMiddle", double.NegativeInfinity},
            {"MonumentTop", double.NegativeInfinity},

        };
        public static double ScoreBuildingId(string id)
        {
            if (!building_id_scores.ContainsKey(id))
            {
                Debug.Log("Ruins: Missing score for building " + id);
                building_id_scores[id] = double.NegativeInfinity;
            }
            return building_id_scores[id];
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
