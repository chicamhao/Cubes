using UnityEngine;

public sealed class Fractal : MonoBehaviour
{
    [SerializeField] Mesh _mesh;
    [SerializeField] Material _material;

    struct FractalPart
    {
        public Vector3 Direction;
        public Vector3 WorldPosition;

        public Quaternion Rotation;
        public Quaternion WorldRotation;
        public float SpinAngle;
    }

    static readonly Vector3[] _directions =
    {
        Vector3.up, Vector3.right, Vector3.left,
        Vector3.forward, Vector3.back
    };

    // child's orientation is thus also relative to their parent.
    // To a child its parent is the ground,
    // which makes the direction of its offset equal to its local up axis.
    static readonly Quaternion[] _rotations =
    {
        Quaternion.identity, Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };


    [SerializeField, Range(1, 8)] byte _depth = 4;

    private FractalPart[][] _parts;
    Matrix4x4[][] _matrices;
    ComputeBuffer[] _matricesBuffers;
    static readonly int _matricesId = Shader.PropertyToID("_Matrices");

    // for drawing multiple objects with the same material, but slightly diff properties
    static MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        _propertyBlock ??= new();

        _parts = new FractalPart[_depth][];
        _matrices = new Matrix4x4[_depth][];
        _matricesBuffers = new ComputeBuffer[_depth];
        var stride = 16 * 4; // 4x4 = 16 floats

        for (int i = 0, length = 1 ; i < _parts.Length; i++, length *= 5)
        {
            _parts[i] = new FractalPart[length];
            _matrices[i] = new Matrix4x4[length];
            _matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        // root
        var rootPart = CreatePart(0);
        _parts[0][0] = rootPart;

        // init part's directions and rotations
        for (var i = 1; i < _parts.Length; i++) // level 
        {
            for(var j = 0; j < _parts[i].Length; j += 5) // child
            {
                for (var z = 0; z < 5; z++)
                {
                    _parts[i][j + z] = CreatePart(z);
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach(var m in _matricesBuffers)
        {
            m.Release();
        }

        _parts = null;
        _matrices = null;
        _matricesBuffers = null;
    }

    private void OnValidate()
    {
        if (_parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private FractalPart CreatePart(int childIndex)
    {
        return new FractalPart()
        {
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex],
        };
    }

    private void Update()
    {
        var spinAngleDelta = 22.5f * Time.deltaTime;

        // calculate root transform matrix
        var root = _parts[0][0];
        root.SpinAngle += spinAngleDelta;
        root.WorldRotation = transform.rotation * (root.Rotation * Quaternion.Euler(0f, root.SpinAngle, 0f));
        root.WorldPosition = transform.position;
        _parts[0][0] = root;

        float objectScale = transform.lossyScale.x;
        _matrices[0][0] = Matrix4x4.TRS(
            root.WorldPosition, root.WorldRotation, Vector3.one);

        // caculate parts transform matrices
        float scale = objectScale;
        for (var i = 1; i < _parts.Length; i++) // level 
        {
            scale *= 0.5f;
            for (var j = 0; j < _parts[i].Length; j ++) // child
            {
                var parentTransform = _parts[i - 1][j / 5];
                var part = _parts[i][j];

                // animation
                part.SpinAngle += spinAngleDelta;

                // transform
                part.WorldRotation = 
                    // rotation be stacked via multication of quaternions
                    parentTransform.WorldRotation * (part.Rotation * Quaternion.Euler(0f, part.SpinAngle, 0f));

                // part position relaive to its designated parent
                part.WorldPosition = parentTransform.WorldPosition +
                    // since rotation also affect the direction of its offset
                    parentTransform.WorldRotation * (1.5f * scale * part.Direction); 

                _parts[i][j] = part;

                _matrices[i][j] = Matrix4x4.TRS(
                    part.WorldPosition, part.WorldRotation, scale * Vector3.one);

            }
        }

        // upload the matrices to GPU
        var bounds = new Bounds(root.WorldPosition, 3f * objectScale * Vector3.one);
        for (var i = 0; i < _matricesBuffers.Length; i++) // level 
        {
            var buffer = _matricesBuffers[i];
            buffer.SetData(_matrices[i]);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, buffer.count, _propertyBlock);
        }
    }
}
