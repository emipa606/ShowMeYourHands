using Verse;
using UnityEngine;

namespace ShowMeYourHands;

internal class GeneratedHandPositionCacheEntry : IExposable
{
    public Vector3 MainHand;
    public Vector3 SecHand;
    public float MainRotation;
    public float SecRotation;
    public string ModPackageId;
    public long AboutFileLastWriteTimeUtcTicks;

    public void ExposeData()
    {
        Scribe_Values.Look(ref MainHand, "MainHand");
        Scribe_Values.Look(ref SecHand, "SecHand");
        Scribe_Values.Look(ref MainRotation, "MainRotation");
        Scribe_Values.Look(ref SecRotation, "SecRotation");
        Scribe_Values.Look(ref ModPackageId, "ModPackageId");
        Scribe_Values.Look(ref AboutFileLastWriteTimeUtcTicks, "AboutFileLastWriteTimeUtcTicks");
    }
}
