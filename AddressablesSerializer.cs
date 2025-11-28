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
        private AddressableAssetGroup _targetGroup;
        private string _prefixName = "Level ";
        private int _idOffset = 0;
        
        // Assets list
        private List<GameObject> _assetList = new List<GameObject>();
        private Vector2 _scrollPos;
        private string _defaultGroupName = "Normal Level";

       

        #region Callbacks

        [MenuItem("Tools/NamPhuThuy - AddressablesAdapter/Addressables Serializer")]
        private static void ShowWindow()
        {
            GetWindow<AddressablesSerializer>($"{nameof(AddressablesSerializer)}");
        }
        
        private void OnEnable()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                _targetGroup = settings.FindGroup(_defaultGroupName);
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
            _targetGroup = (AddressableAssetGroup)EditorGUILayout.ObjectField(
                "Group",
                _targetGroup,
                typeof(AddressableAssetGroup),
                false
            );

            if (_targetGroup == null)
            {
                EditorGUILayout.HelpBox($"Select or create a group named `{_defaultGroupName}`.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Address Config", EditorStyles.boldLabel);
            _prefixName = EditorGUILayout.TextField(nameof(_prefixName), _prefixName);
            
            GUIContent idOffsetLabel = new GUIContent(
                "ID Offset",
                $"The asset-id will start from {_idOffset + 1}"
            );
            _idOffset = EditorGUILayout.IntField(idOffsetLabel, _idOffset);

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

                    if (!_assetList.Contains(go))
                    {
                        _assetList.Add(go);
                    }
                }
            }

            if (GUILayout.Button("Clear List"))
            {
                _assetList.Clear();
            }

            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));
            for (int i = 0; i < _assetList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                _assetList[i] = (GameObject)EditorGUILayout.ObjectField(_assetList[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _assetList.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            GUI.enabled = _targetGroup != null && _assetList.Count > 0;
            if (GUILayout.Button("Apply To Addressables"))
            {
                AddPrefabsToAddressables(settings);
            }

            GUI.enabled = true;
        }

        #endregion

        #region Main Functions

        private void AddPrefabsToAddressables(AddressableAssetSettings settings)
        {
            if (_targetGroup == null)
            {
                Debug.LogError("Normal Level group is not set.");
                return;
            }

            Undo.RecordObject(settings, "Add Level Prefabs To Addressables");

            for (int i = 0; i < _assetList.Count; i++)
            {
                var asset = _assetList[i];
                if (asset == null) continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Asset at index {i} has no valid asset path.");
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"Could not get GUID for asset at path {assetPath}");
                    continue;
                }

                // Try to get existing entry
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, _targetGroup, readOnly: false, postEvent: false);
                }
                else if (entry.parentGroup != _targetGroup)
                {
                    settings.MoveEntry(entry, _targetGroup);
                }

                // Set address: "Level <order>"
                int assetId = _idOffset + i + 1;
                string address = $"Level {assetId}";
                entry.address = address;

                Debug.Log($"Set addressable: {asset.name} -> Group: {_targetGroup.Name}, Address: {address}");
            }

            // Save settings
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        #endregion
    }

}