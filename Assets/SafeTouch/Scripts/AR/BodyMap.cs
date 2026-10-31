using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>One glowing dot on the body map. The lesson reads its words when the child taps it.</summary>
    public class BodyMarker : MonoBehaviour
    {
        public string id;
        public string title;
        public string text;
        public Color color;
        public bool visited;
        public GameObject glow;
        public GameObject label;
    }

    /// <summary>
    /// "My body" in lesson 1: glowing dots on the guide's body, by colour.
    ///   green  - hands and shoulders: a high-five or a pat can be okay, if the child wants it
    ///   yellow - face and hair: people ask first
    ///   red    - the parts a swimsuit covers: private
    /// Only dots on a clothed, cartoon character are shown: nothing
    /// anatomical. The dots sit on the model's bones when it has a Mixamo
    /// skeleton (so they follow the greeting wave) and at body proportions on
    /// the simple figure.
    /// </summary>
    public class BodyMap : MonoBehaviour
    {
        public struct Spot
        {
            public string id, title, text;
            public Color color;
            public Spot(string id, Color color, string title, string text)
            {
                this.id = id; this.color = color; this.title = title; this.text = text;
            }
        }

        readonly List<BodyMarker> markers = new List<BodyMarker>();

        public IList<BodyMarker> Markers { get { return markers; } }

        public bool AllVisited
        {
            get
            {
                foreach (var m in markers) if (!m.visited) return false;
                return markers.Count > 0;
            }
        }

        /// <summary>Puts a dot on the body for each spot whose part can be found. Spots: head, shoulders, hands, chest, private.</summary>
        public void Show(IEnumerable<Spot> spots)
        {
            Hide();
            var bounds = BodyBounds();
            float h = Mathf.Max(bounds.size.y, 0.1f);
            foreach (var spot in spots)
            {
                foreach (var place in Places(spot.id, bounds, h))
                    markers.Add(Marker(spot, place.Key, place.Value, h));
            }
        }

        public void Hide()
        {
            foreach (var m in markers)
            {
                if (m == null) continue;
                if (m.label != null) Destroy(m.label);   // labels hang on the body, not on the dot
                Destroy(m.gameObject);
            }
            markers.Clear();
        }

        /// <summary>Marks a dot as seen: it stops pulsing and shows its words in a card beside it.</summary>
        public void Visit(BodyMarker marker)
        {
            foreach (var m in markers)
                if (m.label != null && m != marker) { Destroy(m.label); m.label = null; }
            if (marker.label != null) Destroy(marker.label);
            float h = Mathf.Max(BodyBounds().size.y, 0.1f);
            var side = Vector3.ProjectOnPlane(Camera.main != null ? Camera.main.transform.right : transform.right, Vector3.up).normalized;
            marker.label = ARFx.Label(marker.title, marker.transform.position + side * h * 0.32f + Vector3.up * h * 0.04f, marker.color, Mathf.Clamp(h / 0.55f, 0.8f, 2.2f));
            marker.label.transform.SetParent(transform, true);
            if (!marker.visited)
            {
                marker.visited = true;
                var pulse = marker.glow.GetComponent<FxAnimator>();
                if (pulse != null) Destroy(pulse);
            }
            ARFx.Burst(marker.transform.position, marker.color, 8);
        }

        BodyMarker Marker(Spot spot, Transform parent, Vector3 worldPosition, float height)
        {
            var root = new GameObject("Body dot " + spot.id);
            root.transform.position = worldPosition;
            root.transform.SetParent(parent, true);
            root.AddComponent<Billboard>();
            float size = height * 0.075f;
            var collider = root.AddComponent<SphereCollider>();
            collider.radius = size * 0.9f / Mathf.Max(root.transform.lossyScale.x, 0.0001f);   // a generous target for small fingers

            var glow = Quad(root.transform, "Glow", ARFx.SoftDot, WithAlpha(spot.color, 0.85f), size * 2.2f);
            Quad(root.transform, "Dot", ARFx.SoftDot, Color.white, size * 0.7f);
            FxAnimator.Pulse(glow);

            var marker = root.AddComponent<BodyMarker>();
            marker.id = spot.id;
            marker.title = spot.title;
            marker.text = spot.text;
            marker.color = spot.color;
            marker.glow = glow;
            return marker;
        }

        static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static GameObject Quad(Transform parent, string name, Texture texture, Color color, float worldSize)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = Vector3.one * worldSize / Mathf.Max(parent.lossyScale.x, 0.0001f);
            var m = ARFx.Unlit(color, texture);
            m.renderQueue = 3100;   // over the body
            quad.GetComponent<Renderer>().sharedMaterial = m;
            return quad;
        }

        /// <summary>Where each spot goes: (parent to follow, world position). Two places for hands and shoulders.</summary>
        IEnumerable<KeyValuePair<Transform, Vector3>> Places(string id, Bounds b, float h)
        {
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward == Vector3.zero) forward = Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            float front = h * 0.09f;   // dots sit just in front of the body, towards where the guide looks
            var feet = new Vector3(b.center.x, b.min.y, b.center.z);
            Transform bone;
            switch (id)
            {
                case "head":
                    bone = Bone("Head");
                    yield return Pair(bone, bone != null ? bone.position + Vector3.up * h * 0.05f + forward * front : feet + Vector3.up * h * 0.88f + forward * front);
                    break;
                case "chest":
                    bone = Bone("Spine2") ?? Bone("Spine1");
                    yield return Pair(bone, bone != null ? bone.position + forward * front : feet + Vector3.up * h * 0.66f + forward * front);
                    break;
                case "private":
                    bone = Bone("Hips");
                    yield return Pair(bone, bone != null ? bone.position - Vector3.up * h * 0.06f + forward * front : feet + Vector3.up * h * 0.46f + forward * front);
                    break;
                case "back":
                    // Behind the chest: seen when the child turns the guide around.
                    bone = Bone("Spine2") ?? Bone("Spine1");
                    yield return Pair(bone, bone != null ? bone.position - forward * front : feet + Vector3.up * h * 0.66f - forward * front);
                    break;
                case "bottom":
                    bone = Bone("Hips");
                    yield return Pair(bone, bone != null ? bone.position - Vector3.up * h * 0.03f - forward * front : feet + Vector3.up * h * 0.48f - forward * front);
                    break;
                case "shoulders":
                    foreach (var side in new[] { "Left", "Right" })
                    {
                        bone = Bone(side + "Arm") ?? Bone(side + "Shoulder");
                        float s = side == "Left" ? -1f : 1f;
                        yield return Pair(bone, bone != null ? bone.position + Vector3.up * h * 0.02f + forward * front * 0.5f
                            : feet + Vector3.up * h * 0.76f + right * s * b.extents.x * 0.8f + forward * front * 0.5f);
                    }
                    break;
                case "hands":
                    foreach (var side in new[] { "Left", "Right" })
                    {
                        bone = Bone(side + "Hand");
                        float s = side == "Left" ? -1f : 1f;
                        if (bone == null && !HasArms()) continue;   // the simple figure has no hands
                        yield return Pair(bone, bone != null ? bone.position + forward * front * 0.3f
                            : feet + Vector3.up * h * 0.48f + right * s * b.extents.x + forward * front * 0.3f);
                    }
                    break;
            }
        }

        KeyValuePair<Transform, Vector3> Pair(Transform bone, Vector3 position)
        {
            return new KeyValuePair<Transform, Vector3>(bone != null ? bone : transform, position);
        }

        bool HasArms()
        {
            return Bone("LeftHand") != null;
        }

        /// <summary>A Mixamo bone such as "mixamorig:LeftHand", found by the end of its name.</summary>
        Transform Bone(string part)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var n = t.name;
                int colon = n.LastIndexOf(':');
                if (colon >= 0) n = n.Substring(colon + 1);
                if (n == part || n == "mixamorig" + part) return t;
            }
            return null;
        }

        /// <summary>The body's size in the room, ignoring labels, shadows, discs and dots.</summary>
        Bounds BodyBounds()
        {
            var b = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponent<TextMesh>() != null || r.GetComponentInParent<BodyMarker>() != null) continue;
                if (r.name == "Contact shadow" || r.name.EndsWith("zone") || r.name == "Safe place" || r.bounds.size.y < 0.02f) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            if (!any) b = new Bounds(transform.position + Vector3.up * 0.3f, new Vector3(0.2f, 0.6f, 0.2f));
            return b;
        }
    }
}
