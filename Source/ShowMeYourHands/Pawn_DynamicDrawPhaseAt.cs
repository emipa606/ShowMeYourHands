using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.DynamicDrawPhaseAt))]
internal static class Pawn_DynamicDrawPhaseAt
{
    public static void Postfix(Pawn __instance, DrawPhase phase, bool __runOriginal)
    {
        if (__runOriginal && phase == DrawPhase.Draw)
        {
            __instance.GetComp<HandDrawer>()?.DrawHandsAfterPawn();
        }
    }
}