using Harmony;
using System.Collections.Generic;

namespace rlane
{
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal class MeteorDefenseLaser_GeneratedBuildings_LoadGeneratedBuildings
    {
        private static void Prefix()
        {
            Strings.Add("STRINGS.BUILDINGS.PREFABS.METEORDEFENSELASER.NAME", "Meteor Defense Laser");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.METEORDEFENSELASER.DESC", "Requires significant power.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.METEORDEFENSELASER.EFFECT", "Blasts incoming meteors into smithereens.");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.LASERSTOREDCHARGE.NAME", "Power available: {0} of {1}.");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.LASERSTOREDCHARGE.TOOLTIP", "This building can store enough power to fire the laser for 3 seconds.");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.LASERKILLS.NAME", "Kill count: {0}");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.LASERKILLS.TOOLTIP", "Number of meteors destroyed.");

            ModUtil.AddBuildingToPlanScreen("Automation", MeteorDefenseLaserConfig.ID);
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class MeteorDefenseLaser_Db_Initialize
    {
        private static void Prefix(Db __instance)
        {
            List<string> ls = new List<string>((string[])Database.Techs.TECH_GROUPING["SkyDetectors"]);
            ls.Add(MeteorDefenseLaserConfig.ID);
            Database.Techs.TECH_GROUPING["SkyDetectors"] = (string[])ls.ToArray();
        }
    }
    
    [HarmonyPatch(typeof(Comet), "OnSpawn")]
    internal class MeteorDefenseLaser_Comet_OnSpawn
    {
        private static void Prefix(Comet __instance)
        {
            MeteorDefenseLaser.comet_tracker.Add(__instance);
        }
    }

    // Also seems to be called for things that aren't comets.
    [HarmonyPatch(typeof(Comet), "OnCleanUp")]
    internal class MeteorDefenseLaser_Comet_OnCleanUp
    {
        private static void Prefix(Comet __instance)
        {
            MeteorDefenseLaser.comet_tracker.Remove(__instance);
        }
    }
    
}
