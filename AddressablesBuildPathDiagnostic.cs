#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public class AddressablesBuildPathDiagnostic : EditorWindow
{
    [MenuItem("Tools/🔍 Diagnose Addressables Build Configuration")]
    static void ShowWindow()
    {
        GetWindow<AddressablesBuildPathDiagnostic>("Build Path Diagnostic");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Addressables Build Path Diagnostic", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Run Full Diagnostic", GUILayout.Height(40)))
        {
            RunFullDiagnostic();
        }
    }
    
    static void RunFullDiagnostic()
    {
        Debug.Log("=================================================");
        Debug.Log("ADDRESSABLES BUILD PATH DIAGNOSTIC");
        Debug.Log("=================================================\n");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("❌ No Addressables settings found!");
            return;
        }
        
        // Check each group's configuration
        Debug.Log("📋 GROUP CONFIGURATIONS:\n");
        
        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            
            var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundleSchema == null)
            {
                Debug.Log($"⚠️ Group: {group.name} - NOT bundled (skipped)\n");
                continue;
            }
            
            Debug.Log($"📦 GROUP: {group.name}");
            Debug.Log($"   Entries: {group.entries.Count}");
            
            // Get build path
            string buildPathRaw = bundleSchema.BuildPath.Id;
            string buildPathResolved = bundleSchema.BuildPath.GetValue(settings);
            
            Debug.Log($"   Build Path (Raw): {buildPathRaw}");
            Debug.Log($"   Build Path (Resolved): {buildPathResolved}");
            
            // Get load path
            string loadPathRaw = bundleSchema.LoadPath.Id;
            string loadPathResolved = bundleSchema.LoadPath.GetValue(settings);
            
            Debug.Log($"   Load Path (Raw): {loadPathRaw}");
            Debug.Log($"   Load Path (Resolved): {loadPathResolved}");
            
            // Check if bundles exist at this location
            if (Directory.Exists(buildPathResolved))
            {
                var bundles = Directory.GetFiles(buildPathResolved, "*.bundle", SearchOption.AllDirectories);
                Debug.Log($"   ✅ Found {bundles.Length} bundles at build path:");
                
                foreach (var bundle in bundles)
                {
                    FileInfo info = new FileInfo(bundle);
                    Debug.Log($"      - {Path.GetFileName(bundle)} ({info.Length / 1024f:F2} KB)");
                }
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Build path doesn't exist yet: {buildPathResolved}");
            }
            
            Debug.Log(""); // Blank line
        }
        
        // Check profile settings
        Debug.Log("\n🎯 ACTIVE PROFILE SETTINGS:\n");
        
        var _settings = AddressableAssetSettingsDefaultObject.Settings;
        var activeProfileId = _settings.activeProfileId;
        
        if (activeProfileId != null)
        {
            var profileEntryNames = settings.profileSettings.GetAllProfileNames();
            Debug.Log($"Active Profile: {profileEntryNames}");
            
            foreach (var entry in profileEntryNames)
            {
                // entry is a string profile variable name
                string value = settings.profileSettings.GetValueByName(activeProfileId, entry);
                Debug.Log($"   {entry} = {value}");
            }
        }
        
        // Check common build locations
        Debug.Log("\n\n📂 CHECKING COMMON BUNDLE LOCATIONS:\n");
        
        CheckLocation("Assets/StreamingAssets");
        CheckLocation("Assets/StreamingAssets/aa");
        CheckLocation("Assets/StreamingAssets/aa/Android");
        CheckLocation("ServerData");
        CheckLocation("ServerData/Android");
        CheckLocation(Application.dataPath + "/../Library/com.unity.addressables/aa");
        CheckLocation("Library/com.unity.addressables/aa/Android");
        
        // Check the Bee/Gradle location
        Debug.Log("\n\n🐝 CHECKING GRADLE BUILD INTERMEDIATES:\n");
        CheckLocation("Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/intermediates/assets/release/mergeReleaseAssets");
        CheckLocation("Library/Bee/Android/Prj/IL2CPP/Gradle/unzipReleaseAar");
        
        Debug.Log("\n=================================================");
        Debug.Log("DIAGNOSTIC COMPLETE");
        Debug.Log("=================================================");
    }
    
    static void CheckLocation(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.Log($"❌ {path} - Does not exist");
            return;
        }
        
        var bundles = Directory.GetFiles(path, "*.bundle", SearchOption.AllDirectories);
        
        if (bundles.Length == 0)
        {
            Debug.Log($"📁 {path} - Exists but no bundles");
            return;
        }
        
        long totalSize = 0;
        Debug.Log($"✅ {path} - Found {bundles.Length} bundles:");
        
        foreach (var bundle in bundles)
        {
            FileInfo info = new FileInfo(bundle);
            totalSize += info.Length;
            string relativePath = bundle.Replace(path, "").TrimStart('\\', '/');
            Debug.Log($"   - {relativePath} ({info.Length / 1024f:F2} KB)");
        }
        
        Debug.Log($"   Total: {totalSize / (1024f * 1024f):FU} MB");
    }
}
#endif