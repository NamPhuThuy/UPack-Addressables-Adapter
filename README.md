# UPack-Addressables-Adapter

UPack-Addressables-Adapter/  
├─ AddressablesSerializer.cs  // Serializes assets into Addressables Groups
├─ GithubDownloader.cs // Download Addressables content from GitHub
├─ LICENSE
├─ README.md

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