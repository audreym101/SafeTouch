using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SafeTouch.AR
{
    /// <summary>
    /// Draws a detected floor or table as a rectangle: a soft 10 cm grid
    /// inside a bright frame, so the child can see where Keza can stand.
    /// ARCore keeps refining a surface's outline, which makes a raw polygon
    /// jitter; the rectangle instead glides towards each new size and
    /// position, and fades in rather than popping. It is blue while the
    /// surface is still too small for Keza's bubble and turns green once it
    /// is big enough (see ARManager.MinSurfaceSide).
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    public class PlaneVisual : MonoBehaviour
    {
        /// <summary>Hides every surface once Keza is placed, and shows them again for "Place again".</summary>
        public static bool ShowSurfaces = true;

        [Tooltip("Unlit transparent material (Sprites/Default); tinted and textured at runtime.")]
        [SerializeField] Material material;
        [SerializeField] float gridCell = 0.1f;       // metres per grid square
        [SerializeField] float frameWidth = 0.016f;   // metres
        [SerializeField] float smoothing = 6f;        // higher follows ARCore's estimate faster
        [Tooltip("Surfaces smaller than this are not drawn yet, so tiny flickering patches stay hidden.")]
        [SerializeField] float minVisibleSide = 0.15f;

        static readonly Color Small = new Color(0.141f, 0.455f, 0.776f);   // Theme.Primary
        static readonly Color Big = new Color(0.231f, 0.655f, 0.463f);     // Theme.Safe
        static Texture2D gridTexture;

        ARPlane plane;
        Mesh gridMesh, frameMesh;
        Material gridMaterial, frameMaterial;
        MeshRenderer gridRenderer, frameRenderer;
        Vector2 shownSize, shownCentre;
        float alpha;
        bool started;

        /// <summary>Set by ARManager on the surface under the reticle, which is drawn a little brighter.</summary>
        public bool Highlighted { get; set; }

        void Awake()
        {
            plane = GetComponent<ARPlane>();
            gridMesh = new Mesh { name = "Surface grid" };
            frameMesh = new Mesh { name = "Surface frame" };
            gridMaterial = MakeMaterial();
            gridMaterial.mainTexture = GridTexture;
            frameMaterial = MakeMaterial();
            gridRenderer = Child("Grid", gridMesh, gridMaterial);
            frameRenderer = Child("Frame", frameMesh, frameMaterial);
        }

        Material MakeMaterial()
        {
            var m = material != null ? new Material(material) : new Material(Shader.Find("Sprites/Default"));
            m.renderQueue = 3000;
            return m;
        }

        MeshRenderer Child(string childName, Mesh mesh, Material mat)
        {
            var go = new GameObject(childName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.002f, 0f);   // just above the real surface
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>The surface is a floor or table (facing up), tracked and not merged into a bigger one.</summary>
        bool Usable
        {
            get
            {
                return plane.alignment == PlaneAlignment.HorizontalUp
                    && plane.trackingState != TrackingState.None
                    && plane.subsumedBy == null;
            }
        }

        void Update()
        {
            var size = plane.size;
            bool show = ShowSurfaces && Usable && Mathf.Max(size.x, size.y) >= minVisibleSide;

            // Ease towards ARCore's latest estimate instead of jumping with it.
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            if (!started && show)
            {
                shownSize = size;
                shownCentre = plane.centerInPlaneSpace;
                started = true;
            }
            shownSize = Vector2.Lerp(shownSize, size, k);
            shownCentre = Vector2.Lerp(shownCentre, plane.centerInPlaneSpace, k);
            alpha = Mathf.Lerp(alpha, show ? (Highlighted ? 1f : 0.65f) : 0f, 1f - Mathf.Exp(-4f * Time.deltaTime));

            bool visible = alpha > 0.01f;
            gridMaterial.color = Colour(0.6f);
            frameMaterial.color = Colour(0.95f);
            gridRenderer.enabled = frameRenderer.enabled = visible;
            if (visible) Rebuild();
        }

        Color Colour(float opacity)
        {
            bool bigEnough = shownSize.x >= ARManager.MinSurfaceSide && shownSize.y >= ARManager.MinSurfaceSide;
            var c = bigEnough ? Big : Small;
            c.a = opacity * alpha;
            return c;
        }

        void Rebuild()
        {
            float hx = shownSize.x * 0.5f, hz = shownSize.y * 0.5f;
            float cx = shownCentre.x, cz = shownCentre.y;

            // The grid: one quad, its texture repeated once per 10 cm.
            float u = shownSize.x / gridCell, v = shownSize.y / gridCell;
            gridMesh.Clear();
            gridMesh.vertices = new[]
            {
                new Vector3(cx - hx, 0f, cz - hz), new Vector3(cx - hx, 0f, cz + hz),
                new Vector3(cx + hx, 0f, cz + hz), new Vector3(cx + hx, 0f, cz - hz)
            };
            gridMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, v), new Vector2(u, v), new Vector2(u, 0f) };
            gridMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            gridMesh.RecalculateBounds();

            // The frame: a ring of four strips along the edges.
            float w = Mathf.Min(frameWidth, Mathf.Min(hx, hz));
            var outer = new[] { new Vector3(cx - hx, 0f, cz - hz), new Vector3(cx - hx, 0f, cz + hz), new Vector3(cx + hx, 0f, cz + hz), new Vector3(cx + hx, 0f, cz - hz) };
            var inner = new[] { new Vector3(cx - hx + w, 0f, cz - hz + w), new Vector3(cx - hx + w, 0f, cz + hz - w), new Vector3(cx + hx - w, 0f, cz + hz - w), new Vector3(cx + hx - w, 0f, cz - hz + w) };
            var verts = new Vector3[8];
            var tris = new int[24];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = outer[i];
                verts[i + 4] = inner[i];
                int n = (i + 1) % 4, t = i * 6;
                tris[t] = i; tris[t + 1] = n; tris[t + 2] = n + 4;
                tris[t + 3] = i; tris[t + 4] = n + 4; tris[t + 5] = i + 4;
            }
            frameMesh.Clear();
            frameMesh.vertices = verts;
            frameMesh.uv = new Vector2[8];
            frameMesh.triangles = tris;
            frameMesh.RecalculateBounds();
        }

        /// <summary>One grid square: a faint fill with brighter lines on two edges, so tiles join into a grid.</summary>
        static Texture2D GridTexture
        {
            get
            {
                if (gridTexture != null) return gridTexture;
                const int size = 64;
                gridTexture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Surface grid", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool line = x < 2 || y < 2;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(line ? 230 : 70));
                }
                gridTexture.SetPixels32(px);
                gridTexture.Apply(true);
                return gridTexture;
            }
        }

        void OnDestroy()
        {
            if (gridMesh != null) Destroy(gridMesh);
            if (frameMesh != null) Destroy(frameMesh);
            if (gridMaterial != null) Destroy(gridMaterial);
            if (frameMaterial != null) Destroy(frameMaterial);
        }
    }
}
