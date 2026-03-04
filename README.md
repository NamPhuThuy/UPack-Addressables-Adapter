# UPack-Addressables-Adapter

<pre>
UPack-Addressables-Adapter/    
├─ AddressablesToolr.cs  // Serializer Tool and Group-creating Tool   
├─ GithubDownloader.cs // Download Addressables content from GitHub
├─ LICENSE  
├─ README.md  
</pre>

Some scripts are modified from: https://github.com/laicasaane/unity-addressables-manager
├─ AddressablesManager.cs // Addressables Manager for Unity Editor

## Setup assets to remote
Step 1:
- Window -> Asset Management -> Addressables -> Groups  
- Create a new group and set its Build and Load Path to Remote.  

Step 2:
- Window -> Asset Management -> Addressables -> Profiles
- Set the Remote Load Path to your remote URL
`https://github.com/<github_username>/<repo_name>/tree/main/Android`
`https://github.com/<github_username>/<repo_name>/raw/refs/heads/main/Android`


Set the Remote Build Path to a local folder where the Addressables content will be built, e.g. `Assets/../AddressablesContent/[BuildTarget]

# Need update tut
The flow when modify data for remote-addessables-groups to not break the flow in newer-builds

# Note
After the first download, Addressables will caches downloaded bundles locally automatically. 
Where the Cache Lives on Device
- Android:  /data/data/<package>/cache/com.unity.addressables/
- iOS:      <AppSandbox>/Library/Caches/com.unity.addressables/


## 3 Situations Where Cache Gets Invalidated
1. OS Clears Cache (Biggest Risk on Mobile)
Android/iOS will DELETE the cache automatically when:
- Device storage is critically low
- User manually clears app cache in Settings
- On iOS: system cache pressure purge

2. You Push a New Bundle to CDN
Old bundle hash: a1b2c3   (cached on device)
New bundle hash: x9y8z7   (after you rebuild)
→ Addressables detects hash mismatch → re-downloads automatically

3. Player Reinstalls the App
Full cache wipe → fresh download required