using UnityEngine;
using Verse;

namespace ShowMeYourHands;

internal class GeneratedHandPositionCacheEntry : IExposable
{
    public long AboutFileLastWriteTimeUtcTicks;
    public Vector3 MainHand;
    public float MainRotation;
    public string ModPackageId;
    public Vector3 SecHand;
    public float SecRotation;

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