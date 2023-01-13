using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(MemoryUtility), "UnloadUnusedUnityAssets")]
public static class MemoryUtility_UnloadUnusedUnityAssets
{
    public static void Postfix()
    {
        ShowMeYourHandsMain.mainHandGraphics = new Dictionary<Pawn, Graphic>();
        ShowMeYourHandsMain.offHandGraphics = new Dictionary<Pawn, Graphic>();
        ShowMeYourHandsMain.pawnBodySizes = new Dictionary<Pawn, float>();
        ShowMeYourHandsMain.pawnsMissingAHand = new Dictionary<Pawn, bool>();
        ShowMeYourHandsMain.colorDictionary = new Dictionary<Thing, Color>();
        ShowMeYourHandsMain.flippedHandMeshes = new Dictionary<Pawn, Mesh>();
        ShowMeYourHandsMain.handMeshes = new Dictionary<Pawn, Mesh>();
        ShowMeYourHandsMain.pawnMeshes = new Dictionary<Pawn, Mesh>();
    }
}