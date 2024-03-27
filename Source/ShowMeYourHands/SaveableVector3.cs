using System;
using System.Globalization;
using UnityEngine;

namespace ShowMeYourHands;

internal class SaveableVector3
{
    private SaveableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SaveableVector3(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }

    private float x { get; }

    private float y { get; }

    private float z { get; }

    public override string ToString()
    {
        return string.Format("({0:F3}, {1:F3}, {2:F3})", [
            x,
            y,
            z
        ]);
    }

    public static SaveableVector3 FromString(string Str)
    {
        Str = Str.TrimStart('(');
        Str = Str.TrimEnd(')');
        var array = Str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        var x = Convert.ToSingle(array[0], invariantCulture);
        var y = Convert.ToSingle(array[1], invariantCulture);
        var z = Convert.ToSingle(array[2], invariantCulture);
        return new SaveableVector3(x, y, z);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}