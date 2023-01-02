using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
internal class ShowMeYourHandsMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ShowMeYourHandsMod instance;

    private static readonly Vector2 buttonSize = new Vector2(120f, 25f);

    private static readonly Vector2 weaponSize = new Vector2(200f, 200f);

    private static readonly Vector2 iconSize = new Vector2(24f, 24f);

    private static readonly Vector2 handSize = new Vector2(43f, 43f);

    private static readonly int buttonSpacer = 200;

    private static readonly float columnSpacer = 0.1f;

    private static float leftSideWidth;

    private static Listing_Standard listing_Standard;

    private static Vector3 currentMainHand;

    private static Vector3 currentOffHand;

    private static bool currentHasOffHand;

    private static bool currentNoHands;

    private static bool currentMainBehind;

    private static bool currentOffBehind;

    private static Vector2 tabsScrollPosition;

    private static Vector2 summaryScrollPosition;

    private static List<ThingDef> allWeapons;

    private static List<string> selectedHasManualDefs;

    private static string currentVersion;

    private static Graphic handTex;

    private static Dictionary<string, int> totalWeaponsByMod = new Dictionary<string, int>();

    private static Dictionary<string, int> fixedWeaponsByMod = new Dictionary<string, int>();

    public static HashSet<string> DefinedByDef;

    private static string selectedDef = "Settings";

    private static string selectedSubDef;


    /// <summary>
    ///     The private settings
    /// </summary>
    private ShowMeYourHandsModSettings settings;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ShowMeYourHandsMod(ModContentPack content)
        : base(content)
    {
        instance = this;
        ParseHelper.Parsers<SaveableVector3>.Register(SaveableVector3.FromString);
        if (instance.Settings.ManualMainHandPositions == null)
        {
            instance.Settings.ManualMainHandPositions = new Dictionary<string, SaveableVector3>();
            instance.Settings.ManualOffHandPositions = new Dictionary<string, SaveableVector3>();
        }

        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.ShowMeYourHands"));
    }

    private static string SelectedDef
    {
        get => selectedDef;
        set
        {
            if (value == "Settings")
            {
                UpdateWeaponStatistics();
            }

            selectedDef = value;
        }
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ShowMeYourHandsModSettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = GetSettings<ShowMeYourHandsModSettings>();
            }

            return settings;
        }

        set => settings = value;
    }

    private static List<ThingDef> AllWeapons
    {
        get
        {
            if (allWeapons == null || allWeapons.Count == 0)
            {
                allWeapons = (from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                    where weapon.IsWeapon && !weapon.destroyOnDrop && !IsShield(weapon)
                    orderby weapon.label
                    select weapon).ToList();
            }

            return allWeapons;
        }
        set => allWeapons = value;
    }

    private Graphic HandTex
    {
        get
        {
            if (handTex == null)
            {
                handTex = GraphicDatabase.Get<Graphic_Multi>("HandIcon", ShaderDatabase.CutoutSkin,
                    new Vector2(1f, 1f),
                    PawnSkinColors.GetSkinColor(0.5f), PawnSkinColors.GetSkinColor(0.5f));
            }

            return handTex;
        }
        set => handTex = value;
    }

    /// <summary>
    ///     The settings-window
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        base.DoSettingsWindowContents(rect);

        var rect2 = rect.ContractedBy(1);
        leftSideWidth = rect2.ContractedBy(10).width / 5 * 2;

        listing_Standard = new Listing_Standard();

        DrawOptions(rect2);
        DrawTabsList(rect2);
        Settings.Write();
    }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "Show Me Your Hands";
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
    }

    public static bool IsShield(ThingDef weapon)
    {
        var isShield = false;
        if (weapon.weaponTags == null)
        {
            return false;
        }

        foreach (var tag in weapon.weaponTags)
        {
            switch (tag)
            {
                case "Shield_Sidearm":
                case "Shield_NoSidearm":
                    continue;
            }

            if (tag.Contains("_ValidSidearm"))
            {
                continue;
            }

            if (tag.Contains("ShieldSafe"))
            {
                continue;
            }

            if (!tag.ToLower().Contains("shield"))
            {
                continue;
            }

            isShield = true;
        }

        return isShield;
    }

    private static void DrawButton(Action action, string text, Vector2 pos)
    {
        var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
        if (!Widgets.ButtonText(rect, text, true, false, Color.white))
        {
            return;
        }

        SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
        action();
    }

    private static void UpdateWeaponStatistics()
    {
        totalWeaponsByMod = new Dictionary<string, int>();
        fixedWeaponsByMod = new Dictionary<string, int>();
        foreach (var currentWeapon in AllWeapons)
        {
            var weaponModName = currentWeapon.modContentPack?.Name;
            if (string.IsNullOrEmpty(weaponModName))
            {
                weaponModName = "SMYH.unknown".Translate();
            }

            if (totalWeaponsByMod.ContainsKey(weaponModName))
            {
                totalWeaponsByMod[weaponModName]++;
            }
            else
            {
                totalWeaponsByMod[weaponModName] = 1;
            }

            if (!DefinedByDef.Contains(currentWeapon.defName) &&
                !instance.Settings.ManualMainHandPositions.ContainsKey(currentWeapon.defName))
            {
                continue;
            }

            if (fixedWeaponsByMod.ContainsKey(weaponModName))
            {
                fixedWeaponsByMod[weaponModName]++;
            }
            else
            {
                fixedWeaponsByMod[weaponModName] = 1;
            }
        }
    }

    private bool DrawIcon(ThingDef thing, Rect rect, Vector3 mainHandPosition, Vector3 offHandPosition)
    {
        if (thing == null)
        {
            return false;
        }

        var texture = thing.graphicData?.Graphic?.MatSingle?.mainTexture;
        if (thing.graphicData?.graphicClass == typeof(Graphic_Random))
        {
            texture = ((Graphic_Random)thing.graphicData.Graphic)?.FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thing.graphicData?.graphicClass == typeof(Graphic_StackCount))
        {
            texture = ((Graphic_StackCount)thing.graphicData.Graphic)?.SubGraphicForStackCount(1, thing).MatSingle
                .mainTexture;
        }

        if (texture == null)
        {
            return false;
        }

        var rectOuter = rect.ExpandedBy(5);
        var rectLine = rect.ExpandedBy(2);
        Widgets.DrawBoxSolid(rectOuter, Color.grey);
        Widgets.DrawBoxSolid(rectLine, new ColorInt(42, 43, 44).ToColor);
        const int handPositionFactor = 200;
        var weaponMiddle = weaponSize.x / 2;

        var mainHandCoords = new Vector2(
            weaponMiddle + (mainHandPosition.x * handPositionFactor) - (handSize.x / 2),
            weaponMiddle - (mainHandPosition.z * handPositionFactor) - (handSize.y / 2));
        var offHandCoords = new Vector2(
            weaponMiddle + (offHandPosition.x * handPositionFactor) - (handSize.x / 2),
            weaponMiddle - (offHandPosition.z * handPositionFactor) - (handSize.y / 2));

        var mainHandRect = new Rect(rect.x + mainHandCoords.x, rect.y + mainHandCoords.y,
            handSize.x,
            handSize.y);
        var offHandRect = new Rect(rect.x + offHandCoords.x, rect.y + offHandCoords.y,
            handSize.x,
            handSize.y);

        if (!currentNoHands)
        {
            if (currentMainBehind)
            {
                GUI.DrawTexture(mainHandRect, HandTex.MatEast.mainTexture);
            }

            if (currentHasOffHand && currentOffBehind)
            {
                GUI.DrawTexture(offHandRect, HandTex.MatEast.mainTexture);
            }
        }

        if (thing.IsRangedWeapon)
        {
            DrawTextureRotatedLocal(rect, texture,
                thing.equippedAngleOffset);
        }
        else
        {
            GUI.DrawTexture(rect, texture);
        }

        if (currentNoHands)
        {
            return true;
        }

        if (!currentMainBehind)
        {
            GUI.DrawTexture(mainHandRect, HandTex.MatSouth.mainTexture);
        }

        if (currentHasOffHand && !currentOffBehind)
        {
            GUI.DrawTexture(offHandRect, HandTex.MatSouth.mainTexture);
        }

        return true;
    }

    private void DrawTextureRotatedLocal(Rect rect, Texture texture, float angle)
    {
        if (angle == 0f)
        {
            GUI.DrawTexture(rect, texture);
            return;
        }

        var matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, rect.center);
        GUI.DrawTexture(rect, texture);
        GUI.matrix = matrix;
    }

    private void DrawWeapon(ThingDef thing, Rect rect)
    {
        if (thing?.graphicData?.Graphic?.MatSingle?.mainTexture == null)
        {
            return;
        }

        var texture2D = thing.graphicData.Graphic.MatSingle.mainTexture;
        if (thing.graphicData.graphicClass == typeof(Graphic_Random))
        {
            texture2D = ((Graphic_Random)thing.graphicData.Graphic).FirstSubgraphic().MatSingle.mainTexture;
        }

        if (thing.graphicData.graphicClass == typeof(Graphic_StackCount))
        {
            texture2D = ((Graphic_StackCount)thing.graphicData.Graphic).SubGraphicForStackCount(1, thing).MatSingle
                .mainTexture;
        }

        if (texture2D.width != texture2D.height)
        {
            var ratio = (float)texture2D.width / texture2D.height;

            if (ratio < 1)
            {
                rect.x += (rect.width - (rect.width * ratio)) / 2;
                rect.width *= ratio;
            }
            else
            {
                rect.y += (rect.height - (rect.height / ratio)) / 2;
                rect.height /= ratio;
            }
        }

        GUI.DrawTexture(rect, texture2D);
    }

    private void DrawOptions(Rect rect)
    {
        var optionsOuterContainer = rect.ContractedBy(10);
        optionsOuterContainer.x += leftSideWidth + columnSpacer;
        optionsOuterContainer.width -= leftSideWidth + columnSpacer;
        Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
        var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = optionsInnerContainer.ContractedBy(10);
        frameRect.x = leftSideWidth + columnSpacer + 20;
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;

        switch (SelectedDef)
        {
            case null:
                return;
            case "Settings":
            {
                listing_Standard.Begin(frameRect);
                Text.Font = GameFont.Medium;
                listing_Standard.Label("SMYH.settings".Translate());
                Text.Font = GameFont.Small;
                listing_Standard.Gap();
                if (Prefs.UIScale != 1f)
                {
                    GUI.color = Color.yellow;
                    listing_Standard.Label(
                        "SMYH.uiscale.label".Translate(),
                        -1F,
                        "SMYH.uiscale.tooltip".Translate());
                    listing_Standard.Gap();
                    GUI.color = Color.white;
                }

                if (instance.Settings.ManualMainHandPositions?.Count > 0)
                {
                    var copyPoint = listing_Standard.Label("SMYH.copy.label".Translate(), -1F,
                        "SMYH.copy.tooltip".Translate());
                    DrawButton(() => { CopyChangedWeapons(); }, "SMYH.copy.button".Translate(),
                        new Vector2(copyPoint.position.x + buttonSpacer, copyPoint.position.y));
                    listing_Standard.Gap();
                    var labelPoint = listing_Standard.Label("SMYH.resetall.label".Translate(), -1F,
                        "SMYH.resetall.tooltip".Translate());
                    DrawButton(() =>
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                "SMYH.resetall.confirm".Translate(),
                                delegate
                                {
                                    instance.Settings.ResetManualValues();
                                    UpdateWeaponStatistics();
                                }));
                        }, "SMYH.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                    if (!string.IsNullOrEmpty(selectedSubDef) && selectedHasManualDefs.Count > 0)
                    {
                        DrawButton(() => { CopyChangedWeapons(true); }, "SMYH.copyselected.button".Translate(),
                            new Vector2(copyPoint.position.x + buttonSpacer + buttonSize.x + 10,
                                copyPoint.position.y));
                        DrawButton(() =>
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "SMYH.resetselected.confirm".Translate(selectedSubDef),
                                    delegate
                                    {
                                        foreach (var weaponDef in from ThingDef weapon in AllWeapons
                                                 where
                                                     weapon.modContentPack == null &&
                                                     selectedSubDef == "SMYH.unknown".Translate() ||
                                                     weapon.modContentPack?.Name == selectedSubDef
                                                 select weapon)
                                        {
                                            WhandCompProps whandCompProps = null;
                                            ResetOneWeapon(weaponDef, ref whandCompProps);
                                        }

                                        selectedHasManualDefs = new List<string>();
                                        UpdateWeaponStatistics();
                                    }));
                            }, "SMYH.resetselected.button".Translate(),
                            new Vector2(labelPoint.position.x + buttonSpacer + buttonSize.x + 10,
                                labelPoint.position.y));
                    }
                }
                else
                {
                    listing_Standard.Gap((buttonSize.y * 2) + 12);
                }

                listing_Standard.CheckboxLabeled("SMYH.logging.label".Translate(), ref Settings.VerboseLogging,
                    "SMYH.logging.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.matcharmor.label".Translate(), ref Settings.MatchArmorColor,
                    "SMYH.matcharmor.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.matchartificiallimb.label".Translate(),
                    ref Settings.MatchArtificialLimbColor,
                    "SMYH.matchartificiallimb.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.matchhandamounts.label".Translate(),
                    ref Settings.MatchHandAmounts,
                    "SMYH.matchhandamounts.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.resizehands.label".Translate(), ref Settings.ResizeHands,
                    "SMYH.resizehands.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.repositionhands.label".Translate(),
                    ref Settings.RepositionHands,
                    "SMYH.repositionhands.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.showwhencarry.label".Translate(),
                    ref Settings.ShowWhenCarry,
                    "SMYH.showwhencarry.tooltip".Translate());
                listing_Standard.CheckboxLabeled("SMYH.showothertimes.label".Translate(),
                    ref Settings.ShowOtherTmes,
                    "SMYH.showothertimes.tooltip".Translate());
                if (currentVersion != null)
                {
                    listing_Standard.Gap();
                    GUI.contentColor = Color.gray;
                    listing_Standard.Label("SMYH.version.label".Translate(currentVersion));
                    GUI.contentColor = Color.white;
                }

                listing_Standard.GapLine();
                Text.Font = GameFont.Medium;
                listing_Standard.Label("SMYH.summary".Translate(), -1F, "SMYH.summary.tooltip".Translate());
                Text.Font = GameFont.Small;
                listing_Standard.Gap();
                listing_Standard.End();

                var tabFrameRect = frameRect;
                tabFrameRect.y += 375;
                tabFrameRect.height -= 375;
                var tabContentRect = tabFrameRect;
                tabContentRect.x = 0;
                tabContentRect.y = 0;
                if (totalWeaponsByMod.Count == 0)
                {
                    UpdateWeaponStatistics();
                }

                tabContentRect.height = (totalWeaponsByMod.Count * 25f) + 15;
                Widgets.BeginScrollView(tabFrameRect, ref summaryScrollPosition, tabContentRect);
                listing_Standard.Begin(tabContentRect);
                foreach (var keyValuePair in totalWeaponsByMod)
                {
                    var fixedWeapons = 0;
                    if (fixedWeaponsByMod.ContainsKey(keyValuePair.Key))
                    {
                        fixedWeapons = fixedWeaponsByMod[keyValuePair.Key];
                    }

                    var percent = fixedWeapons / (decimal)keyValuePair.Value * 100;

                    GUI.color = GetColorFromPercent(percent);

                    if (listing_Standard.ListItemSelectable(
                            $"{keyValuePair.Key} {Math.Round(percent)}% ({fixedWeapons}/{keyValuePair.Value})",
                            Color.yellow,
                            out _,
                            selectedSubDef == keyValuePair.Key))
                    {
                        selectedSubDef = selectedSubDef == keyValuePair.Key ? null : keyValuePair.Key;
                    }

                    GUI.color = Color.white;
                }

                listing_Standard.End();

                Widgets.EndScrollView();
                break;
            }

            default:
            {
                var currentDef = DefDatabase<ThingDef>.GetNamedSilentFail(SelectedDef);
                listing_Standard.Begin(frameRect);
                if (currentDef == null)
                {
                    listing_Standard.Label("SMYH.error.weapon".Translate(SelectedDef));
                    listing_Standard.End();
                    break;
                }

                var compProperties = currentDef.GetCompProperties<WhandCompProps>();
                if (compProperties == null)
                {
                    listing_Standard.Label("SMYH.error.hands".Translate(SelectedDef));
                    listing_Standard.End();
                    break;
                }

                Text.Font = GameFont.Medium;
                var labelPoint = listing_Standard.Label(currentDef.label.CapitalizeFirst(), -1F,
                    currentDef.defName);
                Text.Font = GameFont.Small;
                var modName = currentDef.modContentPack?.Name;
                var modId = currentDef.modContentPack?.PackageId;
                if (currentDef.modContentPack != null)
                {
                    listing_Standard.Label($"{modName}", -1F, modId);
                }
                else
                {
                    listing_Standard.Gap();
                }

                var description = currentDef.description;
                if (!string.IsNullOrEmpty(description))
                {
                    if (description.Length > 250)
                    {
                        description = $"{description.Substring(0, 250)}...";
                    }

                    Widgets.Label(new Rect(labelPoint.x, labelPoint.y + 50, 250, 150), description);
                }

                listing_Standard.Gap(150);

                var weaponRect = new Rect(labelPoint.x + 270, labelPoint.y + 5, weaponSize.x,
                    weaponSize.y);

                if (currentMainHand == Vector3.zero && !currentNoHands)
                {
                    currentMainHand = compProperties.MainHand;
                    currentOffHand = compProperties.SecHand;
                    currentHasOffHand = currentOffHand != Vector3.zero;
                    currentMainBehind = compProperties.MainHand.y < 0;
                    currentOffBehind = compProperties.SecHand.y < 0 || currentOffHand == Vector3.zero;
                }

                currentNoHands = currentMainHand == Vector3.zero;

                if (!DrawIcon(currentDef, weaponRect, currentMainHand, currentOffHand))
                {
                    listing_Standard.Label("SMYH.error.texture".Translate(SelectedDef));
                    listing_Standard.End();
                    break;
                }


                listing_Standard.Gap(20);
                listing_Standard.CheckboxLabeled("SMYH.twohands.label".Translate(), ref currentHasOffHand);
                if (currentHasOffHand)
                {
                    currentNoHands = false;
                }

                listing_Standard.CheckboxLabeled("SMYH.nohands.label".Translate(), ref currentNoHands);
                if (currentNoHands)
                {
                    currentHasOffHand = false;
                    currentMainHand = Vector3.zero;
                    currentOffHand = Vector3.zero;
                }

                listing_Standard.GapLine();
                listing_Standard.ColumnWidth = 230;
                Rect lastMainLabel;
                if (!currentNoHands)
                {
                    listing_Standard.Label("SMYH.mainhandhorizontal.label".Translate());
                    currentMainHand.x = Widgets.HorizontalSlider_NewTemp(listing_Standard.GetRect(20),
                        currentMainHand.x, -0.5f, 0.5f, false,
                        currentMainHand.x.ToString(), null, null, 0.001f);
                    lastMainLabel = listing_Standard.Label("SMYH.mainhandvertical.label".Translate());
                    currentMainHand.z = Widgets.HorizontalSlider_NewTemp(listing_Standard.GetRect(20),
                        currentMainHand.z, -0.5f, 0.5f, false,
                        currentMainHand.z.ToString(), null, null, 0.001f);
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentMainBehind);
                }
                else
                {
                    lastMainLabel = listing_Standard.Label("");
                }

                if (currentHasOffHand)
                {
                    listing_Standard.NewColumn();
                    listing_Standard.Gap(262);
                    listing_Standard.Label("SMYH.offhandhorizontal.label".Translate());
                    currentOffHand.x = Widgets.HorizontalSlider_NewTemp(listing_Standard.GetRect(20),
                        currentOffHand.x, -0.5f, 0.5f, false,
                        currentOffHand.x.ToString(), null, null, 0.001f);
                    listing_Standard.Label("SMYH.offhandvertical.label".Translate());
                    currentOffHand.z = Widgets.HorizontalSlider_NewTemp(listing_Standard.GetRect(20),
                        currentOffHand.z, -0.5f, 0.5f, false,
                        currentOffHand.z.ToString(), null, null, 0.001f);
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentOffBehind);
                }

                if (instance.Settings.ManualMainHandPositions.ContainsKey(currentDef.defName))
                {
                    DrawButton(() =>
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "SMYH.resetsingle.confirm".Translate(), delegate
                            {
                                ResetOneWeapon(currentDef, ref compProperties);
                                currentMainHand = compProperties.MainHand;
                                currentOffHand = compProperties.SecHand;
                                currentHasOffHand = currentOffHand != Vector3.zero;
                                currentMainBehind = compProperties.MainHand.y < 0;
                                currentOffBehind = compProperties.SecHand.y < 0;
                            }));
                    }, "SMYH.reset.button".Translate(), lastMainLabel.position + new Vector2(350, 170));
                }

                if (currentMainHand != compProperties.MainHand ||
                    currentOffHand != compProperties.SecHand ||
                    currentHasOffHand != (currentOffHand != Vector3.zero) ||
                    currentMainBehind != compProperties.MainHand.y < 0 ||
                    currentOffBehind != compProperties.SecHand.y < 0)
                {
                    DrawButton(() =>
                    {
                        currentNoHands = currentMainHand == Vector3.zero;
                        currentMainHand = compProperties.MainHand;
                        currentOffHand = compProperties.SecHand;
                        currentHasOffHand = currentOffHand != Vector3.zero;
                        currentMainBehind = compProperties.MainHand.y < 0;
                        currentOffBehind = compProperties.SecHand.y < 0;
                    }, "SMYH.undo.button".Translate(), lastMainLabel.position + new Vector2(190, 170));
                    DrawButton(() =>
                    {
                        currentMainHand.y = currentMainBehind ? -0.1f : 0.1f;
                        currentOffHand.y = currentOffBehind ? -0.1f : 0.1f;
                        if (!currentHasOffHand)
                        {
                            currentOffHand = Vector3.zero;
                        }

                        if (currentNoHands)
                        {
                            currentMainHand = Vector3.zero;
                        }

                        compProperties.MainHand = currentMainHand;
                        compProperties.SecHand = currentOffHand;
                        instance.Settings.ManualMainHandPositions[currentDef.defName] =
                            new SaveableVector3(compProperties.MainHand);
                        instance.Settings.ManualOffHandPositions[currentDef.defName] =
                            new SaveableVector3(compProperties.SecHand);
                    }, "SMYH.save.button".Translate(), lastMainLabel.position + new Vector2(25, 170));
                }

                listing_Standard.End();
                break;
            }
        }
    }

    private void CopyChangedWeapons(bool onlySelected = false)
    {
        if (onlySelected && string.IsNullOrEmpty(selectedSubDef))
        {
            return;
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        stringBuilder.AppendLine("<Defs>");
        if (onlySelected)
        {
            var selectedMod = ModsConfig.ActiveModsInLoadOrder.FirstOrDefault(data => data?.Name == selectedSubDef);
            stringBuilder.AppendLine(selectedMod is { PackageIdNonUnique: { } }
                ? $"  <WHands.ClutterHandsTDef MayRequire=\"{selectedMod.PackageIdNonUnique}\">"
                : "  <WHands.ClutterHandsTDef>");
        }
        else
        {
            stringBuilder.AppendLine("  <WHands.ClutterHandsTDef>");
        }

        stringBuilder.AppendLine(
            onlySelected
                ? $"     <defName>ClutterHandsSettings_{Regex.Replace(selectedSubDef, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled)}</defName>"
                : $"     <defName>ClutterHandsSettings_{SystemInfo.deviceName.GetHashCode()}_All</defName>");

        stringBuilder.AppendLine("      <label>Weapon hand settings</label>");
        stringBuilder.AppendLine("      <thingClass>Thing</thingClass>");
        stringBuilder.AppendLine("      <WeaponCompLoader>");
        var handPositionsToIterate = instance.Settings.ManualMainHandPositions;
        if (onlySelected)
        {
            var weaponsDefsToSelectFrom = (from ThingDef weapon in AllWeapons
                where weapon.modContentPack == null &&
                      selectedSubDef == "SMYH.unknown".Translate() ||
                      weapon.modContentPack?.Name == selectedSubDef
                select weapon.defName).ToList();
            handPositionsToIterate = new Dictionary<string, SaveableVector3>(
                from position in instance.Settings.ManualMainHandPositions
                where weaponsDefsToSelectFrom.Contains(position.Key)
                select position);
        }

        foreach (var settingsManualMainHandPosition in handPositionsToIterate)
        {
            stringBuilder.AppendLine("          <li>");
            stringBuilder.AppendLine($"              <MainHand>{settingsManualMainHandPosition.Value}</MainHand>");
            if (instance.Settings.ManualOffHandPositions.ContainsKey(settingsManualMainHandPosition.Key))
            {
                var secHand = instance.Settings.ManualOffHandPositions[settingsManualMainHandPosition.Key];
                if (secHand.ToVector3() != Vector3.zero)
                {
                    stringBuilder.AppendLine($"              <SecHand>{secHand}</SecHand>");
                }
            }

            stringBuilder.AppendLine("              <ThingTargets>");
            stringBuilder.AppendLine(
                $"                 <li>{settingsManualMainHandPosition.Key}</li> <!-- {ThingDef.Named(settingsManualMainHandPosition.Key).label} -->");
            stringBuilder.AppendLine("              </ThingTargets>");
            stringBuilder.AppendLine("          </li>");
        }

        stringBuilder.AppendLine("      </WeaponCompLoader>");
        stringBuilder.AppendLine("  </WHands.ClutterHandsTDef>");
        stringBuilder.AppendLine("</Defs>");

        GUIUtility.systemCopyBuffer = stringBuilder.ToString();
        Messages.Message("Modified data copied to clipboard.", MessageTypeDefOf.SituationResolved, false);
    }

    private void DrawTabsList(Rect rect)
    {
        var scrollContainer = rect.ContractedBy(10);
        scrollContainer.width = leftSideWidth;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        var innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        var tabFrameRect = innerContainer.ContractedBy(5);
        tabFrameRect.y += 15;
        tabFrameRect.height -= 15;
        var tabContentRect = tabFrameRect;
        tabContentRect.x = 0;
        tabContentRect.y = 0;
        tabContentRect.width -= 20;
        var weaponsToShow = AllWeapons;
        var listAddition = 24;
        if (!string.IsNullOrEmpty(selectedSubDef))
        {
            weaponsToShow = (from ThingDef weapon in AllWeapons
                where weapon.modContentPack == null &&
                      selectedSubDef == "SMYH.unknown".Translate() ||
                      weapon.modContentPack?.Name == selectedSubDef
                select weapon).ToList();
            listAddition = 60;
        }

        tabContentRect.height = (weaponsToShow.Count * 25f) + listAddition;
        Widgets.BeginScrollView(tabFrameRect, ref tabsScrollPosition, tabContentRect);
        listing_Standard.Begin(tabContentRect);
        //Text.Font = GameFont.Tiny;
        if (listing_Standard.ListItemSelectable("SMYH.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
        {
            SelectedDef = SelectedDef == "Settings" ? null : "Settings";
        }

        listing_Standard.ListItemSelectable(null, Color.yellow, out _);
        selectedHasManualDefs = new List<string>();
        foreach (var thingDef in weaponsToShow)
        {
            var toolTip = "SMYH.weaponrow.red";
            if (!DefinedByDef.Contains(thingDef.defName) &&
                !instance.Settings.ManualMainHandPositions.ContainsKey(thingDef.defName))
            {
                GUI.color = Color.red;
            }
            else
            {
                if (instance.Settings.ManualMainHandPositions.ContainsKey(thingDef.defName))
                {
                    toolTip = "SMYH.weaponrow.green";
                    GUI.color = Color.green;
                    selectedHasManualDefs.Add(thingDef.defName);
                }
                else
                {
                    toolTip = "SMYH.weaponrow.cyan";
                    GUI.color = Color.cyan;
                }
            }

            if (listing_Standard.ListItemSelectable(thingDef.label.CapitalizeFirst(), Color.yellow,
                    out var position,
                    SelectedDef == thingDef.defName, false, toolTip.Translate()))
            {
                SelectedDef = SelectedDef == thingDef.defName ? null : thingDef.defName;
                currentMainHand = Vector3.zero;
                currentOffHand = Vector3.zero;
            }

            GUI.color = Color.white;
            position.x = position.x + tabContentRect.width - iconSize.x;
            DrawWeapon(thingDef, new Rect(position, iconSize));
        }

        if (!string.IsNullOrEmpty(selectedSubDef))
        {
            listing_Standard.ListItemSelectable(null, Color.yellow, out _);
            if (listing_Standard.ListItemSelectable(
                    "SMYH.showhidden".Translate(AllWeapons.Count - weaponsToShow.Count), Color.yellow,
                    out _))
            {
                selectedSubDef = string.Empty;
            }
        }

        listing_Standard.End();
        //Text.Font = GameFont.Small;
        Widgets.EndScrollView();
    }

    private void ResetOneWeapon(ThingDef currentDef, ref WhandCompProps compProperties)
    {
        instance.Settings.ManualMainHandPositions.Remove(currentDef.defName);
        instance.Settings.ManualOffHandPositions.Remove(currentDef.defName);
        if (compProperties == null)
        {
            compProperties = currentDef.GetCompProperties<WhandCompProps>();
        }

        compProperties.MainHand = Vector3.zero;
        compProperties.SecHand = Vector3.zero;
        RimWorld_MainMenuDrawer_MainMenuOnGUI.LoadFromDefs(currentDef);
        if (compProperties.MainHand == Vector3.zero)
        {
            RimWorld_MainMenuDrawer_MainMenuOnGUI.FigureOutSpecific(currentDef);
        }
    }

    private Color GetColorFromPercent(decimal percent)
    {
        switch (percent)
        {
            case < 25:
                return Color.red;
            case < 50:
                return Color.yellow;
            case < 75:
                return Color.white;
            case >= 75:
                return Color.green;
        }
    }
}