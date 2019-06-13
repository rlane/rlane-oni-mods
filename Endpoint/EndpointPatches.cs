using Harmony;
using Database;

// TODO: Add option to leave pilot or passengers on Endpoint.
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
}
