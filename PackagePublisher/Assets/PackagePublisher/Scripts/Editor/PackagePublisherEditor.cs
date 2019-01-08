﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PackagePublisher
{
    
    

    [System.Serializable]
    public class PackageJsonData : ScriptableObject
    {
        [SerializeField]
        public string name;
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
        public StringStringDictionary dependencies;

    }

    public class PackagePublisherEditor : EditorWindow
    {
        private static string REGISTRY_URL = @"http://localhost:4873";
        private static string LOCAL_JSON_PATH = "Assets/package.json";
        private static string GLOBAL_JSON_PATH = Application.dataPath + "/package.json";

        private SerializedObject m_packageJsonData;

        [MenuItem("Landfall/Package Publisher")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(PackagePublisherEditor));
            window.titleContent = new GUIContent("Landfall Package Publisher");
            window.Show();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            LoadPackage();
        }

        private void OnGUI()
        {
            if (m_packageJsonData == null)
            {
                EditorGUILayout.LabelField("Missing package.json file. Have you created Assets/package.json?");
                return;
            }

            if (GUILayout.Button("Publish Package"))
            {
                PublishPackage(string.Format(@"npm publish --registry {0}", REGISTRY_URL));
            }

            EditorGUILayout.LabelField("Package.json Settings");

            var editor = Editor.CreateEditor(m_packageJsonData.targetObject);
            if (editor != null)
            {
                editor.OnInspectorGUI();
            }

            //DrawPackageJson();
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
                SavePackageJson((PackageJsonData)m_packageJsonData.targetObject);
            }
        }

        private void LoadPackage()
        {
            TextAsset packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>(LOCAL_JSON_PATH);
            var c = ScriptableObject.CreateInstance<PackageJsonData>();

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
                c.dependencies = new StringStringDictionary();
                c.dependencies.Add(new KeyValuePair<string, string>("com.my-company.my-product", "0.0.1"));

                SavePackageJson(c);

                packageJson = AssetDatabase.LoadAssetAtPath<TextAsset>(LOCAL_JSON_PATH);
            }

            UnityEngine.Debug.Log("Loaded Package: " + packageJson.text);

            JsonConvert.PopulateObject(packageJson.text, c);

            UnityEngine.Debug.Log("Num Dep: " + c.dependencies.Count);

            m_packageJsonData = new SerializedObject(c);

        }

        private void SavePackageJson(PackageJsonData data)
        {
            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(GLOBAL_JSON_PATH, json);
            AssetDatabase.Refresh();
        }

        private void PublishPackage(string command)
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.WorkingDirectory = Application.dataPath;
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;


            process = Process.Start(processInfo);

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();



            process.WaitForExit();

            exitCode = process.ExitCode;
            process.Close();

            if (exitCode == 0)
            {
                if (!string.IsNullOrEmpty(output))
                    UnityEngine.Debug.Log(output);
                UnityEngine.Debug.Log("Successfully published package!");
            }
            else
            {
                if (!string.IsNullOrEmpty(error))
                    UnityEngine.Debug.LogError(error);
                UnityEngine.Debug.LogError("Failed to publish package, read errors above!");
            }
        }
    }

}