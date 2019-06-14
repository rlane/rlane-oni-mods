using Harmony;
using Database;
using UnityEngine;

// TODO: Add trait to new duplicants if previously rescued.
// TODO: Add main menu option to see rescued duplicants.

namespace Endpoint
{
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
}
