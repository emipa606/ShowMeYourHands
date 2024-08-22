using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch]
public static class CombatExtended_PawnRenderer_DrawEquipmentAiming
{
    private static FieldInfo recoilOffsetField;
    private static FieldInfo muzzleJumpField;

    public static bool Prepare()
    {
        if (ModLister.GetActiveModWithIdentifier("CETeam.CombatExtended") == null)
        {
            return false;
        }

        recoilOffsetField = AccessTools.Field(
            AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"),
            "recoilOffset");
        muzzleJumpField = AccessTools.Field(
            AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"),
            "muzzleJump");
        return true;
    }

    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"), "DrawMesh");
    }

    public static void Postfix(Thing eq, Vector3 position, float aimAngle)
    {
        var recoilOffset = (Vector3)recoilOffsetField.GetValue(null);
        var muzzleJump = (float)muzzleJumpField.GetValue(null);

        if (aimAngle is > 200f and < 340f)
        {
            muzzleJump = -muzzleJump;
        }

        position += recoilOffset;
        aimAngle += muzzleJump;

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(position, aimAngle);
    }
}