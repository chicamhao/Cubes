using System;
using UnityEngine;
using static MathUtility;
using static UnityEngine.Mathf;

public sealed class Graph : MonoBehaviour
{
    [SerializeField] Transform _pointPrefab;

    enum TransitionMode { Cycle, Random };
    [SerializeField] TransitionMode _mode;
    [SerializeField] GraphType _currentGraph;

    [SerializeField, Range(10, 100)] int _resolution;
    [SerializeField, Min(0f)] float _functionDuration = 1f;
    [SerializeField, Min(0f)] float _transitionDuration = 1f;

    GraphType _transitionGraph;
    Transform[] _points;
    float _duration;
    bool _transiting;

    private void Awake()
    {
        var step = 2f / _resolution;
        var scale = Vector3.one * step;
        _points = new Transform[_resolution * _resolution];

        for (var i = 0; i < _points.Length; i++)
        {
            var point = _points[i] = Instantiate(_pointPrefab);
            point.localScale = scale;
            point.SetParent(transform, false);
        }
    }

    private void Update()
    {
        _duration += Time.deltaTime;
        if (_transiting)
        {
            if (_duration >= _transitionDuration)
            {
                _duration -= _transitionDuration;
                _transiting = false;
            }
        }
        else if (_duration >= _functionDuration)
        {
            _duration -= _functionDuration;
            _transiting = true;

            _transitionGraph = _currentGraph;
            _currentGraph = _mode switch
            {
                TransitionMode.Cycle => GetNextGraphFrom(_currentGraph),
                TransitionMode.Random => GetRandomFrom(_currentGraph),
                _ => throw new NotImplementedException()
            };
        }

        if (_transiting)
        {
            UpdateTransition();
        }
        else
        {
            UpdateGraph();
        }

    }

    private void UpdateGraph()
    {
        var f = GetGraphFunc(_currentGraph);
        var time = Time.time;
        var step = 2f / _resolution;
        var v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < _points.Length; i++, x++)
        {
            if (x == _resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1f;
            }
            var u = (x + 0.5f) * step - 1f;
            _points[i].localPosition = f(u, v, time);
        }
    }

    private void UpdateTransition()
    {
        var from = GetGraphFunc(_transitionGraph);
        var to = GetGraphFunc(_currentGraph);
        var progress = _duration / _transitionDuration;
        var time = Time.time;
        var step = 2f / _resolution;
        var v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < _points.Length; i++, x++)
        {
            if (x == _resolution)
            {
                x = 0;
                z ++;
                v = (z + 0.5f) * step - 1f;
            }

            var u = (x + 0.5f) * step - 1f;
            _points[i].localPosition = Morph(u, v, time, from, to, progress);
        }
    }
}

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
            choice = (GraphType)UnityEngine.Random.Range(0, _funcs.Length);
        }
        return choice;
    }

    public static Vector3 Morph(float u, float v, float t, GraphFunc from, GraphFunc to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    private static Vector3 Wave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + v + t));
        p.z = v;
        return p;
        //return new Vector3(u, Sin(PI * (u + v + t)), v);
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
