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
        return ShowMeYourHandsMain.CELoaded;
    }

    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"), "DrawMesh");
    }

    public static void Postfix(Thing eq, float aimAngle, Matrix4x4 matrix)
    {
        var location = matrix.Position();
        var owner = ShowMeYourHandsMain.TryGetWeaponOwner(eq);
        if (owner != null)
        {
            var cachedLocation = location - owner.DrawPos;
            if (cachedLocation.MagnitudeHorizontalSquared() > 0.81f)
            {
                return;
            }

            ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(cachedLocation, aimAngle);
            return;
        }

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(location, aimAngle);
    }
}