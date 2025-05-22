using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch]
public static class CombatExtended_PawnRenderer_DrawEquipmentAiming
{
    public static bool Prepare()
    {
        return ModLister.GetActiveModWithIdentifier("CETeam.CombatExtended", true) != null;
    }

    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"), "DrawMesh");
    }

    public static void Postfix(Thing eq, float aimAngle, Matrix4x4 matrix)
    {
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(matrix.Position(), aimAngle);
    }
}