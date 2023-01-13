using System;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

//[HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming", typeof(Thing), typeof(Vector3), typeof(float))]
public class PawnRenderer_DrawEquipmentAiming
{
    //[HarmonyPrefix]
    //[HarmonyPriority(Priority.High)]
    //// JecsTools Oversized overrides always
    //// Gunplay overrides if animations is turned on
    //// [O21] Toolbox overrides if animations is turned on
    //// Yayo Combat 3 overrides if animations is turned on
    //[HarmonyBefore("jecstools.jecrell.comps.oversized",
    //    "rimworld.androitiers-jecrell.comps.oversized",
    //    "com.github.automatic1111.gunplay",
    //    "com.o21toolbox.rimworld.mod",
    //    "com.yayo.combat",
    //    "com.yayo.combat3")]
    public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}