using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WHands;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
public static class RimWorld_MainMenuDrawer_MainMenuOnGUI
{
    private static bool alreadyRun;

    private static List<ThingDef> doneWeapons = new List<ThingDef>();

    [HarmonyPostfix]
    public static void MainMenuOnGUI()
    {
        if (alreadyRun)
        {
            return;
        }

        alreadyRun = true;

        UpdateHandDefinitions();

        var original = typeof(PawnRenderer).GetMethod("DrawEquipmentAiming");
        var patches = Harmony.GetPatchInfo(original);
        var prefix = typeof(PawnRenderer_DrawEquipmentAiming).GetMethod("SaveWeaponLocation");
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
            ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, -1, null, new[] { patch.owner }));
        }

        ShowMeYourHandsMain.harmony.Patch(original, new HarmonyMethod(prefix, Priority.Last));

        patches = Harmony.GetPatchInfo(original);

        if (patches.Prefixes.Count > 0)
        {
            ShowMeYourHandsMain.LogMessage($"{patches.Prefixes.Count} current active prefixes");
            foreach (var patch in patches.Prefixes.OrderByDescending(patch => patch.priority))
            {
                if (ShowMeYourHandsMain.knownPatches.Contains(patch.owner))
                {
                    ShowMeYourHandsMain.LogMessage(
                        $"Prefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}");
                }
                else
                {
                    ShowMeYourHandsMain.LogMessage(
                        $"There is an unexpected patch of the weapon-rendering function. This may affect hand-positions. Please report the following information to the author of the 'Show Me Your Hands'-mod\nPrefix - Owner: {patch.owner}, Method: {patch.PatchMethod.Name}, Prio: {patch.priority}");
                }
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

    public static void UpdateHandDefinitions()
    {
        doneWeapons = new List<ThingDef>();
        var currentStage = "LoadFromSettings";

        try
        {
            LoadFromSettings();
            currentStage = "LoadFromDefs";
            LoadFromDefs();
            currentStage = "FigureOutTheRest";
            FigureOutTheRest();
        }
        catch (Exception exception)
        {
            ShowMeYourHandsMain.LogMessage(
                $"Failed to save some settings, if debugging the stage it failed was {currentStage}.\n{exception}");
        }

        ShowMeYourHandsMain.LogMessage($"Defined hand definitions of {doneWeapons.Count} weapons", true);
    }

    public static void FigureOutSpecific(ThingDef weapon)
    {
        var compProps = weapon.GetCompProperties<WhandCompProps>();
        if (compProps == null)
        {
            compProps = new WhandCompProps
            {
                compClass = typeof(WhandComp)
            };
            if (weapon.IsMeleeWeapon)
            {
                compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
            }
            else
            {
                compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                    ? secHand
                    : Vector3.zero;
                compProps.MainHand = mainHand;
            }

            weapon.comps.Add(compProps);
        }
        else
        {
            if (weapon.IsMeleeWeapon)
            {
                compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
            }
            else
            {
                compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                    ? secHand
                    : Vector3.zero;
                compProps.MainHand = mainHand;
            }
        }
    }

    private static void FigureOutTheRest()
    {
        foreach (var weapon in from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                 where weapon.IsWeapon && !weapon.destroyOnDrop &&
                       !doneWeapons.Contains(weapon)
                 select weapon)
        {
            if (ShowMeYourHandsMod.IsShield(weapon))
            {
                ShowMeYourHandsMain.LogMessage($"Ignoring {weapon.defName} is probably a shield");
                continue;
            }

            var compProps = weapon.GetCompProperties<WhandCompProps>();
            if (compProps == null)
            {
                compProps = new WhandCompProps
                {
                    compClass = typeof(WhandComp)
                };
                if (weapon.IsMeleeWeapon)
                {
                    compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                }
                else
                {
                    compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                        ? secHand
                        : Vector3.zero;
                    compProps.MainHand = mainHand;
                }

                weapon.comps.Add(compProps);
            }
            else
            {
                if (weapon.IsMeleeWeapon)
                {
                    compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                }
                else
                {
                    compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                        ? secHand
                        : Vector3.zero;
                    compProps.MainHand = mainHand;
                }
            }

            doneWeapons.Add(weapon);
        }
    }

    private static void LoadFromSettings()
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

            var compProps = weapon.GetCompProperties<WhandCompProps>();
            if (compProps == null)
            {
                compProps = new WhandCompProps

                {
                    compClass = typeof(WhandComp),
                    MainHand = keyValuePair.Value.ToVector3(),
                    SecHand =
                        ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions
                            .ContainsKey(keyValuePair.Key) == true
                            ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                                .ToVector3()
                            : Vector3.zero
                };
                weapon.comps.Add(compProps);
            }
            else
            {
                compProps.MainHand = keyValuePair.Value.ToVector3();
                compProps.SecHand =
                    ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions.ContainsKey(keyValuePair.Key) ==
                    true
                        ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key].ToVector3()
                        : Vector3.zero;
            }

            doneWeapons.Add(weapon);
        }
    }

    public static void LoadFromDefs(ThingDef specificDef = null)
    {
        var defs = DefDatabase<ClutterHandsTDef>.AllDefsListForReading;
        if (specificDef == null)
        {
            ShowMeYourHandsMod.DefinedByDef = new HashSet<string>();
        }

        foreach (var handsTDef in defs)
        {
            if (handsTDef.WeaponCompLoader.Count <= 0)
            {
                return;
            }

            foreach (var weaponSets in handsTDef.WeaponCompLoader)
            {
                if (weaponSets.ThingTargets.Count <= 0)
                {
                    continue;
                }

                foreach (var weaponDefName in weaponSets.ThingTargets)
                {
                    if (specificDef != null && weaponDefName != specificDef.defName)
                    {
                        continue;
                    }

                    var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
                    if (weapon == null)
                    {
                        continue;
                    }

                    if (specificDef == null && doneWeapons.Contains(weapon))
                    {
                        continue;
                    }

                    var compProps = weapon.GetCompProperties<WhandCompProps>();
                    if (compProps == null)
                    {
                        compProps = new WhandCompProps

                        {
                            compClass = typeof(WhandComp),
                            MainHand = weaponSets.MainHand,
                            SecHand = weaponSets.SecHand
                        };
                        weapon.comps.Add(compProps);
                    }
                    else
                    {
                        compProps.MainHand = weaponSets.MainHand;
                        compProps.SecHand = weaponSets.SecHand;
                    }

                    ShowMeYourHandsMod.DefinedByDef.Add(weapon.defName);
                    if (specificDef != null)
                    {
                        return;
                    }

                    doneWeapons.Add(weapon);
                }
            }
        }
    }

    private static bool IsWeaponLong(ThingDef weapon, out Vector3 mainHand, out Vector3 secHand)
    {
        var texture = weapon.graphicData.Graphic.MatSingle.mainTexture;

        // This is not allowed
        //var icon = (Texture2D) texture;

        // This is
        var renderTexture = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, renderTexture);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        var icon = new Texture2D(texture.width, texture.height);
        icon.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        icon.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);


        var pixels = icon.GetPixels32();
        var width = icon.width;
        var startPixel = width;
        var endPixel = 0;

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


        var percentWidth = (endPixel - startPixel) / (float)width;
        var percentStart = 0f;
        if (startPixel != 0)
        {
            percentStart = startPixel / (float)width;
        }

        var percentEnd = 0f;
        if (width - endPixel != 0)
        {
            percentEnd = (width - endPixel) / (float)width;
        }

        ShowMeYourHandsMain.LogMessage(
            $"{weapon.defName}: start {startPixel.ToString()}, percentstart {percentStart}, end {endPixel.ToString()}, percentend {percentEnd}, width {width}, percent {percentWidth}");

        if (percentWidth > 0.7f)
        {
            mainHand = new Vector3(-0.3f + percentStart, 0.3f, -0.05f);
            secHand = new Vector3(0.2f, -0.100f, -0.05f);
        }
        else
        {
            mainHand = new Vector3(-0.3f + percentStart, 0.3f, 0f);
            secHand = Vector3.zero;
        }

        return percentWidth > 0.7f;
    }
}