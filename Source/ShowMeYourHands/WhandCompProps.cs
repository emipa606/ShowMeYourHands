using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public class WhandCompProps : CompProperties
{
    public Vector3 MainHand = Vector3.zero;
    public float MainRotation = 0f;
    public Vector3 SecHand = Vector3.zero;
    public float SecRotation = 0f;

    public WhandCompProps()
    {
        compClass = typeof(WhandComp);
    }
}