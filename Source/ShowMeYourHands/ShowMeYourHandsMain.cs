﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class ShowMeYourHandsMain
{
    public static readonly Dictionary<Thing, Tuple<Vector3, float>> weaponLocations =
        new Dictionary<Thing, Tuple<Vector3, float>>();

    public static readonly Dictionary<ThingDef, Vector3> southOffsets = new Dictionary<ThingDef, Vector3>();
    public static readonly Dictionary<ThingDef, Vector3> northOffsets = new Dictionary<ThingDef, Vector3>();
    public static readonly Dictionary<ThingDef, Vector3> eastOffsets = new Dictionary<ThingDef, Vector3>();
    public static readonly Dictionary<ThingDef, Vector3> westOffsets = new Dictionary<ThingDef, Vector3>();

    public static readonly List<ThingDef> IsColorable;

    public static readonly Harmony harmony;

    public static readonly bool BabysAndChildrenLoaded;

    public static readonly MethodInfo GetBodySizeScaling;

    public static readonly bool OversizedWeaponLoaded;

    public static readonly bool EnableOversizedLoaded;

    public static bool DualWieldLoaded;

    public static bool YayoAdoptedLoaded;

    public static readonly BodyPartDef HandDef;

    public static readonly Dictionary<HediffDef, Color> HediffColors;

    public static readonly List<string> knownPatches = new List<string>
    {
        // This mod
        "Mlie.ShowMeYourHands",
        // Yayos Combat 3
        // Replaces weapon drawer
        "com.yayo.combat",
        "com.yayo.combat3",
        "com.yayo.yayoAni",
        "com.yayo.yayoAni.continued",
        // Dual Wield
        // Replaces weapon drawer if dual wielding
        "Roolo.DualWield",
        // Vanilla Expanded Framework
        "OskarPotocki.VFECore",
        // Vanilla Weapons Expanded - Laser
        // Modifies weapon position for lasers
        "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
        // JecsTools
        "jecstools.jecrell.comps.oversized",
        "rimworld.androitiers-jecrell.comps.oversized",
        "jecstools.jecrell.comps.installedpart",
        "rimworld.Ogliss.comps.oversized",
        "rimworld.jecrellpelador.comps.oversizedbigchoppa",
        // Adeptus Mechanicus, not sure what
        "com.ogliss.rimworld.mod.AdeptusMechanicus",
        // Faction Colors, not sure what
        "rimworld.ohu.factionColors.main",
        // Enable oversized weapons
        "rimworld.carnysenpai.enableoversizedweapons",
        // Modifies weapon position
        "com.github.automatic1111.gunplay",
        // Red Scare Framework
        // Modifies weapon position, not sure why
        "Chismar.RedScare",
        // [O21] Toolbox
        // Modifies weapon position for lasers
        "com.o21toolbox.rimworld.mod",
        // Rimlaser
        // Modifies weapon position for lasers
        "com.github.automatic1111.rimlaser",
        // Combat extended
        "CombatExtended.HarmonyCE",
        // Performance optimizer
        "PerformanceOptimizer.Main",
        // RIMMSqol
        "RIMMSqol",
        // Projectile position
        "Mlie.BetterProjectileOrigin",
        "Explorite.rimworld.mod.HarmonyPatches"
    };

    static ShowMeYourHandsMain()
    {
        DualWieldLoaded = ModLister.GetActiveModWithIdentifier("Roolo.DualWield") != null;
        YayoAdoptedLoaded = ModLister.GetActiveModWithIdentifier("com.yayo.combat3") != null;
        BabysAndChildrenLoaded = ModLister.GetActiveModWithIdentifier("babies.and.children.continued") != null;
        if (BabysAndChildrenLoaded)
        {
            var type = AccessTools.TypeByName("BabiesAndChildren.GraphicTools");
            if (type != null)
            {
                GetBodySizeScaling = type.GetMethod("GetBodySizeScaling");
            }

            LogMessage("BabiesAndChildren loaded, will compensate for children hand size");
        }

        OversizedWeaponLoaded = AccessTools.TypeByName("CompOversizedWeapon") != null;
        EnableOversizedLoaded = ModLister.GetActiveModWithIdentifier("CarnySenpai.EnableOversizedWeapons") != null;

        if (OversizedWeaponLoaded || EnableOversizedLoaded)
        {
            var allWeapons = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon).ToList();
            foreach (var weapon in allWeapons)
            {
                saveWeaponOffsets(weapon);
            }

            LogMessage(
                $"OversizedWeapon loaded, will compensate positioning. Cached offsets for {allWeapons.Count} weapons");
        }

        var compProperties = new CompProperties { compClass = typeof(HandDrawer) };
        foreach (var thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                 where race.race?.Humanlike == true
                 select race)
        {
            thingDef.comps?.Add(compProperties);
        }

        HandDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Hand");

        var partsHediffs =
            DefDatabase<HediffDef>.AllDefsListForReading.Where(def =>
                def.hediffClass == typeof(Hediff_AddedPart) && def.spawnThingOnRemoved != null);
        HediffColors = new Dictionary<HediffDef, Color>();
        foreach (var partsHediff in partsHediffs)
        {
            var techLevel = partsHediff.spawnThingOnRemoved.techLevel;
            HediffColors[partsHediff] = GetColorFromTechLevel(techLevel);
        }

        LogMessage($"Cached {HediffColors.Count} hediffs colors");

        IsColorable = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.HasComp(typeof(CompColorable)))
            .ToList();

        harmony = new Harmony("Mlie.ShowMeYourHands");

        harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (ModLister.GetActiveModWithIdentifier("MalteSchulze.RIMMSqol") == null)
        {
            return;
        }

        LogMessage(
            "RIMMSqol loaded, will remove their destructive Prefixes for the rotation-methods");
        var original = typeof(Pawn_RotationTracker).GetMethod("FaceCell");
        harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");
        original = typeof(Pawn_RotationTracker).GetMethod("Face");
        harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");
    }

    public static void LogMessage(string message, bool forced = false, bool warning = false)
    {
        if (warning)
        {
            Log.Warning($"[ShowMeYourHands]: {message}");
            return;
        }

        if (!forced && !ShowMeYourHandsMod.instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[ShowMeYourHands]: {message}");
    }


    private static Color GetColorFromTechLevel(TechLevel techLevel)
    {
        switch (techLevel)
        {
            case TechLevel.Neolithic:
                return ThingDefOf.WoodLog.stuffProps.color;
            case TechLevel.Industrial:
                return ThingDefOf.Steel.stuffProps.color;
            case TechLevel.Spacer:
                return ThingDefOf.Silver.stuffProps.color;
            case TechLevel.Ultra:
                return ThingDefOf.Gold.stuffProps.color;
            case TechLevel.Archotech:
                return ThingDefOf.Plasteel.stuffProps.color;
            default:
                return ThingDefOf.Steel.stuffProps.color;
        }
    }


    private static void saveWeaponOffsets(ThingDef weapon)
    {
        if (OversizedWeaponLoaded)
        {
            var thingComp =
                weapon.comps.FirstOrDefault(y => y.GetType().ToString().Contains("CompOversizedWeapon"));
            if (thingComp == null)
            {
                return;
            }

            var oversizedType = thingComp.GetType();
            var fields = oversizedType.GetFields().Where(info => info.Name.Contains("Offset"));

            foreach (var fieldInfo in fields)
            {
                switch (fieldInfo.Name)
                {
                    case "northOffset":
                        northOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "southOffset":
                        southOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "westOffset":
                        westOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                    case "eastOffset":
                        eastOffsets[weapon] = fieldInfo.GetValue(thingComp) is Vector3
                            ? (Vector3)fieldInfo.GetValue(thingComp)
                            : Vector3.zero;
                        break;
                }
            }

            return;
        }

        if (!EnableOversizedLoaded)
        {
            return;
        }

        if (weapon.graphicData == null)
        {
            return;
        }

        var graphicData = weapon.graphicData;

        var baseOffset = graphicData.drawOffset;

        northOffsets[weapon] = graphicData.drawOffsetNorth ?? baseOffset;
        southOffsets[weapon] = graphicData.drawOffsetSouth ?? baseOffset;
        eastOffsets[weapon] = graphicData.drawOffsetEast ?? baseOffset;
        westOffsets[weapon] = graphicData.drawOffsetWest ?? baseOffset;
    }
}