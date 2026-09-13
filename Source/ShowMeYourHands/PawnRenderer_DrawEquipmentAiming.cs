using System;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public static class PawnRenderer_DrawEquipmentAiming
{
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