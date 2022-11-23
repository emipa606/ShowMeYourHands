using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer), "DrawCarriedThing", typeof(Pawn), typeof(Vector3), typeof(Thing))]
public static class PawnRenderer_DrawCarriedThing
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

        if (pawn.CurJob?.def.defName == "Ingest" && !pawn.pather.Moving)
        {
            return;
        }

        var handComp = pawn.GetComp<HandDrawer>();
        if (handComp == null)
        {
            return;
        }

        PawnRenderer.CalculateCarriedDrawPos(pawn, carriedThing, ref drawLoc, out var behind, out var flip);
        if (behind)
        {
            drawLoc.y -= 0.03474903f;
        }
        else
        {
            drawLoc.y += 0.06474903f;
        }

        if (flip)
        {
            drawLoc.x *= -1;
        }

        handComp.DrawHands(carriedThing, drawLoc);

        //var behind = false;
        //var flip = false;
        //if (pawn.CurJob == null ||
        //    !pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref vector, ref behind, ref flip))
        //{
        //if (carriedThing is Pawn or Corpse)
        //{
        //    vector += new Vector3(0.44f, 0f, 0f);
        //}
        //else
        //{
        //    vector += new Vector3(0.18f, 0f, 0.05f);
        //}
        ////}

        //if (behind)
        //{
        //    vector.y -= 0.03474903f;
        //}
        //else
        //{
        //    vector.y += 0.03474903f;
        //}

        //handComp.DrawHands(carriedThing, drawLoc);
    }
}