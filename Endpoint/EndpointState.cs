using System;
using System.IO;
using System.Collections.Generic;
using Klei;
using System.Linq;


namespace Endpoint
{
    class EndpointState : YamlIO<EndpointState>
    {
        public Dictionary<string, int> times_rescued { get; set; }  = new Dictionary<string, int>();

        public static string Filename()
        {
            return Path.Combine(Util.RootFolder(), "endpoint_state.yaml");
        }

        public static EndpointState Load()
        {
            EndpointState result;
            try
            {
                result = LoadFile(Filename());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to load endpoint_state.yml: " + ex.ToString());
                result = new EndpointState();
            }
            if (result.times_rescued == null)
            {
                Debug.Log("Missing times_rescued");
                result.times_rescued = new Dictionary<string, int>();
            }
            Debug.Log("Loaded Endpoint state: " + result.ToString());
            return result;
        }

        public void Save()
        {
            Debug.Log("Saving Endpoint state: " + ToString());
            try
            {
                Save(Filename());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to save endpoint_state.yml: " + ex.ToString());
            }
        }

        public override string ToString()
        {
            return "{" + string.Join(",", times_rescued.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
        }
    }
}
