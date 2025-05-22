using BigAndSmall;
using Verse;

namespace ShowMeYourHands;

public class BigAndSmallFramework
{
    public static float GetModifiedSize(Pawn pawn, float originalSize)
    {
        if (HumanoidPawnScaler.GetCache(pawn) is { } cache)
        {
            originalSize *= cache.bodyRenderSize;
        }

        return originalSize;
    }
}