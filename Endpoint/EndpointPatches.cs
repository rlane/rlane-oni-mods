using Harmony;
using Database;
using UnityEngine;
using System.Linq;
using System.Text;

namespace Endpoint
{
    class Endpointmain
    {
        public static void OnLoad()
        {
            Debug.Log("Endpoint state path: " + EndpointState.Filename());
            if (!System.IO.File.Exists(EndpointState.Filename()))
            {
                Debug.Log("endpoint_state.yaml file not found. Creating it.");
                new EndpointState().Save();
            }
        }
    }

    [HarmonyPatch(typeof(SpacecraftManager), "OnSpawn")]
    internal class Endpoint_SpacecraftManager_OnSpawn
    {
        const int DISTANCE = 5;

        private static void Postfix(SpacecraftManager __instance)
        {
            var terra_planet_type = Db.Get().SpaceDestinationTypes.TerraPlanet;
            Db.Get().SpaceDestinationTypes.Add(new Database.SpaceDestinationType("Endpoint", Db.Get().Root, "Endpoint", "A garden planet, inhabited by other survivors.", terra_planet_type.iconSize, terra_planet_type.spriteName, terra_planet_type.elementTable, terra_planet_type.recoverableEntities, terra_planet_type.artifactDropTable));

            if (!__instance.destinations.Exists((x) => x.type == "Endpoint"))
            {
                __instance.destinations.Add(new SpaceDestination(__instance.destinations.Count, "Endpoint", DISTANCE));
            }
        }
    }

    [HarmonyPatch(typeof(CommandModule), "OnSpawn")]
    internal class Endpoint_CommandModule_OnSpawn
    {
        private static void Postfix(CommandModule __instance)
        {
            __instance.FindOrAdd<EndpointTransport>();
        }
    }

    [HarmonyPatch(typeof(TouristModule), "OnSpawn")]
    internal class Endpoint_TouristModule_OnSpawn
    {
        private static void Postfix(TouristModule __instance)
        {
            __instance.FindOrAdd<EndpointTransport>();
        }
    }

    [HarmonyPatch(typeof(Spacecraft), "ProgressMission")]
    internal class Endpoint_Spacecraft_ProgressMission
    {
        private static void Postfix(Spacecraft __instance, float deltaTime)
        {
            bool has_reached_destination = (__instance.state == Spacecraft.MissionState.Underway || __instance.state == Spacecraft.MissionState.WaitingToLand || __instance.state == Spacecraft.MissionState.Landing) && __instance.GetTimeLeft() <= __instance.GetDuration() / 2;
            var destination = SpacecraftManager.instance.GetSpacecraftDestination(__instance.id);
            foreach (GameObject item in AttachableBuilding.GetAttachedNetwork(__instance.launchConditions.GetComponent<AttachableBuilding>()))
            {
                item.GetComponent<EndpointTransport>()?.SetReachedDestination(has_reached_destination, destination);
            }
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateTraits")]
    internal class Endpoint_MinionStartingStats_GenerateTraits
    {
        private static void Postfix(MinionStartingStats __instance)
        {
            var name = __instance.Name;
            var state = EndpointState.Load();
            if (state.times_rescued.ContainsKey(name))
            {
                int count = state.times_rescued[name];
                var trait = new Klei.AI.Trait("Rescued", "Rescued", "A previous iteration of this duplicant visited the great printing pod in the sky (x" + count + ").", 0, true, null, true, true);
                foreach (var attribute in TUNING.DUPLICANTSTATS.DISTRIBUTED_ATTRIBUTES)
                {
                    trait.Add(new Klei.AI.AttributeModifier(attribute, state.times_rescued[name], "Rescued x" + count));
                }
                __instance.Traits.Add(trait);
            }
        }
    }

    [HarmonyPatch(typeof(MainMenu), "OnPrefabInit")]
    internal class Endpoint_MainMenu_OnPrefabInit
    {
        private static void Postfix(MainMenu __instance, KButton ___buttonPrefab, GameObject ___buttonParent)
        {
            KButton kButton = Util.KInstantiateUI<KButton>(___buttonPrefab.gameObject, ___buttonParent, force_active: true);
            kButton.onClick += () => {
                ConfirmDialogScreen confirmDialogScreen = (ConfirmDialogScreen)KScreenManager.Instance.StartScreen(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject, Global.Instance.globalCanvas);
                var text = new StringBuilder();
                text.AppendLine("Duplicants rescued:");
                var state = EndpointState.Load();
                foreach (var item in from x in state.times_rescued orderby -x.Value, x.Key select x)
                {
                    if (item.Value == 1)
                    {
                        text.AppendLine(item.Key);
                    } else
                    {
                        text.AppendLine(item.Key + " x" + item.Value);
                    }
                }
                confirmDialogScreen.PopupConfirmDialog(text.ToString(), null, null, null, null, "Endpoint Population");
            };
            LocText loctext = kButton.GetComponentInChildren<LocText>();
            loctext.text = "ENDPOINT";
            loctext.fontSize = 14.0f;
        }
    }
}
