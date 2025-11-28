using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NamPhuThuy.AddressablesAdapter
{
    public class AddressablesSerializer : EditorWindow
    {
        private AddressableAssetGroup targetGroup;
        private string prefixName = "Level ";
        
        private int idOffset = 0;
        
        // Assets list
        private List<GameObject> assetList = new List<GameObject>();
        private Vector2 scrollPos;

        [MenuItem("Tools/NamPhuThuy - AddressablesAdapter/Addressables Serializer")]
        private static void ShowWindow()
        {
            GetWindow<AddressablesSerializer>("Addressables Serializer");
        }

        #region Callbacks

        private void OnEnable()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                targetGroup = settings.FindGroup("Normal Level");
            }
        }

        private void OnGUI()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Addressables not initialized. Use `Window\\Asset Management\\Addressables\\Groups` first.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Target Addressables Group", EditorStyles.boldLabel);
            targetGroup = (AddressableAssetGroup)EditorGUILayout.ObjectField(
                "Group",
                targetGroup,
                typeof(AddressableAssetGroup),
                false
            );

            if (targetGroup == null)
            {
                EditorGUILayout.HelpBox("Select or create a group named `Normal Level`.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Address Config", EditorStyles.boldLabel);
            prefixName = EditorGUILayout.TextField("Prefix Name", prefixName);
            
            GUIContent idOffsetLabel = new GUIContent(
                "ID Offset",
                $"The asset-id will start from {idOffset + 1}"
            );
            idOffset = EditorGUILayout.IntField(idOffsetLabel, idOffset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level prefabs", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Selected Assets"))
            {
                var selectedObjects = Selection.objects;
                foreach (var obj in selectedObjects)
                {
                    var go = obj as GameObject;
                    if (go == null)
                        continue;

                    // Optional: ensure it's actually a prefab asset, not a scene object
                    var path = AssetDatabase.GetAssetPath(go);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (!assetList.Contains(go))
                    {
                        assetList.Add(go);
                    }
                }
            }

            if (GUILayout.Button("Clear List"))
            {
                assetList.Clear();
            }

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
            for (int i = 0; i < assetList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                assetList[i] = (GameObject)EditorGUILayout.ObjectField(assetList[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    assetList.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            GUI.enabled = targetGroup != null && assetList.Count > 0;
            if (GUILayout.Button("Apply To Addressables"))
            {
                AddPrefabsToAddressables(settings);
            }

            GUI.enabled = true;
        }

        #endregion

        private void AddPrefabsToAddressables(AddressableAssetSettings settings)
        {
            if (targetGroup == null)
            {
                Debug.LogError("Normal Level group is not set.");
                return;
            }

            Undo.RecordObject(settings, "Add Level Prefabs To Addressables");

            for (int i = 0; i < assetList.Count; i++)
            {
                var prefab = assetList[i];
                if (prefab == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Prefab at index {i} has no valid asset path.");
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"Could not get GUID for prefab at path {assetPath}");
                    continue;
                }

                // Try to get existing entry
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
                }
                else if (entry.parentGroup != targetGroup)
                {
                    settings.MoveEntry(entry, targetGroup);
                }

                // Set address: "Level <order>"
                int levelId = idOffset + i + 1;
                string address = $"Level {levelId}";
                entry.address = address;

                Debug.Log($"Set addressable: {prefab.name} -> Group: {targetGroup.Name}, Address: {address}");
            }

            // Save settings
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

}