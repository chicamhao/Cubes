using System;
using UnityEngine;
using static MathUtility;

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
