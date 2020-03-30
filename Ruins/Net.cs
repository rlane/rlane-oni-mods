using YamlDotNet.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Net;
using System;
using System.Collections.Generic;
using System.Text;
using Klei;

namespace Ruins
{
    public static class Net
    {
        static bool verbose = false;
        static string server = "https://oni-ruins.appspot.com";  // TODO make configurable

        public static void Upload(TemplateContainer template)
        {
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
                WebRequest request = WebRequest.Create(server + "/generate_upload_url");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    response_data = new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
            }

            if (verbose)
            {
                Debug.Log("HTTP response content: " + response_data);
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

            Debug.Log("Uploading to blob " + url);
            if (verbose)
            {
                foreach (var entry in fields)
                {
                    Debug.Log("Field " + entry.Key + ": " + entry.Value);
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

        public static TemplateContainer Download()
        {
            string url;
            using (var response = WebRequest.Create(server + "/generate_download_url").GetResponse())
            {
                url = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }

            Debug.Log("Downloading blob " + url);
            using (var response = WebRequest.Create(url).GetResponse())
            {
                using (var gzip_stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                {
                    return new DeserializerBuilder().Build().Deserialize<TemplateContainer>(new StreamReader(gzip_stream));
                }
            }
        }
    }
}