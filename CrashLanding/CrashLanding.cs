using Harmony;

namespace CrashLanding
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class Strings
    {
        public static LocString NAME = "Crash Landing";

        public static LocString DESCRIPTION = "Your ship has crashed on an inhospitable asteroid. You have enough food, water, and energy to survive for a short time.\n\n" +
            "<smallcaps>Oxygen production is still online and the cargo hold contains a small amount of dirt and seeds. Your situation is precarious but careful planning and a lot of luck might allow you to escape.</smallcaps>\n\n";

        private static void Prefix(Db __instance)
        {
            ModUtil.RegisterForTranslation(typeof(Strings));
        }
    }
}
