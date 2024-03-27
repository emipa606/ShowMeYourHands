using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawCarriedThing), typeof(Pawn), typeof(Vector3),
    typeof(Thing))]
public static class PawnRenderUtility_DrawCarriedThing
{
    public static void Postfix(Vector3 drawLoc, Pawn pawn, Thing carriedThing)
    {
        if (carriedThing == null)
        {
            return;
        }

        if (!ShowMeYourHandsMod.instance.Settings.ShowWhenCarry)
        {
            return;
        }

        var handComp = pawn.GetComp<HandDrawer>();
        if (handComp == null)
        {
            return;
        }

        PawnRenderUtility.CalculateCarriedDrawPos(pawn, carriedThing, ref drawLoc, out var flip);
        //if (behind)
        //{
        //    drawLoc.y -= 0.03474903f;
        //}
        //else
        //{
        //    drawLoc.y += 0.06474903f;
        //}

        if (flip)
        {
            drawLoc.x *= -1;
        }

        handComp.ItemHeldLocation = drawLoc;
    }
}