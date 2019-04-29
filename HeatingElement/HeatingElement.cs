using Harmony;
using System;
using System.Collections.Generic;

namespace HeatingElement
{
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal class HeatingElement_GeneratedBuildings_LoadGeneratedBuildings
    {
        private static void Prefix()
        {
            Strings.Add("STRINGS.BUILDINGS.PREFABS.HEATINGELEMENT.NAME", "Heating Element");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.HEATINGELEMENT.DESC", "Heating Elements will quickly melt if used without caution.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.HEATINGELEMENT.EFFECT", "Produces a large amount of Heat.");

            ModUtil.AddBuildingToPlanScreen("Utilities", HeatingElementConfig.ID);
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class HeatingElement_Db_Initialize
    {
        private static void Prefix(Db __instance)
        {
            List<string> ls = new List<string>((string[])Database.Techs.TECH_GROUPING["TemperatureModulation"]);
            ls.Add(HeatingElementConfig.ID);
            Database.Techs.TECH_GROUPING["TemperatureModulation"] = (string[])ls.ToArray();
        }
    }
}
