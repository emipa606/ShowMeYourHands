using System;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public class PawnRenderer_DrawEquipmentAiming
{
    public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}