# Bone Reference Helper

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that adds a lot of useful things for transferring bone references of skinned mesh renderers

Buttons that get added:
![image](https://user-images.githubusercontent.com/12719947/185762036-d35a98b6-ea70-4aea-b71c-161f2ef099a5.png)

- Copy Bone References into Clipboard - reads all the references in bone list of SkinnedMeshRenderer and serializes them into Clipboard
- Paste directly - reads reference list from Clipboard and directly sets bone list to whatever was found in the Clipboard
- Paste based on names - reads reference list from Clipboard, and then goes through the current bone list of the skinned mesh renderer, replacing every bone reference with one that was found in Clipboard
- Set references from hierarchy - goes through the current bone list of the skinned mesh renderer, replacing every bone with one that was found in the provided hierarchy

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
2. Place [BoneReferenceHelper.dll](https://github.com/TheJebForge/BoneReferenceHelper/releases/latest/download/BoneReferenceHelper.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Neos logs.
