using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditorInternal;
#endif

#if UNITY_EDITOR
namespace NamPhuThuy.AddressablesAdapter
{
    public class AddressablesTool : EditorWindow
    {
        private AddressableAssetGroup _targetGroup;
        private AddressableAssetSettings _addressableAssetSettings;
        private string _prefixName = "Level ";
        private int _idOffset = 0;
        
        // Assets list
        private List<Object> _assetList = new List<Object>();
        private ReorderableList _reorderableList;
        private Vector2 _scrollPos;
        private string _defaultGroupName = "Normal Level";
        
        // New: input field for groups to create
        private string _groupNamesInput = "Normal Level, Hard Level, UI, Audio";

        #region Callbacks

        [MenuItem("NamPhuThuy/AddressablesAdapter/Addressables Tool")]
        private static void ShowWindow()
        {
            GetWindow<AddressablesTool>($"{nameof(AddressablesTool)}");
        }
        
        private void OnEnable()
        {
            _addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (_addressableAssetSettings != null)
            {
                _targetGroup = _addressableAssetSettings.FindGroup(_defaultGroupName);
            }
            
            InitReorderableList();
        }

        private void OnGUI()
        {
            if (_addressableAssetSettings == null)
            {
                EditorGUILayout.HelpBox(
                    "Addressables not initialized. Use `Window\\Asset Management\\Addressables\\Groups` first.",
                    MessageType.Error);
                return;
            }

            GUISerializer();
            EditorGUILayout.Space(10);
            
            GUIGroupCreator();
            EditorGUILayout.Space(10);

            GUI.enabled = true;
        }

        private void GUISerializer()
        {
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
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Selected Assets"))
            {
                var selectedObjects = Selection.objects;
                foreach (var obj in selectedObjects)
                {
                    // Optional: keep only asset types you care about
                    // if (obj is not GameObject && obj is not Sprite && obj is not Texture2D) continue;
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogWarning(message: $"Cant find {obj.name}");
                        continue; 
                    }

                    if (!_assetList.Contains(obj))
                    {
                        _assetList.Add(obj);
                    }
                }
                
                // Refresh list after changes
                _reorderableList.list = _assetList;
            }

            if (GUILayout.Button("Clear List"))
            {
                _assetList.Clear();
                _reorderableList.list = _assetList;
            }

            EditorGUILayout.Space();

            // Optional: put the list inside a box and scroll view
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                float listHeight = (_assetList.Count * (EditorGUIUtility.singleLineHeight + 6f)) +
                                   EditorGUIUtility.singleLineHeight + 10f; // header
                var rect = GUILayoutUtility.GetRect(0, listHeight, GUILayout.ExpandWidth(true));
                _reorderableList.DoList(rect);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // GUI.enabled = _targetGroup != null && _assetList.Count > 0;
            if (GUILayout.Button("Apply To Addressables"))
            {
                ListAssetsIntoGroups(_addressableAssetSettings);
            }
        }

        private void GUIGroupCreator()
        {
            EditorGUILayout.LabelField("Create Addressables Groups", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter group names separated by commas.\nExample: Normal Level, Hard Level, UI, Audio",
                MessageType.Info);
            _groupNamesInput = EditorGUILayout.TextField("Groups To Create", _groupNamesInput);

            if (GUILayout.Button("Create Groups"))
            {
                var groupNames = _groupNamesInput
                    .Split(',')
                    .Select(groupName => groupName.Trim())
                    .Where(groupName => !string.IsNullOrEmpty(groupName))
                    .ToList();

                CreateGroups(_addressableAssetSettings, groupNames);
            }
        }

        #endregion

        #region Initialization

        private void InitReorderableList()
        {
            _reorderableList = new ReorderableList(
                _assetList,
                typeof(Object),
                draggable: true,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false
            );

            _reorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Assets (prefabs, sprites, textures, etc. - drag to reorder)");
            };

            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0 || index >= _assetList.Count) return;

                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                _assetList[index] = EditorGUI.ObjectField(
                    rect,
                    _assetList[index],
                    typeof(Object), // <- allow any asset type
                    false
                );
            };
        }

        #endregion

        #region Create Groups Function

        // New helper: create several Addressables groups at once
        private void CreateGroups(AddressableAssetSettings settings, List<string> groupNames)
        {
            if (settings == null || groupNames == null || groupNames.Count == 0)
                return;

            Undo.RecordObject(settings, "Create Addressables Groups");

            foreach (var groupName in groupNames)
            {
                if (string.IsNullOrWhiteSpace(groupName))
                    continue;

                var existing = settings.FindGroup(groupName);
                if (existing != null)
                    continue;

                var group = settings.CreateGroup(
                    groupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: false,
                    settings.DefaultGroup.Schemas);

                Debug.Log($"Created Addressables group: {groupName}");
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        #endregion
        
        #region List Assets into Groups

        private void ListAssetsIntoGroups(AddressableAssetSettings settings)
        {
            // Adjust the addresses first
            Undo.RecordObject(settings, "Modified Assets Addresses");
            ModifyAssetAddress(settings);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            // Add assets to groups
            if (_targetGroup == null)
            {
                Debug.LogError("Normal Level group is not set.");
                return;
            }
            
            Undo.RecordObject(settings, "Add Assets To Addressables");
            AddAssets(settings);

            // Save settings
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private void ModifyAssetAddress(AddressableAssetSettings settings)
        {
            
            for (int i = 0; i < _assetList.Count; i++)
            {
                var asset = _assetList[i];
                if (asset == null)
                {
                    continue;
                }

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

                AddressableAssetEntry entry = settings.FindAssetEntry(guid);

                // Set address: "Level <order>"
                int assetId = _idOffset + i + 1;
                string address = $"{_prefixName} {assetId}";
                entry.address = address;
            }
        }

        private void AddAssets(AddressableAssetSettings settings)
        {
            for (int i = 0; i < _assetList.Count; i++)
            {
                var asset = _assetList[i];
                if (asset == null)
                {
                    continue;
                }

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
            }
        }
        
        #endregion
    }

}
#endif