using Harmony;

namespace ErrorStackTrace
{
    [HarmonyPatch(typeof(KCrashReporter), "ShowDialog")]
    internal class ErrorStackTrace_KCrashReporter_ShowDialog
    {
        private static void Prefix(string stack_trace)
        {
            Debug.Log("Stack trace:\n" + stack_trace);
        }
    }
}
