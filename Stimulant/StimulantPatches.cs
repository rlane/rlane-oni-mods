using Harmony;
using System.Collections.Generic;

namespace Stimulant
{
    [HarmonyPatch(typeof(MedicinalPill), "EffectDescriptors")]
    internal class Stimulant_MedicinalPill_EffectDescriptors
    {
        private static void Postfix(MedicinalPill __instance, ref List<Descriptor> __result)
        {
            if (__instance.info.id == "Stimulant")
            {
                __result.RemoveAt(0);
            }
        }
    }
}