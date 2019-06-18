using Harmony;
using System.Collections.Generic;
using System;

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

    [HarmonyPatch(typeof(DetailsScreen), "SetTitle", new Type[] { typeof(int) })]
    internal class Alarm_DetailsScreen_SetTitle
    {
        private static void Postfix(DetailsScreen __instance, EditableTitleBar ___TabTitle)
        {
            if (___TabTitle != null)
            {
                var alarm = __instance.target.GetComponent<Alarm>();
                if (alarm != null)
                {
                    ___TabTitle.SetUserEditable(editable: true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnNameChanged")]
    internal class Alarm_DetailsScreen_OnNameChanged
    {
        private static void Postfix(DetailsScreen __instance, string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                var alarm = __instance.target.GetComponent<Alarm>();
                if (alarm != null)
                {
                    alarm.SetName(newName);
                }
            }
        }
    }
}
