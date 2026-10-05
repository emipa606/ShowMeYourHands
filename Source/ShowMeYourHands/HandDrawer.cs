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

    private bool DrawHandsOnWeapon(Pawn pawn)
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
            return true;
        }

        if (!(bool)ShowMeYourHandsMain.CarryWeaponMethod.Invoke(pawn.Drawer.renderer, [pawn]))
        {
            return false;
        }

        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
        {
            DrawHandsOnWeapon(mainHandWeapon, 143f, pawn, offhandWeapon, true);
            return true;
        }

        if (pawn.Rotation == Rot4.East)
        {
            DrawHandsOnWeapon(mainHandWeapon, 143f, pawn, offhandWeapon);
            return true;
        }

        if (pawn.Rotation != Rot4.West)
        {
            return false;
        }

        DrawHandsOnWeapon(mainHandWeapon, 217f, pawn, offhandWeapon);
        return true;
    }

    private void DrawHandsOnWeapon(Thing mainHandWeapon, float aimAngle, Pawn pawn, Thing offHandWeapon = null,
        bool idle = false, bool aiming = false)
    {
        var flipped = false;

        if (!ShowMeYourHandsMain.weaponLocations.TryGetValue(mainHandWeapon, out var location))
        {
            return;
        }

        var mainWeaponLocation = GetCurrentWeaponLocation(mainHandWeapon, location.Item1);
        var mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
        var offhandWeaponLocation = Vector3.zero;
        var offHandAngle = mainHandAngle;
        if (offHandWeapon != null && ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
        {
            offhandWeaponLocation =
                GetCurrentWeaponLocation(offHandWeapon, ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1);
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

        mainHandAngle = adjustMainHandAngle(mainHandWeapon, offHandWeapon, pawn, idle, flipped, mainHandAngle,
            out var skipMainHand, out var mainMelee, out var mainMeleeExtra);
        offHandAngle = adjustOffHandAngle(offHandWeapon, pawn, idle, flipped, offHandAngle,
            out var skipOffHand, out var offMelee, out var offMeleeExtra);

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
            mainHandWeapon.def?.graphicData?.drawSize.x.Equals(1f) == false &&
            mainHandWeapon.def is { graphicData: not null })
        {
            drawSize = mainHandWeapon.def.graphicData.drawSize.x;
        }

        ensurePawnBodySizeCached(pawn);

        var mesh = ShowMeYourHandsMain.GetMeshFromPawn(pawn, flipped);

        drawMainHand(mesh, pawn, mainHandWeapon, flipped, drawSize, mainMelee, aiming, skipMainHand, mainWeaponLocation,
            mainHandAngle, mainMeleeExtra, matSingle);

        if (shouldSkipOffHand(pawn, skipOffHand))
        {
            return;
        }

        drawOffHand(mesh, pawn, offHandWeapon, flipped, drawSize, idle, offMelee, offhandWeaponLocation,
            mainWeaponLocation, mainHandAngle, offHandAngle, offMeleeExtra, matSingle, offSingle);
    }

    private static Vector3 GetCurrentWeaponLocation(Thing weapon, Vector3 cachedLocation)
    {
        var owner = ShowMeYourHandsMain.TryGetWeaponOwner(weapon);
        return owner != null ? owner.DrawPos + cachedLocation : cachedLocation;
    }

    private static float adjustMainHandAngle(Thing mainHandWeapon, Thing offHandWeapon, Pawn pawn, bool idle,
        bool flipped,
        float mainHandAngle, out bool skipMainHand, out bool mainMelee, out float mainMeleeExtra)
    {
        skipMainHand = false;
        mainMelee = false;
        mainMeleeExtra = 0f;

        if (!mainHandWeapon.def.IsMeleeWeapon)
        {
            if (flipped)
            {
                mainHandAngle -= 180f;
            }

            return mainHandAngle;
        }

        skipMainHand = ShowMeYourHandsMain.MeleeAnimationsLoaded;
        mainMelee = true;
        mainMeleeExtra = 0.0001f;

        if (idle && offHandWeapon != null)
        {
            return pawn.Rotation == Rot4.South
                ? mainHandAngle - mainHandWeapon.def.equippedAngleOffset
                : mainHandAngle + mainHandWeapon.def.equippedAngleOffset;
        }

        if (!flipped)
        {
            return mainHandAngle + mainHandWeapon.def.equippedAngleOffset;
        }

        mainHandAngle -= 180f;
        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
        return mainHandAngle;
    }

    private static float adjustOffHandAngle(Thing offHandWeapon, Pawn pawn, bool idle, bool flipped, float offHandAngle,
        out bool skipOffHand, out bool offMelee, out float offMeleeExtra)
    {
        skipOffHand = false;
        offMelee = false;
        offMeleeExtra = 0f;

        if (offHandWeapon?.def.IsMeleeWeapon != true)
        {
            if (flipped)
            {
                offHandAngle -= 180f;
            }

            return offHandAngle;
        }

        skipOffHand = ShowMeYourHandsMain.MeleeAnimationsLoaded;
        offMelee = true;
        offMeleeExtra = 0.0001f;

        if (idle && pawn.Rotation == Rot4.North)
        {
            return offHandAngle - offHandWeapon.def.equippedAngleOffset;
        }

        if (!flipped)
        {
            return offHandAngle + offHandWeapon.def.equippedAngleOffset;
        }

        offHandAngle -= 180f;
        offHandAngle -= offHandWeapon.def.equippedAngleOffset;
        return offHandAngle;
    }

    private static void ensurePawnBodySizeCached(Pawn pawn)
    {
        if (ShowMeYourHandsMain.pawnBodySizes.ContainsKey(pawn))
        {
            return;
        }

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

    private void drawMainHand(Mesh mesh, Pawn pawn, Thing mainHandWeapon, bool flipped, float drawSize, bool mainMelee,
        bool aiming, bool skipMainHand, Vector3 mainWeaponLocation, float mainHandAngle, float mainMeleeExtra,
        Material matSingle)
    {
        if (MainHand == Vector3.zero || skipMainHand)
        {
            return;
        }

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
            Quaternion.AngleAxis(mainHandAngle + MainHandRotation, Vector3.up), matSingle, 0);
    }

    private bool shouldSkipOffHand(Pawn pawn, bool skipOffHand)
    {
        if (OffHand == Vector3.zero || skipOffHand || hasCombatExtendedShieldEquipped(pawn))
        {
            return true;
        }

        return ShowMeYourHandsMain.pawnsMissingAHand.ContainsKey(pawn) && ShowMeYourHandsMain.pawnsMissingAHand[pawn];
    }

    private void drawOffHand(Mesh mesh, Pawn pawn, Thing offHandWeapon, bool flipped, float drawSize, bool idle,
        bool offMelee, Vector3 offhandWeaponLocation, Vector3 mainWeaponLocation, float mainHandAngle,
        float offHandAngle,
        float offMeleeExtra, Material matSingle, Material offSingle)
    {
        var x2 = OffHand.x * drawSize;
        var z2 = OffHand.z * drawSize;
        var y2 = OffHand.y < 0 ? -0.0001f : 0.0001f;

        if (offHandWeapon != null)
        {
            drawSize = getWeaponDrawSize(offHandWeapon);
            x2 = OffHand.x * drawSize;
            z2 = OffHand.z * drawSize;

            if (flipped)
            {
                x2 *= -1;
            }

            if (idle && !offMelee)
            {
                z2 += pawn.Rotation == Rot4.South ? 0.05f : -0.05f;
            }

            offhandWeaponLocation += adjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);

            Graphics.DrawMesh(mesh,
                offhandWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle),
                Quaternion.AngleAxis(offHandAngle + OffHandRotation, Vector3.up), matSingle, 0);
            return;
        }

        if (flipped)
        {
            x2 *= -1;
        }

        Graphics.DrawMesh(mesh,
            mainWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(mainHandAngle),
            Quaternion.AngleAxis(mainHandAngle + OffHandRotation, Vector3.up), offSingle, 0);
    }

    private static float getWeaponDrawSize(Thing weapon)
    {
        if (ShowMeYourHandsMod.instance.Settings.RepositionHands && weapon.def.graphicData != null &&
            weapon.def?.graphicData?.drawSize.x.Equals(1f) == false)
        {
            return weapon.def.graphicData.drawSize.x;
        }

        return 1f;
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


    private void drawHandsAllTheTime(Pawn pawn)
    {
        ensurePawnBodySizeCached(pawn);
        _ = HandColor;

        if (!TryGetAllTimeHandDrawData(pawn, out var mesh, out var mainSingle, out var bodySize))
        {
            return;
        }

        var sideOffset = new Vector3(0.2f, 0, 0);
        var layerOffset = new Vector3(0, 0.1f, 0);
        var heightOffset = new Vector3(0, 0, 0.7f * bodySize / 2);
        if (!TryGetAllTimeBasePosition(pawn, bodySize, heightOffset, ref sideOffset, out var basePosition))
        {
            return;
        }

        if (!DrawAllTimeMainHand(pawn, mesh, mainSingle, basePosition, sideOffset, layerOffset))
        {
            return;
        }

        if (PawnIsMissingAHand(pawn))
        {
            return;
        }

        DrawAllTimeOffHand(pawn, mesh, mainSingle, basePosition, sideOffset, layerOffset);
    }

    private static bool TryGetAllTimeHandDrawData(Pawn pawn, out Mesh mesh, out Material mainSingle,
        out float bodySize)
    {
        mesh = ShowMeYourHandsMain.GetMeshFromPawn(pawn);
        mainSingle = null;
        bodySize = ShowMeYourHandsMain.pawnBodySizes[pawn];

        var mainHandTex = ShowMeYourHandsMain.mainHandGraphics[pawn];
        if (mainHandTex == null)
        {
            return false;
        }

        mainSingle = mainHandTex.MatSingle;
        return true;
    }

    private static bool TryGetAllTimeBasePosition(Pawn pawn, float bodySize, Vector3 heightOffset,
        ref Vector3 sideOffset,
        out Vector3 basePosition)
    {
        basePosition = pawn.DrawPos - heightOffset;
        if (!pawn.Crawling)
        {
            return !pawn.Downed;
        }

        var offsetPercent = Mathf.Clamp(
            Vector3.Distance(pawn.Drawer.tweener.LastTickTweenedVelocity, Vector3.zero) * 100,
            0f, 1f);
        var offset = offsetPercent * 0.2f * bodySize;
        sideOffset += new Vector3(0.1f, 0, 0);

        if (pawn.Rotation == Rot4.West)
        {
            basePosition += new Vector3(offset, 0, 0);
        }
        else if (pawn.Rotation == Rot4.East)
        {
            basePosition -= new Vector3(offset, 0, 0);
        }
        else if (pawn.Rotation == Rot4.North)
        {
            basePosition += heightOffset * 2;
            basePosition -= new Vector3(0, 0.1f, 0);
            basePosition -= new Vector3(0, 0, offset);
        }
        else if (pawn.Rotation == Rot4.South)
        {
            basePosition -= heightOffset;
            basePosition += new Vector3(0, 0, offset);
        }

        return true;
    }

    private static bool DrawAllTimeMainHand(Pawn pawn, Mesh mesh, Material mainSingle, Vector3 basePosition,
        Vector3 sideOffset, Vector3 layerOffset)
    {
        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(mesh, basePosition + sideOffset - layerOffset, new Quaternion(), mainSingle, 0);
            return true;
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(mesh, basePosition - sideOffset + layerOffset, new Quaternion(), mainSingle, 0);
            return true;
        }

        if (pawn.Rotation == Rot4.East)
        {
            if (pawn.Crawling)
            {
                Graphics.DrawMesh(mesh, basePosition + sideOffset, new Quaternion(), mainSingle, 0);
                return true;
            }

            Graphics.DrawMesh(mesh, basePosition + layerOffset, new Quaternion(), mainSingle, 0);
            return false;
        }

        if (pawn.Rotation == Rot4.West && pawn.Crawling)
        {
            Graphics.DrawMesh(mesh, basePosition - sideOffset, new Quaternion(), mainSingle, 0);
        }

        return true;
    }

    private static bool PawnIsMissingAHand(Pawn pawn)
    {
        return hasCombatExtendedShieldEquipped(pawn) ||
               ShowMeYourHandsMain.pawnsMissingAHand.TryGetValue(pawn, out var missingAHand) && missingAHand;
    }

    private static bool hasCombatExtendedShieldEquipped(Pawn pawn)
    {
        if (!ShowMeYourHandsMain.CELoaded || pawn.apparel?.WornApparel == null)
        {
            return false;
        }

        return pawn.apparel.WornApparel.Any(apparel =>
            apparel.def?.thingClass?.FullName == "CombatExtended.Apparel_Shield");
    }

    private static void DrawAllTimeOffHand(Pawn pawn, Mesh mesh, Material mainSingle,
        Vector3 basePosition, Vector3 sideOffset, Vector3 layerOffset)
    {
        if (pawn.Rotation == Rot4.North)
        {
            Graphics.DrawMesh(mesh, basePosition - sideOffset - layerOffset, new Quaternion(), mainSingle, 0);
            return;
        }

        if (pawn.Rotation == Rot4.South)
        {
            Graphics.DrawMesh(mesh, basePosition + sideOffset + layerOffset, new Quaternion(), mainSingle, 0);
            return;
        }

        if (pawn.Crawling)
        {
            var crawlOffset = pawn.Rotation == Rot4.West ? -sideOffset * 2 : sideOffset * 2;
            Graphics.DrawMesh(mesh, basePosition + crawlOffset, new Quaternion(), mainSingle, 0);
            return;
        }

        Graphics.DrawMesh(mesh, basePosition + layerOffset, new Quaternion(), mainSingle, 0);
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


        if (mainHandTex == null)
        {
            return;
        }

        LastDrawn = GenTicks.TicksAbs;
        var matSingle = mainHandTex.MatSingle;
        var height = new Vector3(0, 0, 0.1f);
        var width = new Vector3(-0.2f, 0, 0);
        if (pawn.Rotation == Rot4.West)
        {
            height.z *= -1;
        }

        Graphics.DrawMesh(mesh,
            ItemHeldLocation + height + width, new Quaternion(), matSingle, 0);

        if (PawnIsMissingAHand(pawn))
        {
            return;
        }

        Graphics.DrawMesh(mesh,
            ItemHeldLocation + (height * -1) + (width * -1), new Quaternion(), matSingle, 0);
    }

    public override void PostDraw()
    {
        if (!tryGetDrawablePawn(out var pawn) || shouldSkipPawnPostDraw(pawn))
        {
            return;
        }

        if (tryDrawCarriedItemHands(pawn) || tryDrawWeaponHands(pawn) || shouldSkipAmbientHands(pawn))
        {
            return;
        }

        drawHandsAllTheTime(pawn);
    }

    private bool tryGetDrawablePawn(out Pawn pawn)
    {
        if (parent is Pawn { Spawned: true, Map: not null } drawnPawn)
        {
            pawn = drawnPawn;
            return true;
        }

        pawn = null;
        return false;
    }

    private static bool shouldSkipPawnPostDraw(Pawn pawn)
    {
        if (ShowMeYourHandsMod.instance.Settings.ShowHandsOnlyOnColonists && !pawn.IsPlayerControlled)
        {
            return true;
        }

        return !ShowMeYourHandsMod.instance.Settings.ShowOnRace.TryGetValue(pawn.def.defName, out var showOnRace) ||
               !showOnRace;
    }

    private bool tryDrawCarriedItemHands(Pawn pawn)
    {
        if (!ShowMeYourHandsMod.instance.Settings.ShowWhenCarry || pawn.carryTracker?.CarriedThing == null)
        {
            return false;
        }

        DrawHandsOnItem(pawn);
        return true;
    }

    private bool tryDrawWeaponHands(Pawn pawn)
    {
        if (pawn.equipment?.Primary == null)
        {
            return false;
        }

        return pawn.CurJob?.def.neverShowWeapon != true && DrawHandsOnWeapon(pawn);
    }

    private bool shouldSkipAmbientHands(Pawn pawn)
    {
        if (!ShowMeYourHandsMod.instance.Settings.ShowOtherTmes && !ShowMeYourHandsMod.instance.Settings.ShowCrawling ||
            LastDrawn >= GenTicks.TicksAbs - 1 ||
            GenTicks.TicksAbs == 0)
        {
            return true;
        }

        return ShowMeYourHandsMod.instance.Settings.ShowCrawling && !pawn.Crawling;
    }

    private static Color getHandColor(Pawn pawn, out bool hasGloves, out Color secondColor)
    {
        hasGloves = false;
        secondColor = default;

        if (pawn.story == null)
        {
            return getRaceHandColor(pawn);
        }

        var baseColor = pawn.story.SkinColor;
        var addedHands = getAddedHandsForColor(pawn);
        updateMissingHandState(pawn, addedHands);

        return !shouldUseApparelColor(pawn)
            ? getLimbOrBaseColor(baseColor, addedHands, out secondColor)
            : getApparelHandColor(pawn, ref hasGloves);
    }

    private static Color getRaceHandColor(Pawn pawn)
    {
        if (ShowMeYourHandsMain.raceDictionary.TryGetValue(pawn.def, out var color))
        {
            return color;
        }

        var lifeStageGraphic = pawn.kindDef?.lifeStages?.LastOrDefault()?.bodyGraphicData;
        var graphicData = lifeStageGraphic ?? pawn.def.graphicData;
        var texture = graphicData?.Graphic?.MatSingle?.mainTexture as Texture2D;

        ShowMeYourHandsMain.raceDictionary[pawn.def] = texture == null
            ? Color.white
            : averageColorFromTexture(texture);

        return ShowMeYourHandsMain.raceDictionary[pawn.def];
    }

    private static IEnumerable<Hediff> getAddedHandsForColor(Pawn pawn)
    {
        if (!ShowMeYourHandsMod.instance.Settings.MatchHandAmounts &&
            !ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
        {
            return [];
        }

        return pawn.health?.hediffSet?.hediffs.Where(hediff =>
            hediff is Hediff_AddedPart addedPart && ShowMeYourHandsMain.HediffContainsHand(addedPart.Part));
    }

    private static void updateMissingHandState(Pawn pawn, IEnumerable<Hediff> addedHands)
    {
        if (!ShowMeYourHandsMod.instance.Settings.MatchHandAmounts || pawn.health is not { hediffSet: not null })
        {
            return;
        }

        ShowMeYourHandsMain.pawnsMissingAHand[pawn] = pawn.health.hediffSet
                .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.HandDef) +
            addedHands?.Count() < 2;
    }

    private static bool shouldUseApparelColor(Pawn pawn)
    {
        return ShowMeYourHandsMod.instance.Settings.MatchArmorColor && (from apparel in pawn.apparel.WornApparel
            where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
            select apparel).Any();
    }

    private static Color getLimbOrBaseColor(Color baseColor, IEnumerable<Hediff> addedHands, out Color secondColor)
    {
        secondColor = default;
        if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor || addedHands == null || !addedHands.Any())
        {
            return baseColor;
        }

        var mainColor = (Color)default;
        foreach (var hediffAddedPart in addedHands.Select(hediff => hediff.def))
        {
            if (!ShowMeYourHandsMain.HediffColors.TryGetValue(hediffAddedPart, out var hediffColor))
            {
                continue;
            }

            if (mainColor == default)
            {
                mainColor = hediffColor;
                continue;
            }

            secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart];
        }

        return mainColor == default ? baseColor : mainColor;
    }

    private static Color getApparelHandColor(Pawn pawn, ref bool hasGloves)
    {
        if (pawn.apparel == null)
        {
            return pawn.story.SkinColor;
        }

        var outerApparel = getOutermostHandApparel(pawn);
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

        ShowMeYourHandsMain.colorDictionary[outerApparel] = getOuterApparelColor(outerApparel);
        return ShowMeYourHandsMain.colorDictionary[outerApparel];
    }

    private static Thing getOutermostHandApparel(Pawn pawn)
    {
        var handApparel = from apparel in pawn.apparel.WornApparel
            where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
            select apparel;

        Thing outerApparel = null;
        var highestDrawOrder = 0;
        foreach (var thing in handApparel)
        {
            var thingOutmostLayer = thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
            if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
            {
                continue;
            }

            highestDrawOrder = thingOutmostLayer;
            outerApparel = thing;
        }

        return outerApparel;
    }

    private static Color getOuterApparelColor(Thing outerApparel)
    {
        if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
        {
            return outerApparel.def.GetColorForStuff(outerApparel.Stuff);
        }

        return averageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
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