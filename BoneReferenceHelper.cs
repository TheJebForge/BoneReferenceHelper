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

namespace BoneReferenceHelper
{
    public class BoneReferenceHelper : ResoniteMod
    {
        public override string Name => "BoneReferenceHelper";
        public override string Author => "TheJebForge";
        public override string Version => "2.0.4";
        public override string Link => "https://github.com/TheJebForge/BoneReferenceHelper";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony($"net.{Author}.{Name}");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.BuildInspectorUI))]
        class SkinnedMeshRenderer_BuildInspectorUI_Patch
        {
            static void Postfix(SkinnedMeshRenderer __instance, UIBuilder ui) {

                ui.Style.MinHeight = 24 * 4 + 4 * 3 + 36;

                bool UseMeshBoneListInstead = false;
                
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
                            ui.Checkbox("Use mesh bone list for names instead", false).State.OnValueChange += field => UseMeshBoneListInstead = field.Value;
                            ui.Button("Copy Bone References into Clipboard").LocalPressed += (button, data) => CopyBoneReferences(__instance, button, UseMeshBoneListInstead);

                            ui.HorizontalLayout(4f);
                            {
                                ui.Button("Paste directly").LocalPressed += (button, data) => PasteBoneReferencesDirectly(__instance, button);
                                ui.Button("Paste based on current names").LocalPressed += (button, data) => PasteBoneReferencesBasedOnNames(__instance, button, UseMeshBoneListInstead);
                            }
                            ui.NestOut();

                            Slot slotRefHolder = ui.Empty("Slot Reader");
                            ui.NestInto(slotRefHolder);
                            {
                                ui.HorizontalLayout(4f);
                                {
                                    ReferenceField<Slot> slotField = slotRefHolder.AttachComponent<ReferenceField<Slot>>();

                                    const int index = 3;
                                    SyncMemberEditorBuilder.Build(
                                        slotField.GetSyncMember(index),
                                        "Armature",
                                        slotField.GetSyncMemberFieldInfo(index),
                                        ui);

                                    ui.Button("Set references from hierarchy").LocalPressed += (button, data) => SetReferencesFromHierarchy(__instance, button, slotField.Reference.Target, UseMeshBoneListInstead);
                                }
                                ui.NestOut();
                            }
                            ui.NestOut();
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
            static void CopyBoneReferences(SkinnedMeshRenderer instance, IButton button, bool meshBoneList) {
                List<Bone> bones = GetBonesList(instance);
                string lastText = button.LabelText;
                
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

                    if (meshBoneList) {
                        try {
                            name = bones[index].Name;
                        } catch(IndexOutOfRangeException) {}
                    }

                    builder.AppendFormat("{0},{1}\n", refId, name);
                }

                instance.InputInterface.Clipboard.SetText(builder.ToString());

                button.LabelText = "Copied!";
                
                button.RunInSeconds(1f, () => button.LabelText = lastText);
            }

            static void PasteBoneReferencesDirectly(SkinnedMeshRenderer instance, IButton button) {
                string lastText = button.LabelText;

                string clipboard = instance.InputInterface.Clipboard.GetText().Trim();
                
                if (clipboard.StartsWith("ID")) {
                    instance.Bones.Clear();

                    int countProcessed = 0;
                    
                    foreach (string bone in clipboard.Split('\n')) {
                        int commaIndex = bone.IndexOf(',');
                        try {
                            RefID reference = RefID.Parse(bone.Substring(0, commaIndex));

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
            }
            
            static Dictionary<string, RefID> ParseStringToDictionary(string clipboard) {
                return clipboard.Split('\n')
                    .Select(boneString =>
                    {
                        int commaIndex = boneString.IndexOf(',');
                        try {
                            RefID reference = RefID.Parse(boneString.Substring(0, commaIndex));
                            string name = boneString.Substring(commaIndex + 1);

                            return new KeyValuePair<string, RefID>(name, reference);
                        }
                        catch (ArgumentException) {
                            return new KeyValuePair<string, RefID>("null", RefID.Null);
                        }
                    })
                    .Distinct()
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            static Dictionary<string, RefID> IndexAllChildren(Slot root) {
                Dictionary<string, RefID> foundSlots = new Dictionary<string, RefID>();

                Queue<Slot> slotQueue = new Queue<Slot>();
                slotQueue.Enqueue(root);

                while (slotQueue.Count > 0) {
                    Slot current = slotQueue.Dequeue();

                    try {
                        foundSlots.Add(current.Name, current.ReferenceID);
                    } catch(ArgumentException) {}

                    foreach (Slot child in current.Children) {
                        slotQueue.Enqueue(child);
                    }
                }

                return foundSlots;
            }

            static int ReplaceSlotReferences(SkinnedMeshRenderer instance, Dictionary<string, RefID> reference, bool meshBoneList) {
                int countProcessed = 0;
                
                List<Bone> bones = GetBonesList(instance);
                
                // Create elements if using mesh bone list
                if (meshBoneList) {
                    while (instance.Bones.Count < bones.Count) {
                        instance.Bones.Add(null);
                    }
                }
                
                List<SyncRef<Slot>> list = instance.Bones.Elements.ToList();

                for (int index = 0; index < list.Count; index++) {
                    SyncRef<Slot> slot = list[index];
                    if (slot.Target == null && !meshBoneList) continue;

                    try {
                        string slotName = meshBoneList ? bones[index].Name : slot.Target.Name;

                        if (!reference.ContainsKey(slotName)) continue;

                        slot.Value = reference[slotName];
                        countProcessed++;
                    } catch(IndexOutOfRangeException) {}
                }

                return countProcessed;
            }

            static void PasteBoneReferencesBasedOnNames(SkinnedMeshRenderer instance, IButton button, bool meshBoneList) {
                string lastText = button.LabelText;
                
                string clipboard = instance.InputInterface.Clipboard.GetText().Trim();

                Dictionary<string, RefID> boneReference = ParseStringToDictionary(clipboard);

                int countProcessed = ReplaceSlotReferences(instance, boneReference, meshBoneList);

                button.LabelText = $"Found and replaced: {countProcessed}";
                
                button.RunInSeconds(1f, () => button.LabelText = lastText);
            }
            
            static void SetReferencesFromHierarchy(SkinnedMeshRenderer instance, IButton button, Slot root, bool meshBoneList) {
                string lastText = button.LabelText;

                if (root != null) {
                    Dictionary<string, RefID> boneReference = IndexAllChildren(root);

                    int countProcessed = ReplaceSlotReferences(instance, boneReference, meshBoneList);

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
