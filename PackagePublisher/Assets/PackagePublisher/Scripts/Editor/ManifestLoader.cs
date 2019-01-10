using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PackagePublisher
{
    public static class ManifestLoader
    {
        public static string[] HIDDEN_DEPENDENCIES = new string[]
        {
            "com.unity.package-manager-ui",
            "com.landfall.package-publisher"
        };

        public static bool IsHiddenDependency(string packageName)
        {
            foreach(var dep in HIDDEN_DEPENDENCIES)
            {
                if (packageName == dep)
                    return true;
            }
            return false;
        }

        private static string LOCAL_MANIFEST_PATH = "Packages/manifest.json";
        private static string GLOBAL_MANIFEST_PATH = Application.dataPath + "/../Packages/manifest.json";
        
        public static Manifest LoadManifest()
        {
            string json = File.ReadAllText(GLOBAL_MANIFEST_PATH);
            return JsonConvert.DeserializeObject<Manifest>(json);
        }
        
        public static void SaveManifest(Manifest manifest)
        {
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(GLOBAL_MANIFEST_PATH, json);
            AssetDatabase.Refresh();
        }
    }

    [System.Serializable]
    public class Manifest
    {
        [SerializeField]
        public ScopedRegistry[] scopedRegistries;
        [SerializeField]
        public Dictionary<string, string> dependencies;

        public Dictionary<string, string> GetDependencies(bool excludeDefaultPackages = true)
        {
            if (excludeDefaultPackages)
            {
                Dictionary<string, string> filteredDependencies = new Dictionary<string, string>();
                
                foreach(var kvp in dependencies)
                {
                    if (!ManifestLoader.IsHiddenDependency(kvp.Key))
                        filteredDependencies.Add(kvp.Key, kvp.Value);
                }
                return filteredDependencies;
            }
            else
            {
                return dependencies;
            }
        }
    }

    [System.Serializable]
    public class ScopedRegistry
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public string url;
        [SerializeField]
        public string[] scopes;
    }
}


