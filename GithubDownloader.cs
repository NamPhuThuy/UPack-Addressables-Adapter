using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace NamPhuThuy.AddressablesAdapter
{
    public class GithubDownloader : MonoBehaviour
    {
        [Header("GitHub Configuration")]
        [SerializeField] private string githubUsername = "your-username";
        [SerializeField] private string repositoryName = "your-repo";
        [SerializeField] private string branch = "main";
        [SerializeField] private string assetBundlePath = "AssetBundles"; // Path in repo
        
        [Header("Addressables Settings")]
        [SerializeField] private string remoteCatalogURL;
        
        [Header("Download Settings")]
        [SerializeField] private int maxRetryAttempts = 3;
        [SerializeField] private float retryDelay = 2f;
        
        // Events
        public event Action<string, float> OnDownloadProgress;
        public event Action<string> OnDownloadComplete;
        public event Action<string, string> OnDownloadFailed;
        public event Action<long> OnDownloadSizeCalculated;
        
        private Dictionary<string, AsyncOperationHandle> activeDownloads = new Dictionary<string, AsyncOperationHandle>();
        private bool isInitialized = false;

        #region Initialization

        private void Awake()
        {
            // Construct GitHub raw content URL
            if (string.IsNullOrEmpty(remoteCatalogURL))
            {
                remoteCatalogURL = $"https://raw.githubusercontent.com/{githubUsername}/{repositoryName}/{branch}/{assetBundlePath}";
            }
        }

        /// <summary>
        /// Initialize Addressables with remote catalog from GitHub
        /// </summary>
        public IEnumerator InitializeAddressables()
        {
            if (isInitialized)
            {
                Debug.Log("[GithubDownloader] Already initialized.");
                yield break;
            }

            Debug.Log($"[GithubDownloader] Initializing with catalog: {remoteCatalogURL}");

            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                isInitialized = true;
                Debug.Log("[GithubDownloader] Addressables initialized successfully.");
            }
            else
            {
                Debug.LogError($"[GithubDownloader] Failed to initialize: {initHandle.OperationException}");
            }
        }

        /// <summary>
        /// Update remote catalog to check for new content
        /// </summary>
        public IEnumerator UpdateCatalog()
        {
            Debug.Log("[GithubDownloader] Checking for catalog updates...");
            
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;

            if (checkHandle.Status == AsyncOperationStatus.Succeeded)
            {
                List<string> catalogs = checkHandle.Result;
                
                if (catalogs != null && catalogs.Count > 0)
                {
                    Debug.Log($"[GithubDownloader] Found {catalogs.Count} catalog(s) to update.");
                    
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                    yield return updateHandle;

                    if (updateHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("[GithubDownloader] Catalogs updated successfully.");
                    }
                    else
                    {
                        Debug.LogError($"[GithubDownloader] Catalog update failed: {updateHandle.OperationException}");
                    }
                    
                    Addressables.Release(updateHandle);
                }
                else
                {
                    Debug.Log("[GithubDownloader] No catalog updates available.");
                }
            }
            else
            {
                Debug.LogError($"[GithubDownloader] Failed to check for updates: {checkHandle.OperationException}");
            }
            
            Addressables.Release(checkHandle);
        }

        #endregion

        #region Download Size Calculation

        /// <summary>
        /// Get download size for a specific level/asset key
        /// </summary>
        public IEnumerator GetDownloadSize(string assetKey, Action<long> onComplete)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(assetKey);
            yield return sizeHandle;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                long downloadSize = sizeHandle.Result;
                Debug.Log($"[GithubDownloader] Download size for '{assetKey}': {FormatBytes(downloadSize)}");
                onComplete?.Invoke(downloadSize);
                OnDownloadSizeCalculated?.Invoke(downloadSize);
            }
            else
            {
                Debug.LogError($"[GithubDownloader] Failed to get download size: {sizeHandle.OperationException}");
                onComplete?.Invoke(-1);
            }

            Addressables.Release(sizeHandle);
        }

        /// <summary>
        /// Get total download size for multiple levels
        /// </summary>
        public IEnumerator GetDownloadSize(List<string> assetKeys, Action<long> onComplete)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable<object>)assetKeys);
            yield return sizeHandle;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                long totalSize = sizeHandle.Result;
                Debug.Log($"[GithubDownloader] Total download size: {FormatBytes(totalSize)}");
                onComplete?.Invoke(totalSize);
                OnDownloadSizeCalculated?.Invoke(totalSize);
            }
            else
            {
                Debug.LogError($"[GithubDownloader] Failed to get download size: {sizeHandle.OperationException}");
                onComplete?.Invoke(-1);
            }

            Addressables.Release(sizeHandle);
        }

        #endregion

        #region Download Methods

        /// <summary>
        /// Download dependencies for a level prefab (without loading it)
        /// </summary>
        public IEnumerator DownloadLevelAssets(string levelKey)
        {
            if (activeDownloads.ContainsKey(levelKey))
            {
                Debug.LogWarning($"[GithubDownloader] '{levelKey}' is already downloading.");
                yield break;
            }

            Debug.Log($"[GithubDownloader] Starting download for '{levelKey}'...");

            int retryCount = 0;
            bool success = false;

            while (retryCount < maxRetryAttempts && !success)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(levelKey, false);
                activeDownloads[levelKey] = downloadHandle;

                while (!downloadHandle.IsDone)
                {
                    float progress = downloadHandle.GetDownloadStatus().Percent;
                    OnDownloadProgress?.Invoke(levelKey, progress);
                    yield return null;
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    success = true;
                    Debug.Log($"[GithubDownloader] Download completed for '{levelKey}'.");
                    OnDownloadComplete?.Invoke(levelKey);
                }
                else
                {
                    retryCount++;
                    Debug.LogWarning($"[GithubDownloader] Download failed for '{levelKey}'. Retry {retryCount}/{maxRetryAttempts}");
                    
                    if (retryCount < maxRetryAttempts)
                    {
                        yield return new WaitForSeconds(retryDelay);
                    }
                    else
                    {
                        string errorMsg = downloadHandle.OperationException?.Message ?? "Unknown error";
                        Debug.LogError($"[GithubDownloader] Download failed after {maxRetryAttempts} attempts: {errorMsg}");
                        OnDownloadFailed?.Invoke(levelKey, errorMsg);
                    }
                }

                Addressables.Release(downloadHandle);
                activeDownloads.Remove(levelKey);
            }
        }

        /// <summary>
        /// Download multiple level assets
        /// </summary>
        public IEnumerator DownloadMultipleLevels(List<string> levelKeys, Action<float> onOverallProgress = null)
        {
            int completedCount = 0;
            int totalCount = levelKeys.Count;

            foreach (string levelKey in levelKeys)
            {
                yield return StartCoroutine(DownloadLevelAssets(levelKey));
                completedCount++;
                
                float overallProgress = (float)completedCount / totalCount;
                onOverallProgress?.Invoke(overallProgress);
            }

            Debug.Log($"[GithubDownloader] All {totalCount} levels downloaded.");
        }

        /// <summary>
        /// Download and load a level prefab
        /// </summary>
        public IEnumerator DownloadAndLoadLevel<T>(string levelKey, Action<T> onComplete) where T : UnityEngine.Object
        {
            // First, download dependencies
            yield return StartCoroutine(DownloadLevelAssets(levelKey));

            // Then load the asset
            var loadHandle = Addressables.LoadAssetAsync<T>(levelKey);
            yield return loadHandle;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"[GithubDownloader] Level '{levelKey}' loaded successfully.");
                onComplete?.Invoke(loadHandle.Result);
            }
            else
            {
                Debug.LogError($"[GithubDownloader] Failed to load '{levelKey}': {loadHandle.OperationException}");
                onComplete?.Invoke(null);
            }

            // Don't release here if you want to keep the asset in memory
        }

        /// <summary>
        /// Check if a level is already downloaded (cached locally)
        /// </summary>
        public IEnumerator IsLevelDownloaded(string levelKey, Action<bool> onComplete)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(levelKey);
            yield return sizeHandle;

            bool isDownloaded = false;
            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // If download size is 0, it's already cached
                isDownloaded = sizeHandle.Result == 0;
            }

            onComplete?.Invoke(isDownloaded);
            Addressables.Release(sizeHandle);
        }

        #endregion

        #region Clear Cache

        /// <summary>
        /// Clear all cached asset bundles (use carefully!)
        /// </summary>
        public void ClearCache()
        {
            Caching.ClearCache();
            Debug.Log("[GithubDownloader] Asset bundle cache cleared.");
        }

        #endregion

        #region Utility Methods

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Cancel an active download
        /// </summary>
        public void CancelDownload(string levelKey)
        {
            if (activeDownloads.TryGetValue(levelKey, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                activeDownloads.Remove(levelKey);
                Debug.Log($"[GithubDownloader] Download cancelled for '{levelKey}'.");
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Release all active downloads
            foreach (var kvp in activeDownloads)
            {
                Addressables.Release(kvp.Value);
            }
            activeDownloads.Clear();
        }

        #endregion
    }
}

/*
 **Note**: In your GitHub repo, organize bundles like:
```
/AssetBundles
  /StandaloneWindows64
  /Android
  /iOS
 */