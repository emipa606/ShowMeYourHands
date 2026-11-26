using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WHands;

namespace ShowMeYourHands;

internal static class WeaponStyleHands
{
    private static readonly Dictionary<ThingDef, Dictionary<ThingStyleDef, WhandCompProps>> positions = new();
    private static readonly Dictionary<ThingDef, List<ThingStyleDef>> styles = new();
    private static readonly Dictionary<(ThingDef, ThingStyleDef), ClutterHandsTDef.CompTargets> definitions = new();
    private static readonly Dictionary<(ThingStyleDef, bool), ManualStyleHandEntry> generated = new();

    internal static string Key(ThingDef weapon, ThingStyleDef style)
    {
        return $"{weapon.defName}|{style.defName}";
    }

    internal static void Initialize()
    {
        positions.Clear();
        styles.Clear();
        definitions.Clear();

        foreach (var thingDefStyles in DefDatabase<StyleCategoryDef>.AllDefsListForReading.Select(category =>
                     category.thingDefStyles))
        {
            if (thingDefStyles == null)
            {
                continue;
            }

            foreach (var pair in thingDefStyles)
            {
                AddStyle(pair.ThingDef, pair.StyleDef);
            }
        }

        foreach (var weapon in DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon))
        {
            if (weapon.randomStyle == null)
            {
                continue;
            }

            foreach (var chance in weapon.randomStyle)
            {
                AddStyle(weapon, chance.StyleDef);
            }
        }

        foreach (var def in DefDatabase<ClutterHandsTDef>.AllDefsListForReading)
        {
            foreach (var entry in def.WeaponCompLoader)
            {
                foreach (var weaponName in entry.ThingTargets)
                {
                    var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(weaponName);
                    foreach (var styleName in entry.StyleTargets)
                    {
                        var style = DefDatabase<ThingStyleDef>.GetNamedSilentFail(styleName);
                        if (weapon == null || style?.Graphic == null || !weapon.IsWeapon)
                        {
                            continue;
                        }

                        AddStyle(weapon, style);
                        definitions.TryAdd((weapon, style), entry);
                    }
                }
            }
        }

        foreach (var key in ShowMeYourHandsMod.instance.Settings.ManualStyleHands.Keys)
        {
            var parts = key.Split('|');
            if (parts.Length == 2)
            {
                AddStyle(DefDatabase<ThingDef>.GetNamedSilentFail(parts[0]),
                    DefDatabase<ThingStyleDef>.GetNamedSilentFail(parts[1]));
            }
        }

        foreach (var pair in styles)
        {
            foreach (var style in pair.Value)
            {
                Get(pair.Key, style);
            }
        }
    }

    private static void AddStyle(ThingDef weapon, ThingStyleDef style)
    {
        if (weapon?.IsWeapon != true || style?.Graphic == null)
        {
            return;
        }

        if (!styles.TryGetValue(weapon, out var list))
        {
            list = [];
            styles.Add(weapon, list);
        }

        if (!list.Contains(style))
        {
            list.Add(style);
        }
    }

    internal static IReadOnlyList<ThingStyleDef> GetStyles(ThingDef weapon)
    {
        return styles.TryGetValue(weapon, out var list) ? list : Array.Empty<ThingStyleDef>();
    }

    internal static WhandCompProps Get(ThingDef weapon, ThingStyleDef style)
    {
        if (style?.Graphic == null)
        {
            return weapon.GetCompProperties<WhandCompProps>();
        }

        if (!positions.TryGetValue(weapon, out var byStyle))
        {
            byStyle = new Dictionary<ThingStyleDef, WhandCompProps>();
            positions.Add(weapon, byStyle);
        }

        if (byStyle.TryGetValue(style, out var result))
        {
            return result;
        }

        AddStyle(weapon, style);
        result = Create(weapon, style);
        byStyle.Add(style, result);

        return result;
    }

    private static WhandCompProps Create(ThingDef weapon, ThingStyleDef style)
    {
        if (ShowMeYourHandsMod.instance.Settings.ManualStyleHands.TryGetValue(Key(weapon, style), out var saved))
        {
            return saved.ToCompProps();
        }

        if (definitions.TryGetValue((weapon, style), out var defined))
        {
            return new WhandCompProps
            {
                MainHand = defined.MainHand, SecHand = defined.SecHand,
                MainRotation = defined.MainRotation, SecRotation = defined.SecRotation
            };
        }

        var generatedKey = (style, weapon.IsMeleeWeapon);
        if (generated.TryGetValue(generatedKey, out var calculated))
        {
            return calculated.ToCompProps();
        }

        calculated =
            new ManualStyleHandEntry(RimWorld_MainMenuDrawer_MainMenuOnGUI.CalculateStyleHands(weapon, style));
        generated.Add(generatedKey, calculated);

        return calculated.ToCompProps();
    }

    internal static void Reset(ThingDef weapon, ThingStyleDef style)
    {
        ShowMeYourHandsMod.instance.Settings.ManualStyleHands.Remove(Key(weapon, style));
        if (positions.TryGetValue(weapon, out var byStyle))
        {
            byStyle[style] = Create(weapon, style);
        }
    }

    internal static void ResetAll(ThingDef weapon)
    {
        foreach (var style in GetStyles(weapon))
        {
            Reset(weapon, style);
        }

        var prefix = weapon.defName + "|";
        var saved = ShowMeYourHandsMod.instance.Settings.ManualStyleHands;
        foreach (var key in saved.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
        {
            saved.Remove(key);
        }
    }
}

internal class ManualStyleHandEntry : IExposable
{
    public Vector3 MainHand;
    public float MainRotation;
    public Vector3 SecHand;
    public float SecRotation;

    public ManualStyleHandEntry()
    {
    }

    internal ManualStyleHandEntry(WhandCompProps props)
    {
        MainHand = props.MainHand;
        SecHand = props.SecHand;
        MainRotation = props.MainRotation;
        SecRotation = props.SecRotation;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref MainHand, "MainHand");
        Scribe_Values.Look(ref SecHand, "SecHand");
        Scribe_Values.Look(ref MainRotation, "MainRotation");
        Scribe_Values.Look(ref SecRotation, "SecRotation");
    }

    internal WhandCompProps ToCompProps()
    {
        return new WhandCompProps
        {
            MainHand = MainHand, SecHand = SecHand, MainRotation = MainRotation, SecRotation = SecRotation
        };
    }
}