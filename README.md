# Bone Reference Helper

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adds a lot of useful things for transferring bone references of skinned mesh renderers

### Buttons that get added:
![image](https://user-images.githubusercontent.com/12719947/185762036-d35a98b6-ea70-4aea-b71c-161f2ef099a5.png)

- Copy Bone References into Clipboard - reads all the references in bone list of SkinnedMeshRenderer and serializes them into Clipboard
- Paste directly - reads reference list from Clipboard and directly sets bone list to whatever was found in the Clipboard
- Paste based on names - reads reference list from Clipboard, and then goes through the current bone list of the skinned mesh renderer, replacing every bone reference with one that was found in Clipboard
- Set references from hierarchy - goes through the current bone list of the skinned mesh renderer, replacing every bone with one that was found in the provided hierarchy

### Some usecases:
- I want to apply clothes onto my avatar
  - Open your avatar mesh in inspector
  - Click 'Copy bone references into clipboard'
  - Open your clothes mesh in inspector
  - Click 'Paste based on names'
- I want to apply clothes onto my avatar, but they have some extra bones!
  - Open your clothes mesh in inspector
  - Drag and drop hips slot of the avatar's armature into Armature Root
  - Click 'Set references from hierarchy'
- Oh no! My bone list in SkinnedMeshRenderer is now completely empty!
  - Open your mesh in inspector
  - Set avatar's hips slot into Armature Root
  - Make sure 'Use mesh bone list instead' is checked
  - Click 'Set references from hierarchy'

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [BoneReferenceHelper.dll](https://github.com/TheJebForge/BoneReferenceHelper/releases/latest/download/BoneReferenceHelper.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
