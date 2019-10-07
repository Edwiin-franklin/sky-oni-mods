﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Klei.AI;
using Klei.AI.DiseaseGrowthRules;
using PeterHan.PLib;
using UnityEngine;
using static SkyLib.Logger;
using static SkyLib.OniUtils;
using Sicknesses = Database.Sicknesses;

namespace DiseasesReimagined
{
    // Patches for disease changes
    class DiseasesPatch
    {
        // misc bookkeeping
        public static class Mod_OnLoad
        {
            public static void OnLoad()
            {
                StartLogging();

                AddDiseaseName(SlimeLethalSickness.ID, "Slimelung (lethal)");
                AddDiseaseName(SlimeCoughSickness.ID, "Slimelung (cough)");
                AddDiseaseName(FoodPoisonVomiting.ID, "Food Poisoning (vomiting)");
                
                SkipNotifications.Skip(SlimeLethalSickness.ID);
                SkipNotifications.Skip(SlimeCoughSickness.ID);
                SkipNotifications.Skip(FoodPoisonVomiting.ID);
            }
            
            // Helper method to find a specific attribute modifier
            public static AttributeModifier FindAttributeModifier(List<Sickness.SicknessComponent> components, string id)
            {
                var attr_mod = (AttributeModifierSickness)components.Find(comp => comp is AttributeModifierSickness);
                return Array.Find(attr_mod.Modifers, mod => mod.AttributeId == id);
            }
        }

        // Modifies the Curative Tablet's valid cures
        [HarmonyPatch(typeof(BasicCureConfig), "CreatePrefab")]
        public static class BasicCureConfig_CreatePrefab_Patch
        {
            public static void Postfix(GameObject __result)
            {
                var medinfo = __result.AddOrGet<MedicinalPill>().info;
                // The basic cure now doesn't cure the base disease, only certain symptoms
                medinfo.curedSicknesses = new List<string>(new[] {FoodPoisonVomiting.ID, SlimeCoughSickness.ID});
            }
        }

        // Adds custom disease cures to the doctor stations
        [HarmonyPatch(typeof(DoctorStation), "OnStorageChange")]
        public static class DoctorStation_OnStorageChange_Patch
        {
            public static bool Prefix(DoctorStation __instance, Dictionary<HashedString, Tag> ___treatments_available,
                                      Storage ___storage, DoctorStation.StatesInstance ___smi)
            {
                var docStation = Traverse.Create(__instance);
                ___treatments_available.Clear();
                
                foreach (GameObject go in ___storage.items)
                {
                    if (go.HasTag(GameTags.MedicalSupplies))
                    {
                        Tag tag = go.PrefabID();
                        if (tag == "IntermediateCure")
                        {
                            docStation.CallMethod("AddTreatment", SlimeLethalSickness.ID, tag);
                        }
                        if (tag == "AdvancedCure")
                            docStation.CallMethod("AddTreatment", ZombieSickness.ID, tag);
                    }
                }

                ___smi.sm.hasSupplies.Set(___treatments_available.Count > 0, ___smi);

                return false;
            }
        }

        // Registers our new sicknesses to the DB
        [HarmonyPatch(typeof(Sicknesses), MethodType.Constructor, typeof(ResourceSet))]
        public static class Sicknesses_Constructor_Patch
        {
            public static void Postfix(Sicknesses __instance)
            {
                __instance.Add(new FoodPoisonVomiting());
                __instance.Add(new SlimeCoughSickness());
                __instance.Add(new SlimeLethalSickness());
            }
        }

        // Enables food poisoning to give different symptoms when infected with it
        [HarmonyPatch(typeof(FoodSickness), MethodType.Constructor)]
        public static class FoodSickness_Constructor_Patch
        {
            public static void Postfix(FoodSickness __instance)
            {
                var trav = Traverse.Create(__instance);
                trav.CallMethod("AddSicknessComponent",
                    new AddSicknessComponent(FoodPoisonVomiting.ID, "Food poisoning"));
                trav.CallMethod("AddSicknessComponent",
                    new AttributeModifierSickness(new AttributeModifier[]
                    {
                        // 10% stress/cycle
                        new AttributeModifier(Db.Get().Amounts.Stress.deltaAttribute.Id, 0.01666666666f, "Food poisoning")
                    }));
            }
        }

        // Enables Slimelung to give different symptoms when infected with it.
        [HarmonyPatch(typeof(SlimeSickness), MethodType.Constructor)]
        public static class SlimeSickness_Constructor_Patch
        {
            public static void Postfix(SlimeSickness __instance, ref List<Sickness.SicknessComponent> ___components)
            {
                var sickness = Traverse.Create(__instance);

                // Remove the vanilla SlimelungComponent
                ___components = ___components.Where(comp => !(comp is SlimeSickness.SlimeLungComponent)).ToList();

                // Then replace it with our own
                sickness.CallMethod("AddSicknessComponent",
                    new AddSicknessComponent(SlimeCoughSickness.ID, "Slimelung"));
                sickness.CallMethod("AddSicknessComponent",
                    new AddSicknessComponent(SlimeLethalSickness.ID, "Slimelung"));
                // Also add some minor stress
                sickness.CallMethod("AddSicknessComponent",
                    new AttributeModifierSickness(new AttributeModifier[]
                    {
                        // 10% stress/cycle
                        new AttributeModifier(Db.Get().Amounts.Stress.deltaAttribute.Id, 0.01666666666f, "Slimelung")
                    }));
            }
        }
        
        // Increases sunburn stress
        [HarmonyPatch(typeof(Sunburn), MethodType.Constructor)]
        public static class Sunburn_Constructor_Patch
        {
            public static void Postfix(ref List<Sickness.SicknessComponent> ___components)
            {
                var stressmod =
                    Mod_OnLoad.FindAttributeModifier(___components, Db.Get().Amounts.Stress.deltaAttribute.Id);
                Traverse.Create(stressmod).SetField("Value", .04166666666f); // 30% stress/cycle
            }
        }

        [HarmonyPatch(typeof(ZombieSickness), MethodType.Constructor)]
        public static class ZombieSickness_Constructor_Patch
        {
            public static void Postfix(ZombieSickness __instance)
            {
                // 20% stress/cycle
                Traverse.Create(__instance)
                        .CallMethod("AddSicknessComponent",
                    new AttributeModifierSickness(new AttributeModifier[]
                    {
                        new AttributeModifier(Db.Get().Amounts.Stress.deltaAttribute.Id, 0.03333333333f, "Zombie spores")
                    }));
            }
        }
        

        // Enables skipping notifications when infected
        [HarmonyPatch(typeof(SicknessInstance.States), "InitializeStates")]
        public static class SicknessInstance_States_InitializeStates_Patch
        {
            public static void Postfix(SicknessInstance.States __instance)
            {
                var old_enterActions = __instance.infected.enterActions;
                if (old_enterActions == null)
                {
                    return;
                }
                var new_enterActions = __instance.infected.enterActions = new List<StateMachine.Action>();
                for (var i = 0; i < old_enterActions.Count; i++)
                {
                    if (old_enterActions[i].name != "DoNotification()")
                    {
                        new_enterActions.Add(old_enterActions[i]);
                    }
                    else
                    {
                        DoNotification(__instance);
                    }
                }
            }

            // DoNotification but with a custom version that checks the whitelist.
            public static void DoNotification(SicknessInstance.States __instance)
            {
                var state_target = Traverse
                  .Create(__instance.infected)
                  .CallMethod<GameStateMachine<SicknessInstance.States, SicknessInstance.StatesInstance, SicknessInstance, object>.TargetParameter>("GetStateTarget");
                __instance.infected.Enter("DoNotification()", smi =>
                {
                    // if it's not to be skipped, (reluctantly) do the notification.
                    if (!SkipNotifications.SicknessIDs.Contains(smi.master.Sickness.Id))
                    {
                        Notification notification = Traverse.Create(smi.master).GetField<Notification>("notification");
                        state_target.Get<Notifier>(smi).Add(notification, string.Empty);
                    }
                });
            }
        }

        // Make food poisoning rapidly die on gas
        [HarmonyPatch(typeof(FoodGerms), "PopulateElemGrowthInfo")]
        public static class FoodGerms_PopulateElemGrowthInfo
        {
            public static void Postfix(FoodGerms __instance)
            {
                var rules = __instance.growthRules;
                // Simplest method is to have food poisoning max population on air be 0
                rules.ForEach(rule =>
                {
                    if ((rule as StateGrowthRule)?.state == Element.State.Gas)
                    {
                        rule.maxCountPerKG = 0;
                        rule.minCountPerKG = 0;
                        rule.overPopulationHalfLife = 0.001f;
                    }
                });
                rules.Add(new ElementGrowthRule(SimHashes.Polypropylene)
                {
                    populationHalfLife = 300f,
                    overPopulationHalfLife = 300f
                });
            }
        }

        // Buff zombie spores to diffuse on solids
        [HarmonyPatch(typeof(ZombieSpores), "PopulateElemGrowthInfo")]
        public static class ZombieSpores_PopulateElemGrowthInfo_Patch
        {
            public static void Postfix(ZombieSpores __instance)
            {
                var rules = __instance.growthRules;
                foreach (var rule in rules)
                    // Dying on Solid changed to spread around tiles
                    if (rule is StateGrowthRule stateRule && stateRule.state == Element.State.
                        Solid)
                    {
                        stateRule.minDiffusionCount = 20000;
                        stateRule.diffusionScale = 0.001f;
                        stateRule.minDiffusionInfestationTickCount = 1;
                    }
                // And it survives on lead and iron ore, but has a low overpop threshold
                rules.Add(new ElementGrowthRule(SimHashes.Lead)
                {
                    underPopulationDeathRate = 0.0f,
                    populationHalfLife = float.PositiveInfinity,
                    overPopulationHalfLife = 300.0f,
                    maxCountPerKG = 100.0f,
                    diffusionScale = 0.001f,
                    minDiffusionCount = 50000,
                    minDiffusionInfestationTickCount = 1
                });
                rules.Add(new ElementGrowthRule(SimHashes.IronOre)
                {
                    underPopulationDeathRate = 0.0f,
                    populationHalfLife = float.PositiveInfinity,
                    overPopulationHalfLife = 300.0f,
                    maxCountPerKG = 100.0f,
                    diffusionScale = 0.001f,
                    minDiffusionCount = 50000,
                    minDiffusionInfestationTickCount = 1
                });
                // But gets rekt on abyssalite and neutronium
                rules.Add(new ElementGrowthRule(SimHashes.Katairite)
                {
                    populationHalfLife = 5.0f,
                    overPopulationHalfLife = 5.0f,
                    minDiffusionCount = 1000000
                });
                rules.Add(new ElementGrowthRule(SimHashes.Unobtanium)
                {
                    populationHalfLife = 5.0f,
                    overPopulationHalfLife = 5.0f,
                    minDiffusionCount = 1000000
                });
                // -75% on plastic all germs
                rules.Add(new ElementGrowthRule(SimHashes.Polypropylene)
                {
                    populationHalfLife = 300f,
                    overPopulationHalfLife = 300f
                });
            }
        }

        // Make slimelung die on plastic
        [HarmonyPatch(typeof(SlimeGerms), "PopulateElemGrowthInfo")]
        public static class SlimeGerms_PopulateElemGrowthInfo
        {
            public static void Postfix(SlimeGerms __instance)
            {
                __instance.growthRules.Add(new ElementGrowthRule(SimHashes.Polypropylene)
                {
                    populationHalfLife = 300f,
                    overPopulationHalfLife = 300f
                });
            }
        }

        // Transfer germs from germy irrigation to the plant
        [HarmonyPatch(typeof(PlantElementAbsorbers), "Sim200ms")]
        public static class PlantElementAbsorbers_Sim200ms_Patch
        {
            public static void Prefix(List<PlantElementAbsorber> ___data, float dt,
                ref bool ___updating)
            {
                // This variable is remapped to an instance variable so the store is not dead
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                ___updating = true;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                foreach (var absorber in ___data)
                {
                    var storage = absorber.storage;
                    GameObject farmTile;
                    if (storage != null && (farmTile = storage.gameObject) != null)
                    {
                        if (absorber.consumedElements == null)
                        {
                            var info = absorber.localInfo;
                            InfectPlant(farmTile, info.massConsumptionRate * dt, absorber,
                                info.tag);
                        }
                        else
                            // Grrr LocalInfo is not convertible to ConsumeInfo
                            foreach (var info in absorber.consumedElements)
                                InfectPlant(farmTile, info.massConsumptionRate * dt, absorber,
                                    info.tag);
                    }
                }
                ___updating = false;
            }

            // Infect the plant with germs from the irrigation material
            private static void InfectPlant(GameObject farmTile, float required,
                PlantElementAbsorber absorber, Tag material)
            {
                var storage = absorber.storage;
                GameObject plant;
                PrimaryElement irrigant;
                // Check all available items
                while (required > 0.0f && (irrigant = storage.FindFirstWithMass(material)) !=
                    null)
                {
                    float mass = irrigant.Mass, consumed = Mathf.Min(required, mass);
                    int disease = irrigant.DiseaseCount;
                    if (disease > 0 && (plant = farmTile.GetComponent<PlantablePlot>()?.
                        Occupant) != null)
                    {
                        plant.GetComponent<PrimaryElement>()?.AddDisease(irrigant.DiseaseIdx,
                            Mathf.RoundToInt(required * disease / mass), "Irrigation");
                    }
                    required -= consumed;
                }
            }
        }

        // Sink germ transfer
        [HarmonyPatch(typeof(HandSanitizer.Work), "OnWorkTick")]
        public static class HandSanitizer_Work_OnWorkTick_Patch
        {
            public static void Prefix(HandSanitizer.Work __instance, float dt)
            {
                GermySinkManager.Instance?.SinkWorkTick(__instance, dt);
            }
        }

        [HarmonyPatch(typeof(HandSanitizer.Work), "OnStartWork")]
        public static class HandSanitizer_Work_OnStartWork_Patch
        {
            public static void Prefix(HandSanitizer.Work __instance)
            {
                GermySinkManager.Instance?.StartGermyWork(__instance);
            }
        }

        [HarmonyPatch(typeof(HandSanitizer.Work), "OnCompleteWork")]
        public static class HandSanitizer_Work_OnCompleteWork_Patch
        {
            public static void Postfix(HandSanitizer.Work __instance, Worker worker)
            {
                GermySinkManager.Instance?.FinishGermyWork(__instance, worker);
            }
        }

        // Shower germ transfer
        [HarmonyPatch(typeof(Shower), "OnWorkTick")]
        public static class Shower_OnWorkTick_Patch
        {
            public static void Prefix(Shower __instance, float dt)
            {
                GermySinkManager.Instance?.ShowerWorkTick(__instance, dt);
            }
        }

        [HarmonyPatch(typeof(Shower), "OnStartWork")]
        public static class Shower_OnStartWork_Patch
        {
            public static void Prefix(Shower __instance)
            {
                GermySinkManager.Instance?.StartGermyWork(__instance);
            }
        }

        [HarmonyPatch(typeof(Shower), "OnAbortWork")]
        public static class Shower_OnAbortWork_Patch
        {
            public static void Postfix(Shower __instance, Worker worker)
            {
                GermySinkManager.Instance?.FinishGermyWork(__instance, worker);
            }
        }

        [HarmonyPatch(typeof(Shower), "OnCompleteWork")]
        public static class Shower_OnCompleteWork_Patch
        {
            public static void Postfix(Shower __instance, Worker worker)
            {
                GermySinkManager.Instance?.FinishGermyWork(__instance, worker);
            }
        }

        // Prevent OCD hand washing by observing the hand wash cooldown
        [HarmonyPatch]
        public static class WashHandsReactable_InternalCanBegin_Patch
        {
            public static void Postfix(GameObject new_reactor, ref bool __result)
            {
                var cooldown = new_reactor.GetComponent<WashCooldownComponent>();
                if (cooldown != null && !cooldown.CanWash)
                    __result = false;
            }

            public static MethodBase TargetMethod()
            {
                // Find private class by name
                var parentType = typeof(HandSanitizer);
                var childType = parentType.GetNestedType("WashHandsReactable", BindingFlags.
                    NonPublic | BindingFlags.Instance);
                if (childType == null)
                    throw new InvalidOperationException("Could not patch hand wash class!");
                try
                {
                    var targetMethod = childType.GetMethod("InternalCanBegin", BindingFlags.
                        Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (targetMethod == null)
                        throw new InvalidOperationException("Could not patch hand wash method!");
#if DEBUG
                    PUtil.LogDebug("Patched hand wash method: " + targetMethod);
#endif
                    return targetMethod;
                }
                catch (AmbiguousMatchException e)
                {
                    throw new InvalidOperationException("Could not patch hand wash method!", e);
                }
            }
        }

        // Transfers germs from one object to another using their mass ratios
        public static void TransferByMassRatio(GameObject parent, GameObject child)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");
            if (child == null)
                throw new ArgumentNullException("child");
            PrimaryElement parentElement = parent.GetComponent<PrimaryElement>(),
                childElement = child.GetComponent<PrimaryElement>();
            float seedMass;
            int germs, subGerms;
            // Distribute the germs by mass ratio if there are any
            if (parentElement != null && childElement != null && (seedMass = childElement.
                Mass) > 0.0f && (germs = parentElement.DiseaseCount) > 0)
            {
                byte disease = parentElement.DiseaseIdx;
                subGerms = Mathf.RoundToInt(germs * seedMass / (seedMass +
                    parentElement.Mass));
                // Seed germs
                childElement.AddDisease(disease, subGerms, "TransferToChild");
                // Plant germs
                parentElement.AddDisease(disease, -subGerms, "TransferFromParent");
            }
        }

        // Transfer germs from plant to fruit
        [HarmonyPatch(typeof(Crop), "SpawnFruit")]
        public static class Crop_SpawnFruit_Patch
        {
            // Transfers germs from crop to child (fruit)
            internal static void AddGerms(Crop parent, GameObject child)
            {
                TransferByMassRatio(parent.gameObject, child);
            }

            public static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> method)
            {
                MethodBase setTemp = null, germify = null;
                // No easy way to get the game object without a replacement or transpiler
                try
                {
                    var tempProp = typeof(PrimaryElement).GetProperty("Temperature",
                        BindingFlags.Public | BindingFlags.Instance);
                    // set_Temperature
                    if (tempProp != null)
                        setTemp = tempProp.GetSetMethod(true);
                    germify = typeof(Crop_SpawnFruit_Patch).GetMethod(nameof(AddGerms),
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                }
                catch (AmbiguousMatchException e)
                {
                    // This is not good
                    PUtil.LogException(e);
                }
                foreach (var instr in method)
                    if (instr.opcode == OpCodes.Callvirt && instr.operand == setTemp &&
                        germify != null)
                    {
                        yield return instr;
                        // ldarg.0 loads "this"
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        // ldloc.1 loads the returned GameObject
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        // Callvirt AddGerms
                        yield return new CodeInstruction(OpCodes.Call, germify);
                    }
                    else
                        // Rest of method
                        yield return instr;
            }
        }

        // Transfer germs from plant to seed
        [HarmonyPatch(typeof(SeedProducer), "ProduceSeed")]
        public static class SeedProducer_ProduceSeed_Patch
        {
            public static void Postfix(SeedProducer __instance, GameObject __result)
            {
                var seed = __result;
                var obj = __instance.gameObject;
                if (seed != null && obj != null)
                    TransferByMassRatio(obj, seed);
            }
        }

        // Sporechids spread spores onto their current tile
        [HarmonyPatch(typeof(EvilFlower), "OnSpawn")]
        public static class EvilFlower_OnSpawn_Patch
        {
            public static void Postfix(EvilFlower __instance)
            {
                __instance.gameObject?.AddOrGet<MoreEvilFlower>();
            }
        }

        // Manage lifecycle of GermySinkManager
        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public static class Game_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                GermySinkManager.CreateInstance();
            }
        }

        [HarmonyPatch(typeof(Game), "DestroyInstances")]
        public static class Game_DestroyInstances_Patch
        {
            public static void Postfix()
            {
                GermySinkManager.DestroyInstance();
            }
        }
    }
}
