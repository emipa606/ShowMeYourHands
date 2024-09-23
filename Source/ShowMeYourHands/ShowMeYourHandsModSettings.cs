using System.Collections.Generic;
using Verse;

namespace ShowMeYourHands;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class ShowMeYourHandsModSettings : ModSettings
{
    public Dictionary<string, SaveableVector3> ManualMainHandPositions = new Dictionary<string, SaveableVector3>();

    private List<string> manualMainHandPositionsKeys;

    private List<SaveableVector3> manualMainHandPositionsValues;
    public Dictionary<string, float> ManualMainHandRotations = new Dictionary<string, float>();
    private List<string> manualMainHandRotationsKeys;
    private List<float> manualMainHandRotationsValues;

    public Dictionary<string, SaveableVector3> ManualOffHandPositions = new Dictionary<string, SaveableVector3>();

    private List<string> manualOffHandPositionsKeys;

    private List<SaveableVector3> manualOffHandPositionsValues;
    public Dictionary<string, float> ManualOffHandRotations = new Dictionary<string, float>();
    private List<string> manualOffHandRotationsKeys;
    private List<float> manualOffHandRotationsValues;

    public bool MatchArmorColor;
    public bool MatchArtificialLimbColor;
    public bool MatchHandAmounts;
    public bool RepositionHands = true;
    public bool ResizeHands = true;
    public bool Rotation = ModsConfig.IsActive("andromeda.nicehands");
    public bool ShowCrawling;
    public bool ShowOtherTmes;
    public bool ShowWhenCarry;
    public bool VerboseLogging;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref VerboseLogging, "VerboseLogging");
        Scribe_Values.Look(ref Rotation, "Rotation", ModsConfig.IsActive("andromeda.nicehands"));
        Scribe_Values.Look(ref MatchArmorColor, "MatchArmorColor");
        Scribe_Values.Look(ref MatchArtificialLimbColor, "MatchArtificialLimbColor");
        Scribe_Values.Look(ref MatchHandAmounts, "MatchHandAmounts");
        Scribe_Values.Look(ref ResizeHands, "ResizeHands", true);
        Scribe_Values.Look(ref RepositionHands, "RepositionHands", true);
        Scribe_Values.Look(ref ShowWhenCarry, "ShowWhenCarry");
        Scribe_Values.Look(ref ShowOtherTmes, "ShowOtherTmes");
        Scribe_Values.Look(ref ShowCrawling, "ShowCrawling");
        Scribe_Collections.Look(ref ManualMainHandPositions, "ManualMainHandPositions", LookMode.Value,
            LookMode.Value,
            ref manualMainHandPositionsKeys, ref manualMainHandPositionsValues);
        Scribe_Collections.Look(ref ManualOffHandPositions, "ManualOffHandPositions", LookMode.Value,
            LookMode.Value,
            ref manualOffHandPositionsKeys, ref manualOffHandPositionsValues);
        Scribe_Collections.Look(ref ManualMainHandRotations, "ManualMainHandRotations", LookMode.Value,
            LookMode.Value,
            ref manualMainHandRotationsKeys, ref manualMainHandRotationsValues);
        Scribe_Collections.Look(ref ManualOffHandRotations, "ManualOffHandRotations", LookMode.Value,
            LookMode.Value,
            ref manualOffHandRotationsKeys, ref manualOffHandRotationsValues);
    }

    public void ResetManualValues()
    {
        manualMainHandPositionsKeys = [];
        manualMainHandPositionsValues = [];
        ManualMainHandPositions = new Dictionary<string, SaveableVector3>();
        manualOffHandPositionsKeys = [];
        manualOffHandPositionsValues = [];
        ManualOffHandPositions = new Dictionary<string, SaveableVector3>();
        manualMainHandRotationsKeys = [];
        manualMainHandRotationsValues = [];
        ManualMainHandRotations = new Dictionary<string, float>();
        manualOffHandRotationsKeys = [];
        manualOffHandRotationsValues = [];
        ManualOffHandRotations = new Dictionary<string, float>();
        RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
    }
}