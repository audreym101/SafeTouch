using System.Collections;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Small effects that make the AR scene feel real and answer every touch:
    /// soft contact shadows that sit characters on the real floor, a ripple
    /// where the child taps, a sparkle burst, and floating label cards that
    /// face the phone. All are generated at runtime (no texture assets) and
    /// use the unlit "Sprites/Default" shader, which every build includes.
    /// </summary>
    public static class ARFx
    {
        static Texture2D softDot, ring;
        static Font font;

        /// <summary>A white dot that fades to clear at the edge (shadows, glows, sparkles).</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot != null) return softDot;
                softDot = Radial(64, d => Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f));
                return softDot;
            }
        }

        /// <summary>A thin bright ring with soft edges (tap ripples, the "tap here" pulse).</summary>
        public static Texture2D Ring
        {
            get
            {
                if (ring != null) return ring;
                ring = Radial(128, d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.12f));
                return ring;
            }
        }

        static Texture2D Radial(int size, System.Func<float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r)) / r;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha(d) * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>An unlit, see-through material in the given colour.</summary>
        public static Material Unlit(Color color, Texture texture = null)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.color = color;
            if (texture != null) m.mainTexture = texture;
            m.renderQueue = 3050;   // after the surface grid, before text
            return m;
        }

        /// <summary>A flat textured quad lying on the floor.</summary>
        public static GameObject FloorQuad(string name, Texture texture, Color color, float diameter)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * diameter;
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = Unlit(color, texture);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return quad;
        }

        /// <summary>
        /// A soft dark patch under a character's feet, so they look like they
        /// stand on the real floor instead of floating over the camera picture.
        /// </summary>
        public static void AddShadow(Transform figure, float diameter)
        {
            if (figure.Find("Contact shadow") != null) return;
            var shadow = FloorQuad("Contact shadow", SoftDot, new Color(0f, 0f, 0f, 0.38f), 1f);
            shadow.transform.SetParent(figure, false);
            float s = Mathf.Max(figure.lossyScale.y, 0.0001f);
            shadow.transform.localPosition = new Vector3(0f, 0.004f / s, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = Vector3.one * diameter / s;
        }

        /// <summary>A ring that grows and fades where the child touched the floor.</summary>
        public static void Ripple(Vector3 floorPoint, Color color)
        {
            var ripple = FloorQuad("Tap ripple", Ring, color, 0.05f);
            ripple.transform.position = floorPoint + Vector3.up * 0.006f;
            ripple.AddComponent<FxAnimator>().Play(FxAnimator.Kind.Ripple, 0.6f);
        }

        /// <summary>A burst of little glowing dots, for a grab or a right answer.</summary>
        public static void Burst(Vector3 position, Color color, int count = 14)
        {
            for (int i = 0; i < count; i++)
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                dot.name = "Sparkle";
                Object.Destroy(dot.GetComponent<Collider>());
                dot.transform.position = position;
                dot.transform.localScale = Vector3.one * Random.Range(0.025f, 0.05f);
                dot.GetComponent<Renderer>().sharedMaterial = Unlit(Color.Lerp(color, Color.white, Random.value * 0.5f), SoftDot);
                dot.AddComponent<Billboard>();
                var fx = dot.AddComponent<FxAnimator>();
                fx.velocity = Random.onUnitSphere * Random.Range(0.25f, 0.6f) + Vector3.up * 0.3f;
                fx.Play(FxAnimator.Kind.Sparkle, Random.Range(0.6f, 0.9f));
            }
        }

        /// <summary>
        /// A rounded white card with words on it, floating in the room and
        /// always turned to the phone. Lines wrap at about 20 characters.
        /// </summary>
        public static GameObject Label(string text, Vector3 position, Color accent, float scale = 1f)
        {
            var root = new GameObject("Label");
            root.transform.position = position;
            root.AddComponent<Billboard>();
            // The card lives in a child that pops in, so the label can be parented
            // anywhere afterwards without the animation resetting its size.
            var content = new GameObject("Content").transform;
            content.SetParent(root.transform, false);

            string wrapped = Wrap(text, 20);
            int lines = wrapped.Split('\n').Length;
            int longest = 0;
            foreach (var line in wrapped.Split('\n')) longest = Mathf.Max(longest, line.Length);

            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(card.GetComponent<Collider>());
            card.transform.SetParent(content, false);
            card.transform.localScale = new Vector3(Mathf.Clamp(0.03f + longest * 0.0175f, 0.14f, 0.42f), 0.045f + 0.042f * lines, 1f) * scale;
            // Draw order for see-through things: body dots (3100), then the card, its stripe, and the words on top.
            var cardMaterial = Unlit(new Color(1f, 1f, 1f, 0.94f));
            cardMaterial.renderQueue = 3120;
            card.GetComponent<Renderer>().sharedMaterial = cardMaterial;

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(stripe.GetComponent<Collider>());
            stripe.transform.SetParent(content, false);
            stripe.transform.localPosition = new Vector3(0f, -card.transform.localScale.y * 0.5f + 0.006f * scale, -0.001f);
            stripe.transform.localScale = new Vector3(card.transform.localScale.x, 0.012f * scale, 1f);
            var stripeMaterial = Unlit(accent);
            stripeMaterial.renderQueue = 3121;
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMaterial;

            var label = new GameObject("Text");
            label.transform.SetParent(content, false);
            label.transform.localPosition = new Vector3(0f, 0.004f * scale, -0.003f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.font = Font;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial;
            mesh.text = wrapped;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.0062f * scale;
            mesh.color = new Color(0.12f, 0.16f, 0.27f);   // Theme.Ink

            content.gameObject.AddComponent<FxAnimator>().Play(FxAnimator.Kind.PopIn, 0.25f);
            return root;
        }

        static Material textMaterial;

        /// <summary>The font's material, drawn after the label cards so the words are never covered.</summary>
        static Material TextMaterial
        {
            get
            {
                if (textMaterial == null) textMaterial = new Material(Font.material) { renderQueue = 3150 };
                return textMaterial;
            }
        }

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Fonts/Andika-Regular");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static string Wrap(string text, int width)
        {
            var result = new System.Text.StringBuilder();
            foreach (var paragraph in text.Split('\n'))
            {
                if (result.Length > 0) result.Append('\n');
                int lineLength = 0;
                foreach (var word in paragraph.Split(' '))
                {
                    if (lineLength > 0 && lineLength + 1 + word.Length > width) { result.Append('\n'); lineLength = 0; }
                    else if (lineLength > 0) { result.Append(' '); lineLength++; }
                    result.Append(word);
                    lineLength += word.Length;
                }
            }
            return result.ToString();
        }
    }

    /// <summary>Plays one short effect on its object, then removes it (or, for a pop-in, leaves it).</summary>
    public class FxAnimator : MonoBehaviour
    {
        public enum Kind { Ripple, Sparkle, PopIn, Pulse, Shake }

        public Vector3 velocity;
        Kind kind;
        float duration, start;
        Vector3 baseScale, basePosition;
        Renderer rend;
        Color baseColor;

        public void Play(Kind effect, float seconds)
        {
            kind = effect;
            duration = seconds;
            start = Time.time;
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            rend = GetComponent<Renderer>();
            if (rend != null) baseColor = rend.sharedMaterial.color;
            if (kind == Kind.PopIn) transform.localScale = baseScale * 0.2f;
        }

        void Update()
        {
            float t = duration > 0f ? (Time.time - start) / duration : 1f;
            switch (kind)
            {
                case Kind.Ripple:
                    transform.localScale = baseScale * Mathf.Lerp(1f, 6f, t);
                    Fade(1f - t);
                    break;
                case Kind.Sparkle:
                    transform.position += velocity * Time.deltaTime;
                    velocity += Vector3.down * 0.9f * Time.deltaTime;
                    Fade(1f - t);
                    break;
                case Kind.PopIn:
                    float k = Mathf.Clamp01(t);
                    float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.12f;
                    transform.localScale = baseScale * Mathf.Lerp(0.2f, 1f, k) * overshoot;
                    if (t >= 1f) { transform.localScale = baseScale; Destroy(this); }
                    return;
                case Kind.Pulse:
                    // Loops until removed: a gentle "tap me" breathing.
                    float p = 0.5f + 0.5f * Mathf.Sin((Time.time - start) * 4f);
                    transform.localScale = baseScale * (1f + 0.12f * p);
                    Fade(0.55f + 0.45f * p);
                    return;
                case Kind.Shake:
                    transform.localPosition = basePosition + Vector3.right * Mathf.Sin(t * Mathf.PI * 8f) * 0.02f * (1f - t);
                    if (t >= 1f) { transform.localPosition = basePosition; Destroy(this); }
                    return;
            }
            if (t >= 1f) Destroy(gameObject);
        }

        void Fade(float alpha)
        {
            if (rend == null) return;
            var c = baseColor;
            c.a = baseColor.a * Mathf.Clamp01(alpha);
            rend.material.color = c;
        }

        /// <summary>Starts a looping pulse on obj; returns the animator so it can be stopped (Destroy).</summary>
        public static FxAnimator Pulse(GameObject obj)
        {
            var fx = obj.AddComponent<FxAnimator>();
            fx.Play(Kind.Pulse, 1f);
            return fx;
        }

        public static void Shake(GameObject obj)
        {
            if (obj.GetComponent<FxAnimator>() != null) return;
            obj.AddComponent<FxAnimator>().Play(Kind.Shake, 0.45f);
        }
    }
}
