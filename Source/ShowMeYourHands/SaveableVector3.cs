using System;
using System.Globalization;
using UnityEngine;

namespace ShowMeYourHands;

internal class SaveableVector3
{
    private SaveableVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public SaveableVector3(Vector3 vector3)
    {
        X = vector3.x;
        Y = vector3.y;
        Z = vector3.z;
    }

    private float X { get; }

    private float Y { get; }

    private float Z { get; }

    public override string ToString()
    {
        return string.Format("({0:F3}, {1:F3}, {2:F3})", [
            X,
            Y,
            Z
        ]);
    }

    public static SaveableVector3 FromString(string str)
    {
        str = str.TrimStart('(');
        str = str.TrimEnd(')');
        var array = str.Split(',');
        var invariantCulture = CultureInfo.InvariantCulture;
        var x = Convert.ToSingle(array[0], invariantCulture);
        var y = Convert.ToSingle(array[1], invariantCulture);
        var z = Convert.ToSingle(array[2], invariantCulture);
        return new SaveableVector3(x, y, z);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }
}