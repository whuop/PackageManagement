﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using PackagePublisher.Json;

namespace PackagePublisher
{
    public enum VersionControlType : int
    {
        github = 0,
        gitlab = 1,
        bitbucket = 2
    }

    [System.Serializable]
    public class PackageData : ScriptableObject
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public PackageAuthor author;
        [SerializeField]
        public PackageAuthor[] contributors;
        [SerializeField]
        public string displayName;
        [SerializeField]
        public string version;
        [SerializeField]
        public string unity;
        [SerializeField]
        public string description;
        [SerializeField]
        public string category;
        [SerializeField]
        public string[] keywords;
        [SerializeField]
        public PackageRepository repository;
        [SerializeField]
        public PackageDependency[] dependencies;

        public PackageJson ToPackageJson()
        {
            PackageJson json = new PackageJson();
            json.name = this.name;
            json.author = this.author;
            json.contributors = this.contributors;
            json.displayName = this.displayName;
            json.version = this.version;
            json.unity = this.unity;
            json.description = this.description;
            json.category = this.category;
            json.keywords = this.keywords;
             
            json.repository = new Json.PackageRepository()
            {
                type = ((VersionControlType)this.repository.type).ToString(),
                url = this.repository.url
            };

            json.dependencies = new Dictionary<string, string>();
            for(int i = 0; i < this.dependencies.Length; i++)
            {
                json.dependencies.Add(this.dependencies[i].packageName, this.dependencies[i].packageVersion);
            }

            return json;
        }

        public void FromPackageJson(PackageJson json)
        {
            this.name = json.name;
            this.author = json.author;
            this.contributors = json.contributors;
            this.displayName = json.displayName;
            this.version = json.version;
            this.unity = json.unity;
            this.description = json.description;
            this.category = json.category;
            this.keywords = json.keywords;

            this.repository.url = json.repository.url;
            Enum.TryParse<VersionControlType>(json.repository.type, out this.repository.type);

            if (json.dependencies == null)
                json.dependencies = new Dictionary<string, string>();

            int numDependencies = json.dependencies.Count;
            this.dependencies = new PackageDependency[numDependencies];
            int i = 0;
            foreach(var dep in json.dependencies)
            {
                this.dependencies[i] = new PackageDependency()
                {
                    packageName = dep.Key,
                    packageVersion = dep.Value
                };
                i++;
            }
        }
    }

    [System.Serializable]
    public struct PackageDependency
    {
        [SerializeField]
        public string packageName;
        [SerializeField]
        public string packageVersion;
    }

    [System.Serializable]
    public struct PackageRepository
    {
        [SerializeField]
        public VersionControlType type;
        [SerializeField]
        public string url;
    }

    [System.Serializable]
    public struct PackageAuthor
    {
        public string name;
        public string email;
        public string url;
    }

    public struct PackageRegistry
    {
        public string Name;
        public string Url;
    }
    
    public class PackagePublisherEditor : EditorWindow
    {
        private SerializedObject m_packageJsonData;

        private List<PackageRegistry> m_registries;
        private int m_chosenRegistry = 0;
        private string[] m_registryList;

        private string[] m_versionControlList;
        private int m_chosenVersionControl;

        private string m_localJsonPath;
        private string m_globalJsonPath;

        [MenuItem("Landfall/Package Publisher")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(PackagePublisherEditor));
            window.titleContent = new GUIContent("Landfall Package Publisher");
            window.Show();
        }

        [MenuItem("Landfall/Clear Manifest Dependencies")]
        public static void ClearManifestDependencyList()
        {
            var manifest = ManifestLoader.LoadManifest();
            

            //  Don't remove package manager ui.
            Dictionary<string, string> newDeps = new Dictionary<string, string>();

            for (int i = 0; i < ManifestLoader.HIDDEN_DEPENDENCIES.Length; i++)
            {
                var key = ManifestLoader.HIDDEN_DEPENDENCIES[i];
                if (manifest.dependencies.ContainsKey(key))
                {
                    newDeps.Add(key, manifest.dependencies[key]);
                }
            }

            manifest.dependencies = newDeps;
            ManifestLoader.SaveManifest(manifest);
            AssetDatabase.Refresh();
        }
        
        private void OnEnable()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            InitializePaths();
            InitializeVersionControlTypes();
            InitializeRegistries();
            LoadPackage();
            UpdateManifest();
        }

        private void InitializePaths()
        {
            m_localJsonPath = "Assets/package.json";
            m_globalJsonPath = Application.dataPath + "/package.json";
        }

        private void InitializeVersionControlTypes()
        {
            var values = Enum.GetValues(typeof(VersionControlType)).Cast<VersionControlType>();
            int numElements = values.Count();
            m_versionControlList = new string[numElements];
            int i = 0;
            foreach(var val in values)
            {
                m_versionControlList[i] = val.ToString();
                i++;
            }
        }

        private void InitializeRegistries()
        {
            var landfallRegistry = new PackageRegistry() { Name = "Landfall Registry", Url = @"http://192.168.1.215:4873" };
            var localRegistry = new PackageRegistry() { Name = "Local Registry", Url = @"http://localhost:4873" };

            m_registries = new List<PackageRegistry>();
            m_registries.Add(landfallRegistry);
            m_registries.Add(localRegistry);

            m_registryList = new string[m_registries.Count];
            for(int i = 0; i < m_registries.Count; i++)
            {
                m_registryList[i] = m_registries[i].Name;
            }
        }

        private void UpdateManifest()
        {
            var manifest = ManifestLoader.LoadManifest();

            if (m_chosenRegistry == 0)
            {
                string url = @"http://192.168.1.215:4873";
                manifest.scopedRegistries[0].url = url;
            }
            else if (m_chosenRegistry == 1)
            {
                manifest.scopedRegistries[0].url = @"http://localhost:4873";
            }

            var dependencies = manifest.GetDependencies(true);

            PackageData data = (PackageData)m_packageJsonData.targetObject;

            int numDependencies = dependencies.Count;
            data.dependencies = new PackageDependency[numDependencies];
            int i = 0;
            foreach (var dep in dependencies)
            {
                UnityEngine.Debug.Log("Updating with dependency: " + dep.Key);
                data.dependencies[i] = new PackageDependency()
                {
                    packageName = dep.Key,
                    packageVersion = dep.Value
                };
                i++;
            }

            m_packageJsonData.ApplyModifiedProperties();

            ManifestLoader.SaveManifest(manifest);
            SavePackageJson(data.ToPackageJson());
            LoadPackage();
        }

        private void OnGUI()
        {
            if (m_packageJsonData == null)
            {
                EditorGUILayout.LabelField("Missing package.json file. Have you created Assets/package.json?");
                return;
            }

            //  Draw registry picker
            int newRegistry = EditorGUILayout.Popup("Registry", m_chosenRegistry, m_registryList);
            if (newRegistry != m_chosenRegistry)
            {
                m_chosenRegistry = newRegistry;

                //  Update manifest to the correct registry
                UpdateManifest();
            }



            if (GUILayout.Button("Publish Package"))
            {
                PublishPackage(string.Format(@"npm publish --registry {0}", m_registries[m_chosenRegistry].Url));
            }

            EditorGUILayout.LabelField("Package.json Settings");

            DrawPackageJson();

            
        }

        private Vector2 _scrollPos = Vector2.zero;
        private void DrawPackageJson()
        {
            // display serializedProperty with selected mode
            Type type = typeof(SerializedObject);
            PropertyInfo infor = type.GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            if (infor != null)
            {
                infor.SetValue(m_packageJsonData, InspectorMode.Normal, null);
            }

            EditorGUI.BeginChangeCheck();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            SerializedProperty iterator = m_packageJsonData.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;
                
                EditorGUILayout.PropertyField(iterator, true);
            }
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                m_packageJsonData.ApplyModifiedProperties();
                SavePackageJson(((PackageData)m_packageJsonData.targetObject).ToPackageJson());
            }
        }

        private void LoadPackage()
        {
            TextAsset packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>(m_localJsonPath);
            var c = ScriptableObject.CreateInstance<PackageData>();

            if (packageJson == null)
            {
                //  Initialize c with default data!
                c.name = "com.my-company.product-name";
                c.displayName = "Product Name";
                c.unity = "2018.3";
                c.version = "0.0.1";
                c.description = "Product description here!";
                c.category = "Unity";
                c.keywords = new string[] { "keyword_one", "keyword_two" };
                c.dependencies = new PackageDependency[0];
                c.author = new PackageAuthor { name = "John Doe", email = "john.doe@gmail.com", url = "" };
                c.contributors = new PackageAuthor[0];
                c.repository = new PackageRepository() { type = VersionControlType.github, url = "" };

                SavePackageJson(c.ToPackageJson());

                packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>(m_localJsonPath);
            }

            UnityEngine.Debug.Log("Loaded Package: " + packageJson.text);

            PackageJson jsonPopulate = new PackageJson();
            JsonConvert.PopulateObject(packageJson.text, jsonPopulate);
            c.FromPackageJson(jsonPopulate);

            //UnityEngine.Debug.Log("Num Dep: " + c.dependencies.Count);

            m_packageJsonData = new SerializedObject(c);

        }

        private void SavePackageJson(PackageJson data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(m_globalJsonPath, json);
            AssetDatabase.Refresh();
        }

        private async void PublishPackage(string command)
        {
            await Landfall.Processes.ProcessRunner.RunAsync("cmd.exe", "/c " + command, Application.dataPath, 
                OnPublishFinish, OnPublishOutput, OnPublishError);
        }

        private void OnPublishFinish(object sender, EventArgs args)
        {
            Process p = (Process)sender;

            if (p.ExitCode == 0)
            {
                UnityEngine.Debug.Log("Successfully published package!");
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to publish package! See output above.");
            }
        }

        private void OnPublishOutput(object sender, DataReceivedEventArgs args)
        {
            if (String.IsNullOrEmpty(args.Data) || args.Data == "Null")
                return;
            UnityEngine.Debug.Log(args.Data);
        }

        private void OnPublishError(object sender, DataReceivedEventArgs args)
        {
            if (String.IsNullOrEmpty(args.Data) || args.Data == "Null")
                return;
            UnityEngine.Debug.Log(args.Data);
        }
    }

}