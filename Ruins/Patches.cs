using Harmony;
using STRINGS;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static KButtonMenu;

namespace Ruins
{
    [HarmonyPatch(typeof(PauseScreen), "OnPrefabInit")]
    internal class Ruins_PauseMenu_OnPrefabInit
    {
        private static void Postfix(PauseScreen __instance, ref IList<ButtonInfo> ___buttons)
        {
            var upload_button = new ButtonInfo("Upload Ruins", Action.Invalid, () =>
            {
                ConfirmDialogScreen confirmDialogScreen = (ConfirmDialogScreen)KScreenManager.Instance.StartScreen(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject, Global.Instance.globalCanvas);
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    Net.Upload(Ruins.GetBuildings());
                    Thread.Sleep(1000);
                    confirmDialogScreen.Deactivate();
                }));
                thread.Start();
                var text = new StringBuilder();
                text.AppendLine("Uploading...");
                confirmDialogScreen.PopupConfirmDialog(text.ToString(), null, null, null, null, "Ruins");
            });

            var new_buttons = new List<ButtonInfo>();
            foreach (var button in ___buttons)
            {
                if (button.text == UI.FRONTEND.PAUSE_SCREEN.QUIT)
                {
                    new_buttons.Add(upload_button);
                }
                new_buttons.Add(button);
            }
            ___buttons = new_buttons;
        }
    }

    [HarmonyPatch(typeof(WorldGenSpawner), "PlaceTemplates")]
    internal class Ruins_WorldGenSpawner_PlaceTemplates
    {
        private static void Prefix()
        {
            Debug.Log("Downloading Ruins template");
            var template = Net.Download();
            Debug.Log("Downloaded template with " + template.buildings.Count + " buildings");
            template = Ruins.MakeRuins(template);
            Debug.Log("Generated ruined template with " + template.buildings.Count + " buildings");
            foreach (var prefab in template.buildings)
            {
                SaveGame.Instance.worldGen.SpawnData.buildings.Add(prefab);
            }
        }
    }
}