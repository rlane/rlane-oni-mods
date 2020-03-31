using Harmony;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static KButtonMenu;

namespace Ruins
{
    class RuinsMain
    {
        public static void OnLoad(string path)
        {
            Debug.Log("Ruins mod path: " + path);
            var default_config_path = path + "/config.default.yaml";
            var user_config_path = path + "/config.yaml";
            Ruins.config = RuinsConfig.Load(default_config_path);
            try
            {
                Ruins.config = RuinsConfig.Load(user_config_path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to load Ruins config from " + user_config_path + ": " + ex.ToString());
                Debug.LogWarning("Falling back to default Ruins config");
            }
        }
    }

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
                    try
                    {
                        Net.Upload(Ruins.GetBuildings());
                        Thread.Sleep(1000);
                    } catch (Exception e)
                    {
                        Debug.LogError("Ruins: Failed to upload template: " + e);
                    }
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
            try
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
            } catch (Exception e)
            {
                Debug.LogError("Ruins: failed to download template: " + e);
            }
        }
    }
}