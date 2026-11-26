using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public static class PawnRenderer_DrawEquipmentAiming
{
    private static readonly HashSet<Thing> drawnWeapons = [];
    private static int drawnFrame = -1;

    public static void RecordWeaponDraw(Thing weapon)
    {
        var frame = Time.frameCount;
        if (drawnFrame != frame)
        {
            drawnWeapons.Clear();
            drawnFrame = frame;
        }

        drawnWeapons.Add(weapon);
    }

    public static bool WasWeaponDrawn(Thing weapon)
    {
        return drawnFrame == Time.frameCount && drawnWeapons.Contains(weapon);
    }

    public static void ConfirmWeaponDraw(Thing eq, bool __runOriginal)
    {
        if (__runOriginal)
        {
            RecordWeaponDraw(eq);
        }
    }

    public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        var owner = ShowMeYourHandsMain.TryGetWeaponOwner(eq);
        if (owner != null)
        {
            var cachedLocation = drawLoc - owner.DrawPos;
            if (cachedLocation.MagnitudeHorizontalSquared() > 0.81f)
            {
                return;
            }

            ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(cachedLocation, aimAngle);
            return;
        }

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}