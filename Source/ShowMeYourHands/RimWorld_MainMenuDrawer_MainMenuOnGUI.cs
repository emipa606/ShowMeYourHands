using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WHands;
using Object = UnityEngine.Object;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.MainMenuOnGUI))]
public static class RimWorld_MainMenuDrawer_MainMenuOnGUI
{
    private static bool alreadyRun;

    private static List<ThingDef> doneWeapons = [];
    private static int manuallyDefinedCount;
    private static int modDefinitionCount;
    private static int cachedPositionCount;
    private static int textureCalculatedCount;
    private static int defaultPositionCount;

    [HarmonyPostfix]
    public static void MainMenuOnGUI()
    {
        if (alreadyRun)
        {
            return;
        }

        alreadyRun = true;

        UpdateValidRaces();
        UpdateHandDefinitions();

        var original = typeof(PawnRenderUtility).GetMethod(nameof(PawnRenderUtility.DrawEquipmentAiming));
        var patches = Harmony.GetPatchInfo(original);
        var prefix =
            typeof(PawnRenderer_DrawEquipmentAiming).GetMethod(nameof(PawnRenderer_DrawEquipmentAiming
                .SaveWeaponLocation));
        if (patches is null)
        {
            ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, Priority.High));
            return;
        }

        var modifyingPatches = new List<string>
        {
            "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
            "com.github.automatic1111.gunplay",
            "com.o21toolbox.rimworld.mod",
            "com.github.automatic1111.rimlaser"
        };

        ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, Priority.First));

        foreach (var patch in patches.Prefixes.Where(patch => modifyingPatches.Contains(patch.owner)))
        {
            ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, -1, null, [patch.owner]));
        }

        ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, Priority.Last));

        patches = Harmony.GetPatchInfo(original);

        if (patches.Prefixes.Count > 0)
        {
            ShowMeYourHandsMain.LogMessage($"{patches.Prefixes.Count} current active prefixes");
            foreach (var patch in patches.Prefixes.OrderByDescending(patch => patch.priority))
            {
                ShowMeYourHandsMain.LogMessage(
                    ShowMeYourHandsMain.knownPatches.Contains(patch.owner)
                        ? $"Prefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}"
                        : $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nPrefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}");
            }
        }

        if (patches.Transpilers.Count <= 0)
        {
            return;
        }

        ShowMeYourHandsMain.LogMessage($"{patches.Transpilers.Count} current active transpilers");
        foreach (var patch in patches.Transpilers.OrderByDescending(patch => patch.priority))
        {
            if (ShowMeYourHandsMain.knownPatches.Contains(patch.owner))
            {
                ShowMeYourHandsMain.LogMessage(
                    $"Transpiler - Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}");
            }
            else
            {
                ShowMeYourHandsMain.LogMessage(
                    $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nTranspiler {patch.index}. Owner: {patch.owner}, Method: {patch.PatchMethod}, Prio: {patch.priority}",
                    false, true);
            }
        }
    }

    private static void UpdateValidRaces()
    {
        ShowMeYourHandsMod.instance.Settings.ShowOnRace ??= new Dictionary<string, bool>();
        var changed = false;
        foreach (var def in ShowMeYourHandsMain.allRaces)
        {
            if (ShowMeYourHandsMod.instance.Settings.ShowOnRace.ContainsKey(def.defName))
            {
                continue;
            }

            ShowMeYourHandsMod.instance.Settings.ShowOnRace[def.defName] = def.race.Humanlike;
            changed = true;
        }

        if (changed)
        {
            ShowMeYourHandsMod.instance.Settings.Write();
        }
    }

    public static void UpdateHandDefinitions()
    {
        doneWeapons = [];
        manuallyDefinedCount = 0;
        modDefinitionCount = 0;
        cachedPositionCount = 0;
        textureCalculatedCount = 0;
        defaultPositionCount = 0;
        var currentStage = "LoadFromSettings";

        try
        {
            loadFromSettings();
            currentStage = "LoadFromDefs";
            LoadFromDefs();
            currentStage = "FigureOutTheRest";
            figureOutTheRest();
        }
        catch (Exception exception)
        {
            ShowMeYourHandsMain.LogMessage(
                $"Failed to save some settings, if debugging the stage it failed was {currentStage}.\n{exception}");
        }

        ShowMeYourHandsMain.LogMessage($"Defined hand definitions of {doneWeapons.Count} weapons", true);
        ShowMeYourHandsMain.LogMessage(
            $"Hand-position sources: manual {manuallyDefinedCount}, mod definitions {modDefinitionCount}, cached {cachedPositionCount}, calculated from texture {textureCalculatedCount}, defaults without texture {defaultPositionCount}");
    }

    public static void FigureOutSpecific(ThingDef weapon)
    {
        var compProps = getOrCreateCompProps(weapon);
        applyDefaultWeaponHands(weapon, compProps);
        resetHandRotations(compProps);
    }

    private static void figureOutTheRest()
    {
        var settings = ShowMeYourHandsMod.instance.Settings;
        settings.GeneratedHandPositions ??= new Dictionary<string, GeneratedHandPositionCacheEntry>();
        var modAboutFileTimes = new Dictionary<ModContentPack, long?>();
        var cacheChanged = false;

        foreach (var weapon in from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                 where weapon.IsWeapon && !doneWeapons.Contains(weapon)
                 select weapon)
        {
            if (ShowMeYourHandsMod.IsShield(weapon))
            {
                ShowMeYourHandsMain.LogMessage($"Ignoring {weapon.defName} is probably a shield");
                continue;
            }

            var hasAboutFileTime = tryGetAboutFileLastWriteTime(weapon, modAboutFileTimes, out var modPackageId,
                out var aboutFileLastWriteTimeUtcTicks);
            if (hasAboutFileTime && settings.GeneratedHandPositions.TryGetValue(weapon.defName, out var cached) &&
                cached.ModPackageId == modPackageId &&
                cached.AboutFileLastWriteTimeUtcTicks == aboutFileLastWriteTimeUtcTicks)
            {
                var cachedCompProps = getOrCreateCompProps(weapon);
                cachedCompProps.MainHand = cached.MainHand;
                cachedCompProps.SecHand = cached.SecHand;
                cachedCompProps.MainRotation = cached.MainRotation;
                cachedCompProps.SecRotation = cached.SecRotation;
                cachedPositionCount++;
                doneWeapons.Add(weapon);
                continue;
            }

            var compProps = getOrCreateCompProps(weapon);
            if (applyDefaultWeaponHands(weapon, compProps))
            {
                textureCalculatedCount++;
            }
            else
            {
                defaultPositionCount++;
            }

            resetHandRotations(compProps);

            if (hasAboutFileTime)
            {
                settings.GeneratedHandPositions[weapon.defName] = new GeneratedHandPositionCacheEntry
                {
                    MainHand = compProps.MainHand,
                    SecHand = compProps.SecHand,
                    MainRotation = compProps.MainRotation,
                    SecRotation = compProps.SecRotation,
                    ModPackageId = modPackageId,
                    AboutFileLastWriteTimeUtcTicks = aboutFileLastWriteTimeUtcTicks
                };
                cacheChanged = true;
            }
            else if (settings.GeneratedHandPositions.Remove(weapon.defName))
            {
                cacheChanged = true;
            }

            doneWeapons.Add(weapon);
        }

        if (cacheChanged)
        {
            settings.Write();
        }
    }

    private static bool tryGetAboutFileLastWriteTime(ThingDef weapon,
        Dictionary<ModContentPack, long?> modAboutFileTimes, out string modPackageId,
        out long aboutFileLastWriteTimeUtcTicks)
    {
        var modContentPack = weapon.modContentPack;
        modPackageId = modContentPack?.PackageId;
        aboutFileLastWriteTimeUtcTicks = 0;
        if (modContentPack == null || string.IsNullOrEmpty(modPackageId))
        {
            return false;
        }

        if (!modAboutFileTimes.TryGetValue(modContentPack, out var lastWriteTimeUtcTicks))
        {
            var aboutFilePath = Path.Combine(modContentPack.RootDir, "About", "About.xml");
            if (!File.Exists(aboutFilePath))
            {
                Log.WarningOnce(
                    $"[ShowMeYourHands]: Could not find About-file for '{modContentPack.Name}'; hand-position cache will be skipped.",
                    modContentPack.GetHashCode());
                lastWriteTimeUtcTicks = null;
            }
            else
            {
                try
                {
                    lastWriteTimeUtcTicks = File.GetLastWriteTimeUtc(aboutFilePath).Ticks;
                }
                catch (Exception exception)
                {
                    Log.WarningOnce(
                        $"[ShowMeYourHands]: Could not read About-file timestamp for '{modContentPack.Name}'; hand-position cache will be skipped. {exception.Message}",
                        modContentPack.GetHashCode());
                    lastWriteTimeUtcTicks = null;
                }
            }

            modAboutFileTimes[modContentPack] = lastWriteTimeUtcTicks;
        }

        if (!lastWriteTimeUtcTicks.HasValue)
        {
            return false;
        }

        aboutFileLastWriteTimeUtcTicks = lastWriteTimeUtcTicks.Value;
        return true;
    }

    private static WhandCompProps getOrCreateCompProps(ThingDef weapon)
    {
        var compProps = weapon.GetCompProperties<WhandCompProps>();
        if (compProps != null)
        {
            return compProps;
        }

        compProps = new WhandCompProps
        {
            compClass = typeof(WhandComp)
        };
        weapon.comps.Add(compProps);
        return compProps;
    }

    private static bool applyDefaultWeaponHands(ThingDef weapon, WhandCompProps compProps)
    {
        if (weapon.IsMeleeWeapon)
        {
            compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
            return false;
        }

        var weaponHasLongGraphic = isWeaponLong(weapon, out var mainHand, out var secHand, out var usedTexture);
        compProps.SecHand = weaponHasLongGraphic
            ? secHand
            : Vector3.zero;
        compProps.MainHand = mainHand;
        return usedTexture;
    }

    private static void resetHandRotations(WhandCompProps compProps)
    {
        compProps.MainRotation = 0;
        compProps.SecRotation = 0;
    }

    private static void loadFromSettings()
    {
        if (ShowMeYourHandsMod.instance.Settings.ManualMainHandPositions == null)
        {
            return;
        }

        foreach (var keyValuePair in ShowMeYourHandsMod.instance.Settings.ManualMainHandPositions)
        {
            var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(keyValuePair.Key);
            if (weapon == null)
            {
                continue;
            }

            var compProps = getOrCreateCompProps(weapon);
            compProps.MainHand = keyValuePair.Value.ToVector3();
            compProps.SecHand = getManualOffHandPosition(keyValuePair.Key);
            compProps.MainRotation = getManualMainHandRotation(keyValuePair.Key);
            compProps.SecRotation = getManualOffHandRotation(keyValuePair.Key);

            doneWeapons.Add(weapon);
            manuallyDefinedCount++;
        }
    }

    private static Vector3 getManualOffHandPosition(string defName)
    {
        return ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions?.TryGetValue(defName, out var value) ==
               true
            ? value.ToVector3()
            : Vector3.zero;
    }

    private static float getManualMainHandRotation(string defName)
    {
        return ShowMeYourHandsMod.instance?.Settings?.ManualMainHandRotations?.TryGetValue(defName, out var value) ==
               true
            ? value
            : 0f;
    }

    private static float getManualOffHandRotation(string defName)
    {
        return ShowMeYourHandsMod.instance?.Settings?.ManualOffHandRotations?.TryGetValue(defName, out var value) ==
               true
            ? value
            : 0f;
    }

    public static void LoadFromDefs(ThingDef specificDef = null)
    {
        var defs = DefDatabase<ClutterHandsTDef>.AllDefsListForReading;
        ShowMeYourHandsMod.definedByDef ??= [];

        foreach (var handsComp in defs.Select(hands => hands.WeaponCompLoader))
        {
            if (handsComp.Count <= 0)
            {
                return;
            }

            foreach (var weaponSets in handsComp)
            {
                if (weaponSets.ThingTargets.Count <= 0)
                {
                    continue;
                }

                if (applyWeaponSetsFromDefs(weaponSets, specificDef))
                {
                    return;
                }
            }
        }
    }

    private static bool applyWeaponSetsFromDefs(ClutterHandsTDef.CompTargets weaponSets, ThingDef specificDef)
    {
        foreach (var weaponDefName in weaponSets.ThingTargets)
        {
            if (specificDef != null && weaponDefName != specificDef.defName)
            {
                continue;
            }

            var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
            if (weapon == null || specificDef == null && doneWeapons.Contains(weapon))
            {
                continue;
            }

            applyWeaponSetToCompProps(weapon, weaponSets);
            ShowMeYourHandsMod.definedByDef.Add(weapon.defName);
            if (specificDef != null)
            {
                return true;
            }

            doneWeapons.Add(weapon);
            modDefinitionCount++;
        }

        return false;
    }

    private static void applyWeaponSetToCompProps(ThingDef weapon, ClutterHandsTDef.CompTargets weaponSets)
    {
        var compProps = getOrCreateCompProps(weapon);
        compProps.MainHand = weaponSets.MainHand;
        compProps.SecHand = weaponSets.SecHand;
        compProps.MainRotation = weaponSets.MainRotation;
        compProps.SecRotation = weaponSets.SecRotation;
    }

    private static bool isWeaponLong(ThingDef weapon, out Vector3 mainHand, out Vector3 secHand,
        out bool usedTexture)
    {
        var texture = weapon.graphicData.Graphic?.MatSingle.mainTexture;
        usedTexture = texture != null;
        if (texture == null)
        {
            mainHand = new Vector3(-0.3f, 0.3f, 0f);
            secHand = Vector3.zero;
            return false;
        }

        var renderTexture = new RenderTexture(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        var previous = RenderTexture.active;
        Texture2D icon = null;

        try
        {
            icon = createReadableTexture(texture, renderTexture);
            calculateTextureBounds(icon, out var startPixel, out var endPixel);
            var width = icon.width;
            var percentWidth = (endPixel - startPixel) / (float)width;
            var percentStart = startPixel != 0 ? startPixel / (float)width : 0f;
            var percentEnd = width - endPixel != 0 ? (width - endPixel) / (float)width : 0f;

            ShowMeYourHandsMain.LogMessage(
                $"{weapon.defName}: start {startPixel.ToString()}, percentstart {percentStart}, end {endPixel.ToString()}, percentend {percentEnd}, width {width}, percent {percentWidth}");

            return setHandPositionsFromWidth(percentWidth, percentStart, out mainHand, out secHand);
        }
        finally
        {
            RenderTexture.active = previous;
            renderTexture.Release();
            Object.Destroy(renderTexture);

            if (icon != null)
            {
                Object.Destroy(icon);
            }
        }
    }

    private static Texture2D createReadableTexture(Texture texture, RenderTexture renderTexture)
    {
        Graphics.Blit(texture, renderTexture);
        RenderTexture.active = renderTexture;

        var icon = new Texture2D(texture.width, texture.height);
        Texture2D result;
        try
        {
            icon.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            icon.Apply();
            result = icon;
        }
        catch
        {
            Object.Destroy(icon);
            throw;
        }

        return result;
    }

    private static void calculateTextureBounds(Texture2D icon, out int startPixel, out int endPixel)
    {
        var pixels = icon.GetPixels32();
        var width = icon.width;
        startPixel = width;
        endPixel = 0;

        for (var i = 0; i < icon.height; i++)
        {
            for (var j = 0; j < startPixel; j++)
            {
                if (pixels[j + (i * width)].a < 5)
                {
                    continue;
                }

                startPixel = j;
                break;
            }

            for (var j = width - 1; j >= endPixel; j--)
            {
                if (pixels[j + (i * width)].a < 5)
                {
                    continue;
                }

                endPixel = j;
                break;
            }
        }
    }

    private static bool setHandPositionsFromWidth(float percentWidth, float percentStart, out Vector3 mainHand,
        out Vector3 secHand)
    {
        if (percentWidth > 0.7f)
        {
            mainHand = new Vector3(-0.3f + percentStart, 0.3f, -0.05f);
            secHand = new Vector3(0.2f, -0.100f, -0.05f);
            return true;
        }

        mainHand = new Vector3(-0.3f + percentStart, 0.3f, 0f);
        secHand = Vector3.zero;
        return false;
    }
}