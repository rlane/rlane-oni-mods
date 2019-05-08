using Harmony;
using System.Collections.Generic;

namespace rlane
{
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal class Alarm_GeneratedBuildings_LoadGeneratedBuildings
    {
        private static void Prefix()
        {
            Strings.Add("STRINGS.BUILDINGS.PREFABS.ALARM.NAME", "Alarm");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.ALARM.DESC", "");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.ALARM.EFFECT", "Notifies you when activated.");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.ALARM.NOTIFICATION_NAME", "Alarm");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.ALARM.NOTIFICATION_TOOLTIP", "Alarm activated.");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.ALARM.NAME", "Alarm!");
            Strings.Add("STRINGS.BUILDING.STATUSITEMS.ALARM.TOOLTIP", "Alarm activated.");
            Strings.Add("STRINGS.UI.UISIDESCREENS.ALARM.TITLE", "Brightness");
            Strings.Add("STRINGS.UI.UISIDESCREENS.ALARM.TOOLTIP", "Brightness");
            ModUtil.AddBuildingToPlanScreen("Automation", AlarmConfig.ID);
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class Alarm_Db_Initialize
    {
        private static void Prefix(Db __instance)
        {
            List<string> ls = new List<string>((string[])Database.Techs.TECH_GROUPING["LogicCircuits"]);
            ls.Add(AlarmConfig.ID);
            Database.Techs.TECH_GROUPING["LogicCircuits"] = (string[])ls.ToArray();
        }
    }
}
