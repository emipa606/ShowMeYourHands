using System.Collections.Generic;
using System.Linq;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using UnityEngine;
using Verse;
using static System.Byte;
using Object = UnityEngine.Object;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public class HandDrawer : ThingComp
{
    public Vector3 ItemHeldLocation;
    private int LastDrawn;
    private Vector3 MainHand;
    private float MainHandRotation;
    private Vector3 OffHand;
    private float OffHandRotation;

    private Color HandColor
    {
        get
        {
            if (parent is not Pawn pawn)
            {
                return Color.white;
            }

            if (!pawn.IsHashIntervalTick(100) && field != default)
            {
                return field;
            }

            field = getHandColor(pawn, out var hasGloves, out var secondColor);
            if (!ShowMeYourHandsMain.mainHandGraphics.ContainsKey(pawn) ||
                ShowMeYourHandsMain.mainHandGraphics[pawn].color != field)
            {
                if (hasGloves)
                {
                    ShowMeYourHandsMain.mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("HandClean",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        field, field);
                }
                else
                {
                    ShowMeYourHandsMain.mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("Hand",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        field, field);
                }
            }

            if (ShowMeYourHandsMain.offHandGraphics.ContainsKey(pawn) &&
                ShowMeYourHandsMain.offHandGraphics[pawn].color == field)
            {
                return field;
            }

            if (hasGloves)
            {
                ShowMeYourHandsMain.offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHandClean",
                    ShaderDatabase.Cutout,
                    new Vector2(1f, 1f),
                    field, field);
            }
            else
            {
                if (secondColor != default)
                {
                    ShowMeYourHandsMain.offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHand",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        secondColor, secondColor);
                }
                else
                {
                    ShowMeYourHandsMain.offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHand",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        field, field);
                }
            }

            return field;
        }
    }

    public void ReadXML()
    {
        var whandCompProps = (WhandCompProps)props;
        if (whandCompProps.MainHand != Vector3.zero)
        {
            MainHand = whandCompProps.MainHand;
        }

        if (whandCompProps.SecHand != Vector3.zero)
        {
            OffHand = whandCompProps.SecHand;
        }
    }

    private void DrawHandsOnWeapon(Pawn pawn)
    {
        var mainHandWeapon = pawn.equipment.Primary;
        var compProperties = mainHandWeapon.def.GetCompProperties<WhandCompProps>();
        if (compProperties != null)
        {
            MainHand = compProperties.MainHand;
            OffHand = compProperties.SecHand;
            MainHandRotation = compProperties.MainRotation;
            OffHandRotation = compProperties.SecRotation;
        }
        else
        {
            OffHand = Vector3.zero;
            MainHand = Vector3.zero;
            MainHandRotation = 0f;
            OffHandRotation = 0f;
        }

        ThingWithComps offhandWeapon = null;
        if (pawn.equipment.AllEquipmentListForReading.Count == 2)
        {
            offhandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                where weapon != mainHandWeapon
                select weapon).First();
            var offhandComp = offhandWeapon?.def.GetCompProperties<WhandCompProps>();
            if (offhandComp != null)
            {
                OffHand = offhandComp.MainHand;
            }
        }

        if (pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true } stance_Busy)
        {
            var a = stance_Busy.focusTarg.HasThing
                ? stance_Busy.focusTarg.Thing.DrawPos
                : stance_Busy.focusTarg.Cell.ToVector3Shifted();

            var num = 0f;
            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (a - pawn.DrawPos).AngleFlat();
            }

            DrawHandsOnWeapon(mainHandWeapon, num, pawn, offhandWeapon, false, true);
            return;
        }

        if (!(bool)ShowMeYourHandsMain.CarryWeaponMethod.Invoke(pawn.Drawer.renderer, [pawn]))
        {
            return;
        }

        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
        {
            DrawHandsOnWeapon(mainHandWeapon, 143f, pawn, offhandWeapon, true);
            return;
        }

        if (pawn.Rotation == Rot4.East)
        {
            DrawHandsOnWeapon(mainHandWeapon, 143f, pawn, offhandWeapon);
            return;
        }

        if (pawn.Rotation != Rot4.West)
        {
            return;
        }

        DrawHandsOnWeapon(mainHandWeapon, 217f, pawn, offhandWeapon);
    }


    private void drawHandsAllTheTime(Pawn pawn)
    {
        if (!ShowMeYourHandsMain.pawnBodySizes.ContainsKey(pawn))
        {
            var bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabiesAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, [pawn]);
                }
            }

            ShowMeYourHandsMain.pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        _ = HandColor;

        var mesh = ShowMeYourHandsMain.GetMeshFromPawn(pawn);
        var mainHandTex = ShowMeYourHandsMain.mainHandGraphics[pawn];
        var offHandTex = ShowMeYourHandsMain.offHandGraphics[pawn];

        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        var mainSingle = mainHandTex.MatSingle;
        var offSingle = offHandTex.MatSingle;
        var heightOffset = new Vector3(0, 0, 0.7f * ShowMeYourHandsMain.pawnBodySizes[pawn] / 2);
        var sideOffset = new Vector3(0.2f, 0, 0);
        var layerOffset = new Vector3(0, 0.1f, 0);

        var basePosition = pawn.DrawPos - heightOffset;
        if (pawn.Crawling)
        {
            var offsetPercent =
                Mathf.Clamp(Vector3.Distance(pawn.Drawer.tweener.LastTickTweenedVelocity, Vector3.zero) * 100, 0f,
                    1f);

            var offset = offsetPercent * 0.2f * ShowMeYourHandsMain.pawnBodySizes[pawn];
            sideOffset += new Vector3(0.1f, 0, 0);

            if (pawn.Rotation == Rot4.West)
            {
                basePosition += new Vector3(offset, 0, 0);
            }

            if (pawn.Rotation == Rot4.East)
            {
                basePosition -= new Vector3(offset, 0, 0);
            }

            if (pawn.Rotation == Rot4.North)
            {
                basePosition += heightOffset * 2;
                basePosition -= new Vector3(0, 0.1f, 0);
                basePosition -= new Vector3(0, 0, offset);
            }

            if (pawn.Rotation == Rot4.South)
            {
                basePosition -= heightOffset;
                basePosition += new Vector3(0, 0, offset);
            }
        }
        else
        {
            if (pawn.Downed)
            {
                return;
            }
        }

        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(mesh,
                basePosition + sideOffset - layerOffset, new Quaternion(), mainSingle, 0);
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(mesh,
                basePosition - sideOffset + layerOffset, new Quaternion(), mainSingle, 0);
        }

        if (pawn.Rotation == Rot4.East)
        {
            if (pawn.Crawling)
            {
                Graphics.DrawMesh(mesh,
                    basePosition + sideOffset, new Quaternion(), mainSingle, 0);
            }
            else
            {
                Graphics.DrawMesh(mesh,
                    basePosition + layerOffset, new Quaternion(), mainSingle, 0);
                return;
            }
        }

        if (pawn.Rotation == Rot4.West && pawn.Crawling)
        {
            Graphics.DrawMesh(mesh,
                basePosition - sideOffset, new Quaternion(), mainSingle, 0);
        }

        if (ShowMeYourHandsMain.pawnsMissingAHand.ContainsKey(pawn) && ShowMeYourHandsMain.pawnsMissingAHand[pawn])
        {
            return;
        }

        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(mesh,
                basePosition - sideOffset - layerOffset, new Quaternion(), offSingle, 0);
            return;
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(mesh,
                basePosition + sideOffset + layerOffset, new Quaternion(), offSingle, 0);
            return;
        }

        if (pawn.Crawling)
        {
            if (pawn.Rotation == Rot4.West)
            {
                Graphics.DrawMesh(mesh,
                    basePosition - (sideOffset * 2), new Quaternion(), mainSingle, 0);
            }
            else
            {
                Graphics.DrawMesh(mesh,
                    basePosition + (sideOffset * 2), new Quaternion(), mainSingle, 0);
            }

            return;
        }

        Graphics.DrawMesh(mesh,
            basePosition + layerOffset, new Quaternion(), offSingle, 0);
    }

    private void DrawHandsOnItem(Pawn pawn)
    {
        if (pawn.CurJob?.def.defName is "Ingest" or "SocialRelax" && !pawn.pather.Moving)
        {
            return;
        }

        if (!ShowMeYourHandsMain.pawnBodySizes.ContainsKey(pawn))
        {
            var bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabiesAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, [pawn]);
                }
            }

            ShowMeYourHandsMain.pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        _ = HandColor;
        var mesh = ShowMeYourHandsMain.GetMeshFromPawn(pawn);
        var mainHandTex = ShowMeYourHandsMain.mainHandGraphics[pawn];
        var offHandTex = ShowMeYourHandsMain.offHandGraphics[pawn];


        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        LastDrawn = GenTicks.TicksAbs;
        var matSingle = mainHandTex.MatSingle;
        var offSingle = offHandTex.MatSingle;
        var height = new Vector3(0, 0, 0.1f);
        var width = new Vector3(-0.2f, 0, 0);
        if (pawn.Rotation == Rot4.West)
        {
            height.z *= -1;
        }

        Graphics.DrawMesh(mesh,
            ItemHeldLocation + height + width, new Quaternion(), matSingle, 0);

        if (ShowMeYourHandsMain.pawnsMissingAHand.ContainsKey(pawn) && ShowMeYourHandsMain.pawnsMissingAHand[pawn])
        {
            return;
        }

        Graphics.DrawMesh(mesh,
            ItemHeldLocation + (height * -1) + (width * -1), new Quaternion(), offSingle, 0);
    }

    private void DrawHandsOnWeapon(Thing mainHandWeapon, float aimAngle, Pawn pawn, Thing offHandWeapon = null,
        bool idle = false, bool aiming = false)
    {
        var flipped = false;
        var skipMainHand = false;
        var skipOffHand = false;

        if (!ShowMeYourHandsMain.weaponLocations.TryGetValue(mainHandWeapon, out var location))
        {
            return;
        }

        var mainWeaponLocation = location.Item1;
        var mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
        var offhandWeaponLocation = Vector3.zero;
        var offHandAngle = mainHandAngle;
        var mainMeleeExtra = 0f;
        var offMeleeExtra = 0f;
        var mainMelee = false;
        var offMelee = false;
        if (offHandWeapon != null && ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
        {
            offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
            offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
        }

        mainHandAngle -= 90f;
        offHandAngle -= 90f;
        if (pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f)
        {
            flipped = true;
            MainHandRotation *= -1;
            OffHandRotation *= -1;
        }

        if (mainHandWeapon.def.IsMeleeWeapon)
        {
            skipMainHand = ShowMeYourHandsMain.MeleeAnimationsLoaded;
            mainMelee = true;
            mainMeleeExtra = 0.0001f;
            if (idle && offHandWeapon != null) //Dual wield idle vertical
            {
                if (pawn.Rotation == Rot4.South)
                {
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                mainHandAngle -= 180f;
            }
        }

        if (offHandWeapon?.def.IsMeleeWeapon == true)
        {
            skipOffHand = ShowMeYourHandsMain.MeleeAnimationsLoaded;
            offMelee = true;
            offMeleeExtra = 0.0001f;
            if (idle && pawn.Rotation == Rot4.North) //Dual wield north
            {
                offHandAngle -= offHandWeapon.def.equippedAngleOffset;
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    offHandAngle += offHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                offHandAngle -= 180f;
            }
        }

        mainHandAngle %= 360f;
        offHandAngle %= 360f;

        _ = HandColor;

        if (!ShowMeYourHandsMain.mainHandGraphics.ContainsKey(pawn) ||
            !ShowMeYourHandsMain.offHandGraphics.ContainsKey(pawn))
        {
            return;
        }

        var mainHandTex = ShowMeYourHandsMain.mainHandGraphics[pawn];
        var offHandTex = ShowMeYourHandsMain.offHandGraphics[pawn];


        if (mainHandTex == null || offHandTex == null)
        {
            return;
        }

        var matSingle = mainHandTex.MatSingle;
        var offSingle = offHandTex.MatSingle;
        var drawSize = 1f;
        LastDrawn = GenTicks.TicksAbs;

        if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
            mainHandWeapon.def?.graphicData?.drawSize.x != 1f)
        {
            if (mainHandWeapon.def is { graphicData: not null })
            {
                drawSize = mainHandWeapon.def.graphicData.drawSize.x;
            }
        }

        if (!ShowMeYourHandsMain.pawnBodySizes.ContainsKey(pawn))
        {
            var bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabiesAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, [pawn]);
                }

                if (ShowMeYourHandsMain.BigAndSmallLoaded)
                {
                    bodySize = BigAndSmallFramework.GetModifiedSize(pawn, bodySize);
                }
            }

            ShowMeYourHandsMain.pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        var mesh = ShowMeYourHandsMain.GetMeshFromPawn(pawn, flipped);

        if (MainHand != Vector3.zero && !skipMainHand)
        {
            var x = MainHand.x * drawSize;
            var z = MainHand.z * drawSize;
            var y = MainHand.y < 0 ? -0.0001f : 0.0001f;

            if (flipped)
            {
                x *= -1;
            }

            if (pawn.Rotation == Rot4.North && !mainMelee && !aiming)
            {
                z += 0.1f;
            }

            mainWeaponLocation += adjustRenderOffsetFromDir(pawn, mainHandWeapon as ThingWithComps);

            Graphics.DrawMesh(mesh,
                mainWeaponLocation + new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle),
                Quaternion.AngleAxis(mainHandAngle + MainHandRotation, Vector3.up), y >= 0 ? matSingle : offSingle, 0);
        }

        if (OffHand == Vector3.zero || skipOffHand || ShowMeYourHandsMain.pawnsMissingAHand.ContainsKey(pawn) &&
            ShowMeYourHandsMain.pawnsMissingAHand[pawn])
        {
            return;
        }

        var x2 = OffHand.x * drawSize;
        var z2 = OffHand.z * drawSize;
        var y2 = OffHand.y < 0 ? -0.0001f : 0.0001f;


        if (offHandWeapon != null)
        {
            drawSize = 1f;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                offHandWeapon.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = offHandWeapon.def.graphicData.drawSize.x;
            }

            x2 = OffHand.x * drawSize;
            z2 = OffHand.z * drawSize;

            if (flipped)
            {
                x2 *= -1;
            }

            if (idle && !offMelee)
            {
                if (pawn.Rotation == Rot4.South)
                {
                    z2 += 0.05f;
                }
                else
                {
                    z2 -= 0.05f;
                }
            }


            offhandWeaponLocation += adjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);

            Graphics.DrawMesh(mesh,
                offhandWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle),
                Quaternion.AngleAxis(offHandAngle + OffHandRotation, Vector3.up),
                y2 >= 0 ? matSingle : offSingle, 0);
            return;
        }

        if (flipped)
        {
            x2 *= -1;
        }

        Graphics.DrawMesh(mesh,
            mainWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(mainHandAngle),
            Quaternion.AngleAxis(mainHandAngle + OffHandRotation, Vector3.up), y2 >= 0 ? matSingle : offSingle, 0);
    }

    private static Vector3 adjustRenderOffsetFromDir(Pawn pawn, ThingWithComps weapon)
    {
        if (!ShowMeYourHandsMain.OversizedWeaponLoaded && !ShowMeYourHandsMain.EnableOversizedLoaded)
        {
            return Vector3.zero;
        }

        switch (pawn.Rotation.AsInt)
        {
            case 0:
                return ShowMeYourHandsMain.northOffsets.TryGetValue(weapon.def, out var northValue)
                    ? northValue
                    : Vector3.zero;
            case 1:
                return ShowMeYourHandsMain.eastOffsets.TryGetValue(weapon.def, out var eastValue)
                    ? eastValue
                    : Vector3.zero;
            case 2:
                return ShowMeYourHandsMain.southOffsets.TryGetValue(weapon.def, out var southValue)
                    ? southValue
                    : Vector3.zero;
            case 3:
                return ShowMeYourHandsMain.westOffsets.TryGetValue(weapon.def, out var westValue)
                    ? westValue
                    : Vector3.zero;
            default:
                return Vector3.zero;
        }
    }


    public override void PostDraw()
    {
        if (parent is not Pawn { Spawned: true } pawn || pawn.Map == null)
        {
            return;
        }

        if (!ShowMeYourHandsMod.instance.Settings.ShowOnRace.TryGetValue(pawn.def.defName, out var showOnRace) ||
            !showOnRace)
        {
            return;
        }

        if (ShowMeYourHandsMod.instance.Settings.ShowWhenCarry && pawn.carryTracker?.CarriedThing != null)
        {
            DrawHandsOnItem(pawn);
            return;
        }


        if (pawn.equipment?.Primary != null && pawn.CurJob?.def.neverShowWeapon == false)
        {
            DrawHandsOnWeapon(pawn);
            return;
        }

        if (!ShowMeYourHandsMod.instance.Settings.ShowOtherTmes && !ShowMeYourHandsMod.instance.Settings.ShowCrawling ||
            LastDrawn >= GenTicks.TicksAbs - 1 ||
            GenTicks.TicksAbs == 0)
        {
            return;
        }

        if (ShowMeYourHandsMod.instance.Settings.ShowCrawling && !pawn.Crawling)
        {
            return;
        }

        drawHandsAllTheTime(pawn);
    }

    private static Color getHandColor(Pawn pawn, out bool hasGloves, out Color secondColor)
    {
        hasGloves = false;
        secondColor = default;
        if (pawn.story == null)
        {
            if (!ShowMeYourHandsMain.raceDictionary.ContainsKey(pawn.def))
            {
                ShowMeYourHandsMain.raceDictionary[pawn.def] =
                    averageColorFromTexture((Texture2D)pawn.kindDef.lifeStages.Last().bodyGraphicData.Graphic.MatSingle
                        .mainTexture);
            }

            return ShowMeYourHandsMain.raceDictionary[pawn.def];
        }

        var baseColor = pawn.story.SkinColor;

        IEnumerable<Hediff> addedHands = [];

        if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
            ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
        {
            addedHands = pawn.health?.hediffSet?.hediffs.Where(hediff =>
                hediff is Hediff_AddedPart addedPart && ShowMeYourHandsMain.HediffContainsHand(addedPart.Part));
        }

        if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts && pawn.health is { hediffSet: not null })
        {
            ShowMeYourHandsMain.pawnsMissingAHand[pawn] = pawn.health.hediffSet
                    .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.HandDef) +
                addedHands?.Count() < 2;
        }

        if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor || !(from apparel in pawn.apparel.WornApparel
                where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                select apparel).Any())
        {
            if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor || addedHands == null ||
                !addedHands.Any())
            {
                return baseColor;
            }

            var mainColor = (Color)default;

            foreach (var hediffAddedPart in addedHands)
            {
                if (!ShowMeYourHandsMain.HediffColors.TryGetValue(hediffAddedPart.def, out var hediffColor))
                {
                    continue;
                }

                if (mainColor == default)
                {
                    mainColor = hediffColor;
                    continue;
                }

                secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
            }

            return mainColor == default ? baseColor : mainColor;
        }

        if (pawn.apparel == null)
        {
            return baseColor;
        }

        var handApparel = from apparel in pawn.apparel.WornApparel
            where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
            select apparel;

        //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

        Thing outerApparel = null;
        var highestDrawOrder = 0;
        foreach (var thing in handApparel)
        {
            var thingOutmostLayer =
                thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
            if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
            {
                continue;
            }

            highestDrawOrder = thingOutmostLayer;
            outerApparel = thing;
        }

        if (outerApparel == null)
        {
            return pawn.story.SkinColor;
        }

        hasGloves = true;
        ShowMeYourHandsMain.colorDictionary ??= new Dictionary<Thing, Color>();

        if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
        {
            var comp = outerApparel.TryGetComp<CompColorable>();
            if (comp.Active)
            {
                return comp.Color;
            }
        }

        if (ShowMeYourHandsMain.colorDictionary.TryGetValue(outerApparel, out var color))
        {
            return color;
        }

        if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
        {
            ShowMeYourHandsMain.colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
        }
        else
        {
            ShowMeYourHandsMain.colorDictionary[outerApparel] =
                averageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
        }

        return ShowMeYourHandsMain.colorDictionary[outerApparel];
    }

    private static Color32 averageColorFromTexture(Texture2D texture)
    {
        var renderTexture = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, renderTexture);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        var tex = new Texture2D(texture.width, texture.height);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        var result = averageColorFromColors(tex.GetPixels32());
        Object.Destroy(tex);
        return result;
    }

    private static Color32 averageColorFromColors(Color32[] colors)
    {
        // ReSharper disable once UsageOfDefaultStructEquality
        var shadeDictionary = new Dictionary<Color32, int>();
        foreach (var texColor in colors)
        {
            if (texColor.a < 50)
            {
                // Ignore low transparency
                continue;
            }

            var currentRgb = new Rgb { B = texColor.b, G = texColor.b, R = texColor.r };

            if (currentRgb.Compare(new Rgb { B = 0, G = 0, R = 0 }, new Cie1976Comparison()) < 2)
            {
                // Ignore black pixels
                continue;
            }

            if (shadeDictionary.Count == 0)
            {
                shadeDictionary[texColor] = 1;
                continue;
            }


            var added = false;
            foreach (var rgb in shadeDictionary.Keys.Where(rgb =>
                         currentRgb.Compare(new Rgb { B = rgb.b, G = rgb.b, R = rgb.r }, new Cie1976Comparison()) < 2))
            {
                shadeDictionary[rgb]++;
                added = true;
                break;
            }

            if (!added)
            {
                shadeDictionary[texColor] = 1;
            }
        }

        if (shadeDictionary.Count == 0)
        {
            return new Color32(0, 0, 0, MaxValue);
        }

        var greatestValue = shadeDictionary.Aggregate((rgb, max) => rgb.Value > max.Value ? rgb : max).Key;
        greatestValue.a = MaxValue;
        return greatestValue;
    }
}