using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Harmony;
using Klei;
using Klei.AI;
using Newtonsoft.Json;
using STRINGS;
using TemplateClasses;
using UnityEngine;
using YamlDotNet.Serialization;
using static KButtonMenu;

namespace Ruins
{
    public class Ruins
    {
        public static TemplateContainer MakeRuins(TemplateContainer input)
        {
            var rng = new System.Random();
            var template = new TemplateContainer();
            template.info = input.info;
            foreach (var building in input.buildings) {
                if (rng.NextDouble() < 0.5)
                {
                    template.buildings.Add(building);
                }
            }
            return template;
        }

        public static TemplateContainer GetBuildings()
        {
            var template = new TemplateContainer();
            HashSet<GameObject> _excludeEntities = new HashSet<GameObject>();
            for (int i = 0; i < Components.BuildingCompletes.Count; i++)
            {
                BuildingComplete buildingComplete = Components.BuildingCompletes[i];
                if (_excludeEntities.Contains(buildingComplete.gameObject))
                {
                    continue;
                }
                Grid.CellToXY(Grid.PosToCell(buildingComplete), out int x, out int y);
                {
                    Orientation rotation = Orientation.Neutral;
                    var rotatable = buildingComplete.gameObject.GetComponent<Rotatable>();
                    if (rotatable != null)
                    {
                        rotation = rotatable.GetOrientation();
                    }
                    var primary_element = buildingComplete.GetComponent<PrimaryElement>();
                    if (primary_element == null)
                    {
                        continue;
                    }
                    Prefab prefab = new Prefab(buildingComplete.PrefabID().Name, Prefab.Type.Building, x, y, primary_element.ElementID, primary_element.Temperature, 0f, null, 0, rotation);
                    template.buildings.Add(prefab);
                    _excludeEntities.Add(buildingComplete.gameObject);
                }
            }
            // TODO power/gas/liquid/logic connections
            return template;
        }

        public static void Upload(TemplateContainer template)
        {
            bool verbose = false;
            Debug.Log("Saving ruins");

            var stream = new MemoryStream();
            using (var gzip_stream = new GZipStream(stream, CompressionMode.Compress, true))
            {
                using (var writer = new StreamWriter(gzip_stream))
                {
                    new SerializerBuilder().Build().Serialize(writer, template);
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            var file_bytes = stream.ToArray();
            Debug.Log("Serialized ruins length: " + file_bytes.Length);

            string response_data;
            {
                WebRequest request = WebRequest.Create("https://oni-ruins-test.appspot.com/generate_upload_url");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (verbose)
                    {
                        Debug.Log("HTTP status: " + response.StatusCode);
                    }
                    response_data = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    if (verbose)
                    {
                        Debug.Log("HTTP response content: " + response_data);
                    }
                }
            }

            var fields = new Dictionary<String, String>();
            using (JsonReader jsonReader = new JsonTextReader(new StringReader(response_data)))
            {
                string key = "";
                while (jsonReader.Read())
                {
                    JsonToken tokenType = jsonReader.TokenType;
                    if (tokenType == JsonToken.PropertyName)
                    {
                        key = jsonReader.Value.ToString();
                    }
                    else if (tokenType == JsonToken.String)
                    {
                        fields[key] = jsonReader.Value.ToString();
                    }
                }
            }

            var url = fields["url"];
            fields.Remove("url");

            if (verbose)
            {
                Debug.Log("Found url: " + url);
                foreach (var entry in fields)
                {
                    Debug.Log("Found field " + entry.Key + ": " + entry.Value);
                }
            }

            {
                using (WebClient webClient = new WebClient())
                {
                    Encoding encoding = webClient.Encoding = Encoding.UTF8;
                    string boundary = "----" + System.DateTime.Now.Ticks.ToString("x");
                    string boundary_line = $"--{boundary}\r\n";
                    webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    var writer = new StringBuilder();
                    writer.Append(boundary_line);
                    foreach (var entry in fields)
                    {
                        writer.AppendFormat("Content-Disposition: form-data; name=\"{0}\"\r\n", entry.Key);
                        writer.Append("\r\n");
                        writer.Append(entry.Value);
                        writer.Append("\r\n");
                        writer.Append(boundary_line);
                    }
                    writer.Append("Content-Disposition: form-data; name=\"file\"\r\n");
                    writer.Append("Content-Type: \"text/yaml\"\r\n");
                    writer.Append("\r\n");

                    var prefix_bytes = Encoding.ASCII.GetBytes(writer.ToString());
                    var postfix_bytes = Encoding.ASCII.GetBytes($"\r\n--{boundary}--\r\n");
                    var final_stream = new MemoryStream();
                    final_stream.Write(prefix_bytes, 0, prefix_bytes.Length);
                    final_stream.Write(file_bytes, 0, file_bytes.Length);
                    final_stream.Write(postfix_bytes, 0, postfix_bytes.Length);

                    try
                    {
                        webClient.UploadData(url, "POST", final_stream.ToArray());
                    }
                    catch (WebException obj)
                    {
                        Debug.Log("Failed to upload ruins: " + obj.Status + " " + obj.Message + " " + new StreamReader(obj.Response.GetResponseStream()).ReadToEnd());
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PauseScreen), "OnPrefabInit")]
    internal class Ruins_PauseMenu_OnPrefabInit
    {
        private static void Postfix(PauseScreen __instance, ref IList<ButtonInfo> ___buttons)
        {
            var upload_button = new ButtonInfo("Upload Ruins", Action.Invalid, () => {
                ConfirmDialogScreen confirmDialogScreen = (ConfirmDialogScreen)KScreenManager.Instance.StartScreen(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject, Global.Instance.globalCanvas);
                Thread thread = new Thread(new ThreadStart(() => {
                    Ruins.Upload(Ruins.GetBuildings());
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

}
