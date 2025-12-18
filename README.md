# Bone Reference Helper

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adds a lot of useful things for transferring bone references of skinned mesh renderers

### Elements that get added:
<img width="1009" height="423" alt="image" src="https://github.com/user-attachments/assets/f4fb773a-202d-4824-8006-0e7e6bb8268f" />

- Copy Bone References into Clipboard - reads all the references in bone list of SkinnedMeshRenderer and serializes them into Clipboard
- Paste directly - reads reference list from Clipboard and directly sets bone list to whatever was found in the Clipboard
- Paste based on names - reads reference list from Clipboard, and then goes through the current bone list of the skinned mesh renderer, replacing every bone reference with one that was found in Clipboard
- Set references from hierarchy - goes through the current bone list of the skinned mesh renderer, replacing every bone with one that was found in the provided hierarchy
- Ignore case - if enabled, letter case will be ignored while looking for substitute
- Use find and replace on the source names - if enabled, will use find and replace fields to replace things inside of bone names in current bone reference list or mesh bone list
- Use RegEx - if enabled, find and replace fields are treated as match and replace patterns for RegEx

### How find and replace feature works
The feature will perform find and replace operation in each name it sees from the bone list
- If find field is empty, it will work as C#'s string.Format where it will replace the entire name with the replace pattern
  - For example, if bone's name is `Hips` and replace field is `Outfit.{0}`, the result will be `Outfit.Hips`
- If find field contains something, it will perform simple find and replace
  - For example, if bone's name is `Outfit.Hips`, find field is `Outfit.` and replace field is empty, the result will be `Hips`
  - If bone's name is `UpperLeg`, find field is `Leg` and replace field is `Thigh`, the result will be `UpperThigh`
- If RegEx is enabled, it will work as C#'s Regex.Replace, see more here https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions#regular-expression-examples
  - Example: if bone's name is `Arm.L`, find field is `(.*)\.(.?)` and replace field is `$2_$1`, the result will be `L_Arm`
- If no match was found in the bone's name, it will be left as is

If you want to see what the find and replace feature is doing, you can enable replacementLog in the mod's settings and then check Resonite logs while using the feature.

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
