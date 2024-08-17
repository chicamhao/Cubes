using System;
using UnityEngine;
using static MathUtility;

public sealed class GPUGraph : MonoBehaviour
{
    enum TransitionMode { Cycle, Random };
    [SerializeField] TransitionMode _mode;
    [SerializeField] GraphType _currentGraph;

    const int _maxResolution = 1000;
    [SerializeField, Range(10, _maxResolution)] int _resolution;
    [SerializeField, Min(0f)] float _functionDuration = 1f;
    [SerializeField, Min(0f)] float _transitionDuration = 1f;

    [SerializeField] ComputeShader _computeShader;
    [SerializeField] Material _material;
    [SerializeField] Mesh _mesh;

    static readonly int _positionsId = Shader.PropertyToID("_Positions");
    static readonly int _resolutionId = Shader.PropertyToID("_Resolution");
    static readonly int _stepId = Shader.PropertyToID("_Step");
    static readonly int _timeId = Shader.PropertyToID("_Time");
    static readonly int _transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    GraphType _transitionGraph;
    float _duration;
    bool _transiting;

    ComputeBuffer _positionsBuffer;

    private void OnEnable()
    {
        _positionsBuffer = new ComputeBuffer(_maxResolution * _maxResolution, 3 * 4);
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
        _computeShader.SetInt(_resolutionId, _resolution);
        _computeShader.SetFloat(_stepId, step);
        _computeShader.SetFloat(_timeId, Time.time);

        if (_transiting) // morph
        {
            _computeShader.SetFloat(
                _transitionProgressId,
                Mathf.SmoothStep(0f, 1f, _duration / _transitionDuration)
            );
        }

        // which dones't copy and data but links the buffer to the kernel
        var kernelIndex = (int)_currentGraph
            + (int)(_transiting ? _transitionGraph : _currentGraph) * GraphTypeCount;
        _computeShader.SetBuffer(kernelIndex, _positionsId, _positionsBuffer);

        // fixed 8x8 group size the amouth of groups
        var groups = Mathf.CeilToInt(_resolution / 8f);
        _computeShader.Dispatch(kernelIndex, groups, groups, 1);

        _material.SetBuffer(_positionsId, _positionsBuffer);
        _material.SetFloat(_stepId, step);

        // spatial bounds of points should remain inside a cube with size 2,
        // but points have a size as well, half of which could poke outside the bounds in all directions.
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + step)); // frustum culling included here.
        Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, _resolution * _resolution);
    }
}