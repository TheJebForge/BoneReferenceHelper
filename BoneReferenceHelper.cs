using Elements.Core;
using Elements.Assets;
using ResoniteModLoader;
using HarmonyLib;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoneReferenceHelper
{
    public class BoneReferenceHelper : ResoniteMod
    {
        public override string Name => "BoneReferenceHelper";
        public override string Author => "TheJebForge";
        public override string Version => "2.2.0";
        public override string Link => "https://github.com/TheJebForge/BoneReferenceHelper";

        [AutoRegisterConfigKey] static readonly ModConfigurationKey<bool> ReplacementLog = new ModConfigurationKey<bool>(
            "replacementLog",
            "Should the the name replacement be logged for testing purposes",
            () => false
        );

        static ModConfiguration _config; //If you use config settings, this will be where you interface with them.

        public override void OnEngineInit()
        {
            _config = GetConfiguration()!;
            _config.Save(true);
            
            Harmony harmony = new Harmony($"com.{Author}.{Name}");
            harmony.PatchAll();
        }

        class DialogSettings
        {
            public bool UseMeshBoneListInstead { get; set; }
            public bool UseFindAndReplace { get; set; }
            public bool ApplyOnDestinationInstead { get; set; }
            public bool UseRegex { get; set; }
            public bool IgnoreCase { get; set; } = true;
            public bool UseTrim { get; set; } = true;
            public string FindPattern { get; set; }
            public string ReplacePattern { get; set; }
        }

        static readonly Dictionary<SkinnedMeshRenderer, DialogSettings> SettingsMap = [];

        static DialogSettings GetSettings(SkinnedMeshRenderer instance) {
            if (SettingsMap.TryGetValue(instance, out DialogSettings settings))
                return settings;

            DialogSettings newSettings = new DialogSettings();
            SettingsMap.Add(instance, newSettings);
            
            return newSettings;
        }

        [HarmonyPatch(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.BuildInspectorUI))]
        class SkinnedMeshRenderer_BuildInspectorUI_Patch
        {
            static void Postfix(SkinnedMeshRenderer __instance, UIBuilder ui) {
                ui.Style.MinHeight = 360;

                DialogSettings settings = GetSettings(__instance);
                
                ui.NestInto(ui.Empty("BoneRefHelper"));
                {
                    ui.HorizontalHeader(36, out RectTransform header, out RectTransform content);

                    ui.Style.MinHeight = 24;

                    ui.NestInto(header);
                    ui.Text("Bone Reference Helper", alignment: Alignment.MiddleCenter);
                    ui.NestOut();

                    ui.NestInto(content);
                    {
                        ui.VerticalLayout(4f);
                        {
                            ui.Checkbox("Use mesh bone list for names instead", settings.UseMeshBoneListInstead).State.OnValueChange += field => settings.UseMeshBoneListInstead = field.Value;
                            ui.Button("Copy Bone References into Clipboard").LocalPressed += (button, _) => Task.Run(async () => await CopyBoneReferences(__instance, button, settings));

                            ui.HorizontalLayout(4f);
                            {
                                ui.Button("Paste directly").LocalPressed += (button, _) => Task.Run(async () => await PasteBoneReferencesDirectly(__instance, button));
                                ui.Button("Paste based on current names").LocalPressed += (button, _) => Task.Run(async () => await PasteBoneReferencesBasedOnNames(__instance, button, settings));
                            }
                            ui.NestOut();

                            Slot infoHolder = ui.Empty("Info Holder");
                            ui.NestInto(infoHolder);
                            {
                                ui.HorizontalLayout(4f);
                                {
                                    ReferenceField<Slot> slotField = infoHolder.AttachComponent<ReferenceField<Slot>>();

                                    const string key = "Reference";
                                    SyncMemberEditorBuilder.Build(
                                        slotField.GetSyncMember(key),
                                        "Armature",
                                        slotField.GetSyncMemberFieldInfo(key),
                                        ui);

                                    ui.Button("Set references from hierarchy").LocalPressed += (button, _) => SetReferencesFromHierarchy(__instance, button, slotField.Reference.Target, settings);
                                }
                                ui.NestOut();
                            }
                            ui.NestOut();
                            
                            ui.Checkbox("Use find and replace", settings.UseFindAndReplace).State.OnValueChange += field => settings.UseFindAndReplace = field.Value;
                            ui.Checkbox("Off - Apply find and replace on names from clipboard or armature / On - Apply on names from this mesh", settings.ApplyOnDestinationInstead).State.OnValueChange += field => settings.ApplyOnDestinationInstead = field.Value;
                            ui.Checkbox("Use RegEx", settings.UseRegex).State.OnValueChange += field => settings.UseRegex = field.Value;

                            {
                                ValueField<string> findField = infoHolder.AttachComponent<ValueField<string>>();
                                
                                const string key = "Value";
                                SyncMemberEditorBuilder.Build(
                                    findField.GetSyncMember(key),
                                    "Find",
                                    findField.GetSyncMemberFieldInfo(key),
                                    ui
                                );
                                
                                findField.Value.Changed += field => settings.FindPattern = (field as Sync<string>)?.Value;
                            }
                            
                            {
                                ValueField<string> replaceField = infoHolder.AttachComponent<ValueField<string>>();
                                
                                const string key = "Value";
                                SyncMemberEditorBuilder.Build(
                                    replaceField.GetSyncMember(key),
                                    "Replace",
                                    replaceField.GetSyncMemberFieldInfo(key),
                                    ui
                                );
                                
                                replaceField.Value.Changed += field => settings.ReplacePattern = (field as Sync<string>)?.Value;
                            }

                            ui.Text("See format help on github.com/TheJebForge/BoneReferenceHelper");
                            
                            ui.Checkbox("Ignore case", settings.IgnoreCase).State.OnValueChange += field => settings.IgnoreCase = field.Value;
                            ui.Checkbox("Ignore leading and trailing whitespace", settings.UseTrim).State.OnValueChange += field => settings.UseTrim = field.Value;
                        }
                        ui.NestOut();
                    }
                    ui.NestOut();
                }
                ui.NestOut();
            }

            static List<Bone> GetBonesList(SkinnedMeshRenderer instance) {
                if (instance.Mesh.Asset == null) return new List<Bone>();
                
                Mesh mesh = instance.Mesh.Asset;
                return mesh.Data.Bones.ToList();
            }

            static string LogIfDifferent(string original, string newValue, bool shouldLog) {
                if (shouldLog)
                    Msg(
                        original != newValue
                            ? $"Name '{original}' was replaced with '{newValue}'"
                            : "No replacement was done"
                    );
                return newValue;
            }

            static string PerformFindAndReplaceIfNeeded(string name, DialogSettings settings, bool shouldLog, bool isDestination) {
                if (!settings.UseFindAndReplace || settings.ApplyOnDestinationInstead != isDestination) return name;

                string find = settings.FindPattern ?? "";
                string replace = settings.ReplacePattern ?? "";
                
                if (shouldLog) Msg($"Performing find & replace on '{name}'");

                if (!settings.UseRegex) {
                    if (find == "") {
                        if (shouldLog) Msg($"Find is empty, using string.Format with '{replace}'");
                        return LogIfDifferent(name, string.Format(replace, name), shouldLog);
                    }

                    if (shouldLog) Msg($"Simple find and replace with '{find}' and '{replace}'");
                    return LogIfDifferent(name, name.Replace(find, replace), shouldLog);
                }

                if (shouldLog) Msg($"RegEx replace with '{find}' and '{replace}'");
                return LogIfDifferent(name, Regex.Replace(name, find, replace), shouldLog);
            }

            static string SanitizeForComparison(string name, DialogSettings settings) {
                if (settings.IgnoreCase) name = name.ToLowerInvariant();
                if (settings.UseTrim) name = name.Trim();
                return name;
            }

            static async Task CopyBoneReferences(SkinnedMeshRenderer instance, IButton button, DialogSettings settings) {
                List<Bone> bones = GetBonesList(instance);
                string lastText = button.LabelText;
                
                bool replaceLog = _config.GetValue(ReplacementLog);
                
                Msg($"Bone Count: {instance.Bones.Count}");
                StringBuilder builder = new StringBuilder();

                for (int index = 0; index < instance.Bones.Count; index++) {
                    Slot slot = instance.Bones[index];
                    string refId = "null";
                    string name = "null";

                    if (slot != null) {
                        refId = slot.ReferenceID.ToString();
                        name = slot.Name;
                    }

                    if (settings.UseMeshBoneListInstead) {
                        try {
                            name = bones[index].Name;
                        } catch(IndexOutOfRangeException) {}
                    }

                    name = PerformFindAndReplaceIfNeeded(name, settings, replaceLog, true);

                    builder.Append($"{refId},{name}\n");
                }

                await instance.InputInterface.Clipboard.SetText(builder.ToString());

                button.LabelText = "Copied!";
                
                button.RunInSeconds(1f, () => button.LabelText = lastText);
            }

            static async Task PasteBoneReferencesDirectly(SkinnedMeshRenderer instance, IButton button) {
                string lastText = button.LabelText;
                
                string clipboard = (await instance.InputInterface.Clipboard.GetText()).Trim();
                
                instance.World.RunSynchronously(() => {
                    if (clipboard.StartsWith("ID")) {
                        instance.Bones.Clear();

                        int countProcessed = 0;
                    
                        foreach (string bone in clipboard.Split('\n')) {
                            int commaIndex = bone.IndexOf(',');
                            try {
                                RefID reference = RefID.Parse(bone[..commaIndex]);

                                instance.Bones.Add((Slot)instance.World.ReferenceController.GetObjectOrNull(reference));

                                countProcessed++;
                            }
                            catch (ArgumentException) {
                                instance.Bones.Add(null);
                            }
                        }

                        button.LabelText = $"Bones processed: {countProcessed}";
                    }
                    else {
                        button.LabelText = "No valid bones in clipboard";
                    }
                
                    button.RunInSeconds(1f, () => button.LabelText = lastText);
                });
            }

            static Dictionary<string, RefID> ParseStringToDictionary(string clipboard, DialogSettings settings) {
                return clipboard.Split('\n')
                    .Select(boneString =>
                    {
                        int commaIndex = boneString.IndexOf(',');
                        try {
                            RefID reference = RefID.Parse(boneString[..commaIndex]);
                            string name = boneString[(commaIndex + 1)..];

                            name = SanitizeForComparison(name, settings);

                            return new KeyValuePair<string, RefID>(name, reference);
                        }
                        catch (ArgumentException) {
                            return new KeyValuePair<string, RefID>("null", RefID.Null);
                        }
                    })
                    .Distinct()
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            static Dictionary<string, RefID> IndexAllChildren(Slot root, DialogSettings settings) {
                Dictionary<string, RefID> foundSlots = new Dictionary<string, RefID>();

                bool replaceLog = _config.GetValue(ReplacementLog);
                
                Queue<Slot> slotQueue = new Queue<Slot>();
                slotQueue.Enqueue(root);

                while (slotQueue.Count > 0) {
                    Slot current = slotQueue.Dequeue();

                    try {
                        string name = current.Name;

                        name = PerformFindAndReplaceIfNeeded(name, settings, replaceLog, false);

                        name = SanitizeForComparison(name, settings);
                        
                        foundSlots.Add(name, current.ReferenceID);
                    } catch(ArgumentException) {}

                    foreach (Slot child in current.Children) {
                        slotQueue.Enqueue(child);
                    }
                }

                return foundSlots;
            }

            static int ReplaceSlotReferences(SkinnedMeshRenderer instance, Dictionary<string, RefID> reference, DialogSettings settings) {
                int countProcessed = 0;
                
                List<Bone> bones = GetBonesList(instance);
                
                // Create elements if using mesh bone list
                if (settings.UseMeshBoneListInstead) {
                    while (instance.Bones.Count < bones.Count) {
                        instance.Bones.Add(null);
                    }
                }

                bool replaceLog = _config.GetValue(ReplacementLog);
                
                List<SyncRef<Slot>> list = instance.Bones.Elements.ToList();

                for (int index = 0; index < list.Count; index++) {
                    SyncRef<Slot> slot = list[index];
                    if (slot.Target == null && !settings.UseMeshBoneListInstead) continue;

                    try {
                        string slotName = settings.UseMeshBoneListInstead ? bones[index].Name : slot.Target.Name;

                        slotName = PerformFindAndReplaceIfNeeded(slotName, settings, replaceLog, true);

                        slotName = SanitizeForComparison(slotName, settings);

                        if (!reference.TryGetValue(slotName, out RefID value)) continue;

                        slot.Value = value;
                        countProcessed++;
                    } catch(IndexOutOfRangeException) {}
                }

                return countProcessed;
            }

            static async Task PasteBoneReferencesBasedOnNames(SkinnedMeshRenderer instance, IButton button, DialogSettings settings) {
                string lastText = button.LabelText;
                
                string clipboard = (await instance.InputInterface.Clipboard.GetText()).Trim();

                Dictionary<string, RefID> boneReference = ParseStringToDictionary(clipboard, settings);

                instance.World.RunSynchronously(() => {
                    int countProcessed = ReplaceSlotReferences(instance, boneReference, settings);

                    button.LabelText = $"Found and replaced: {countProcessed}";
                
                    button.RunInSeconds(1f, () => button.LabelText = lastText);
                });
            }
            
            static void SetReferencesFromHierarchy(SkinnedMeshRenderer instance, IButton button, Slot root, DialogSettings settings) {
                string lastText = button.LabelText;

                if (root != null) {
                    Dictionary<string, RefID> boneReference = IndexAllChildren(root, settings);

                    int countProcessed = ReplaceSlotReferences(instance, boneReference, settings);

                    button.LabelText = $"Found and replaced: {countProcessed}";
                }
                else {
                    button.LabelText = "No armature slot was provided";
                }

                button.RunInSeconds(1f, () => button.LabelText = lastText);
            }
        }
    }
}
