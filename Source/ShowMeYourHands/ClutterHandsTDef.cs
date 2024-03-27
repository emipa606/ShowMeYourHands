using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WHands;

public class ClutterHandsTDef : ThingDef
{
    public readonly List<CompTargets> WeaponCompLoader = [];

    public class CompTargets
    {
        public readonly List<string> ThingTargets = [];
        public Vector3 MainHand = Vector3.zero;
        public Vector3 SecHand = Vector3.zero;
    }
}