using System;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using static MathUtility;

public sealed class GPUGraph : MonoBehaviour
{
    enum TransitionMode { Cycle, Random };
    [SerializeField] TransitionMode _mode;
    [SerializeField] GraphType _currentGraph;

    [SerializeField, Range(10, 200)] int _resolution;
    [SerializeField, Min(0f)] float _functionDuration = 1f;
    [SerializeField, Min(0f)] float _transitionDuration = 1f;

    [SerializeField] ComputeShader _computerShader;
    [SerializeField] Material _material;
    [SerializeField] Mesh _mesh;

    static readonly int _positionsId = Shader.PropertyToID("_Positions");
    static readonly int _resolutionId = Shader.PropertyToID("_Resolution");
    static readonly int _stepId = Shader.PropertyToID("_Step");
    static readonly int _timeId = Shader.PropertyToID("_Time");

    GraphType _transitionGraph;
    float _duration;
    bool _transiting;

    ComputeBuffer _positionsBuffer;

    private void OnEnable()
    {
        _positionsBuffer = new ComputeBuffer(_resolution * _resolution, 3 * 4);
    }

    private void OnDisable()
    {
        _positionsBuffer.Release();
        _positionsBuffer = null;
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

        UpdateGraphOnGPU();
    }

    private void UpdateGraphOnGPU()
    {
        var step = 2f / _resolution; // point size/unit scale
        _computerShader.SetInt(_resolutionId, _resolution);
        _computerShader.SetFloat(_stepId, step);
        _computerShader.SetFloat(_timeId, Time.time);

        // which dnes't copy and data but links the buffer to the kernel
        _computerShader.SetBuffer(0, _positionsId, _positionsBuffer);

        // fixed 8x8 group size the amouth of groups
        var groups = Mathf.CeilToInt(_resolution / 8f);
        _computerShader.Dispatch(0, groups, groups, 1);

        _material.SetBuffer(_positionsId, _positionsBuffer);
        _material.SetFloat(_stepId, step);

        // spatial bounds of points should remain inside a cube with size 2,
        // but points have a size as well, half of which could poke outside the bounds in all directions.
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + step)); // frustum culling included here.
        Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, _positionsBuffer.count);
    }
}