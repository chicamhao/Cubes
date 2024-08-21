using UnityEngine;

public sealed class Fractal : MonoBehaviour
{
    [SerializeField] Mesh _mesh;
    [SerializeField] Material _material;

    struct FractalPart
    {
        public Vector3 Direction;
        public Quaternion Rotation;
        public Transform Transform;
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


    private void Awake()
    {
        // initialize.
        _parts = new FractalPart[_depth][];

        var length = 1;
        for (var i = 0; i < _parts.Length; i++)
        {
            _parts[i] = new FractalPart[length];
            length *= 5;
        }

        // root
        var scale = 1f;
        _parts[0][0] = CreatePart(0, 0, scale);

        for (var i = 1; i < _parts.Length; i++) // level 
        {
            scale *= 0.5f;
            for(var j = 0; j < _parts[i].Length; j += 5) // child
            {
                for (var z = 0; z < 5; z++)
                {
                    _parts[i][j + z] = CreatePart(i, z, scale);
                }
            }
        }
    }

    private FractalPart CreatePart(int levelIndex, int childIndex, float scale)
    {
        var go = new GameObject("Fractal part " + levelIndex + " C" + childIndex);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * scale;

        go.AddComponent<MeshFilter>().mesh = _mesh;
        go.AddComponent<MeshRenderer>().material = _material;

        return new FractalPart()
        {
            Direction = _directions[childIndex],
            Rotation = _rotations[childIndex],
            Transform = go.transform
        };
    }

    private void Update()
    {
        var deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);

        var root = _parts[0][0];
        root.Rotation *= deltaRotation;
        root.Transform.localRotation = root.Rotation;
        _parts[0][0] = root;

        for (var i = 1; i < _parts.Length; i++) // level 
        {
            for (var j = 0; j < _parts[i].Length; j ++) // child
            {
                var parentTransform = _parts[i - 1][j / 5].Transform;
                var part = _parts[i][j];

                // animation
                part.Rotation *= deltaRotation;

                // transform
                part.Transform.SetLocalPositionAndRotation(
                    // part position relaive to its designated parent
                    parentTransform.localPosition +
                    parentTransform.transform.localRotation * // since rotation also affect the direction of its offset
                        (1.5f * part.Transform.localScale.x * part.Direction),
                    // rotation be stacked via multication of quaternions
                    parentTransform.localRotation * part.Rotation);

                _parts[i][j] = part;
            }
        }
    }
}
