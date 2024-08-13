using UnityEngine;
using static UnityEngine.Mathf;

public static class MathUtility
{
    public delegate Vector3 GraphFunc(float u, float v, float t);
    public enum GraphType
    {
        Wave,
        MorphingWave,
        Ripple,
        TwistedSphere,
        Torus
    }
    static readonly GraphFunc[] _funcs = { Wave, MorphingWave, Ripple, TwistedSphere, Torus };

    public static GraphFunc GetGraphFunc(GraphType type) => _funcs[(int)type];

    public static GraphType GetNextGraphFrom(GraphType current)
    {
        return (GraphType)Repeat((float)current + 1, _funcs.Length - 1);
    }

    public static GraphType GetRandomFrom(GraphType current)
    {
        var choice = current;
        while (choice == current)
        {
            choice = (GraphType)Random.Range(0, _funcs.Length);
        }
        return choice;
    }

    public static Vector3 Morph(float u, float v, float t, GraphFunc from, GraphFunc to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    private static Vector3 Wave(float u, float v, float t)
    {
        return new Vector3(u, Sin(PI * (u + v + t)), v);
    }

    private static Vector3 MorphingWave(float u, float v, float t)
    {
        var y = Sin((u + 0.5f * t) * PI);
        y += 0.5f * Sin(2f * PI * (v + t));
        y += Sin(PI * (u + v + 0.25f * t));
        y *= 1f / 2.5f;
        return new Vector3(u, y, v);
    }

    private static Vector3 Ripple(float u, float v, float t)
    {
        var d = Sqrt(u * u + v * v);
        var y = Sin(PI * (4f * d - t)); // b: period, t: horizontal shift
        y /= 1f + 10f * d; // decreases with distance because a ripple doesn't have a fixed amplitude
        return new Vector3(u, y, v);
    }

    private static Vector3 TwistedSphere(float u, float v, float t)
    {
        var r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
        var s = r * Cos(0.5f * PI * v);
        return new Vector3(
            s * Sin(PI * u),
            r * Sin(0.5f * PI * v),
            s * Cos(PI * u)
            );
    }

    private static Vector3 Torus(float u, float v, float t)
    {
        var r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        var r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        var s = r1 + r2 * Cos(PI * v);
        return new Vector3(
            s * Sin(PI * u),
            r2 * Sin(PI * u),
            s * Cos(PI * u)
            );
    }
}

