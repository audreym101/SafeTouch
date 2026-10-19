using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// The square that shows where Keza will stand: four corner brackets and
    /// a centre dot lying flat on the floor or table. It glides after the
    /// raycast hit instead of snapping (raycasts jump a few centimetres every
    /// frame), always turns to face the child, and gently pulses green when
    /// Keza can be placed; white means "keep scanning".
    /// </summary>
    public class PlacementReticle : MonoBehaviour
    {
        [Tooltip("Unlit transparent material (Sprites/Default); tinted at runtime.")]
        [SerializeField] Material material;
        [SerializeField] float size = 0.3f;           // metres across
        [SerializeField] float followSpeed = 14f;

        static readonly Color Ready = new Color(0.231f, 0.655f, 0.463f);   // Theme.Safe
        static readonly Color Waiting = new Color(1f, 1f, 1f);

        Material tint;
        Renderer shape;
        bool shown;

        void Awake()
        {
            var go = new GameObject("Square", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = BuildMesh();
            shape = go.GetComponent<MeshRenderer>();
            tint = material != null ? new Material(material) : new Material(Shader.Find("Sprites/Default"));
            tint.renderQueue = 3100;   // above the surface grid
            shape.sharedMaterial = tint;
            shape.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // No SetActive(false) here: the scene keeps the reticle inactive, so Awake first
            // runs inside Show's SetActive(true), and switching it off again would hide it.
        }

        /// <summary>Moves the square towards a hit on a surface. canPlace turns it green.</summary>
        public void Show(Pose hit, Vector3 cameraPosition, bool canPlace)
        {
            var up = hit.rotation * Vector3.up;
            var toCamera = Vector3.ProjectOnPlane(cameraPosition - hit.position, up);
            var facing = toCamera.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toCamera, up) : hit.rotation;

            if (!shown)
            {
                transform.SetPositionAndRotation(hit.position, facing);   // first sighting: no glide from far away
                shown = true;
                gameObject.SetActive(true);
            }
            float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, hit.position, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, k);

            float pulse = canPlace ? 1f + 0.06f * Mathf.Sin(Time.time * 5f) : 1f;
            transform.localScale = Vector3.one * pulse;
            var c = canPlace ? Ready : Waiting;
            c.a = canPlace ? 0.95f : 0.7f;
            tint.color = c;
        }

        public void Hide()
        {
            shown = false;
            gameObject.SetActive(false);
        }

        /// <summary>Four L-shaped corner brackets and a small centre square, flat in the XZ plane.</summary>
        Mesh BuildMesh()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            float h = size * 0.5f, arm = size * 0.3f, t = size * 0.075f;

            System.Action<float, float, float, float> rect = (x0, z0, x1, z1) =>
            {
                int b = verts.Count;
                verts.Add(new Vector3(x0, 0f, z0)); verts.Add(new Vector3(x0, 0f, z1));
                verts.Add(new Vector3(x1, 0f, z1)); verts.Add(new Vector3(x1, 0f, z0));
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            };
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                // One arm along x and one along z from each corner.
                rect(Mathf.Min(sx * h, sx * (h - arm)), Mathf.Min(sz * h, sz * (h - t)), Mathf.Max(sx * h, sx * (h - arm)), Mathf.Max(sz * h, sz * (h - t)));
                rect(Mathf.Min(sx * h, sx * (h - t)), Mathf.Min(sz * h, sz * (h - arm)), Mathf.Max(sx * h, sx * (h - t)), Mathf.Max(sz * h, sz * (h - arm)));
            }
            float d = size * 0.04f;
            rect(-d, -d, d, d);

            var mesh = new Mesh { name = "Placement square" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, new Vector2[verts.Count]);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy()
        {
            if (tint != null) Destroy(tint);
        }
    }
}
