using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Changes the skin colour of a character. It finds the skin by material
    /// or part name, so it works on the simple built-in figures ("Head") and
    /// on imported models (materials such as "Skin_MAT" or "RealismBody")
    /// without hand-picking parts for every model.
    /// </summary>
    public class SkinToneApplier : MonoBehaviour
    {
        [Tooltip("A material or part whose name contains one of these is skin.")]
        [SerializeField] string[] skinKeywords = { "skin", "head", "face", "hand", "realismbody" };

        [Tooltip("...unless it also contains one of these.")]
        [SerializeField] string[] ignoreKeywords = { "eye", "cloth", "hair", "brow", "lash", "glass", "teeth", "iris", "sclera" };

        struct Slot
        {
            public Renderer renderer;
            public int materialIndex;
            public bool masked;   // uses the "SafeTouch/Skin Tone" shader: only the masked skin changes
        }

        static readonly int SkinColorId = Shader.PropertyToID("_SkinColor");

        [Tooltip("Show the tone as a flat colour instead of tinting the model's skin texture. " +
                 "Tinting a light texture can only make it darker, so dark tones would look muddy.")]
        [SerializeField] bool replaceSkinTexture = true;

        readonly List<Slot> slots = new List<Slot>();
        MaterialPropertyBlock block;
        bool searched;

        /// <summary>The colour the skin shows now.</summary>
        public Color Current { get; private set; }

        /// <summary>
        /// Paints every skin slot. Uses a MaterialPropertyBlock, so shared
        /// material assets are never changed and Keza and a visitor that use
        /// the same model can have different tones.
        /// </summary>
        public void Apply(Color color)
        {
            if (!searched) FindSkin();
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var slot in slots)
            {
                if (slot.renderer == null) continue;
                slot.renderer.GetPropertyBlock(block, slot.materialIndex);
                if (slot.masked)
                {
                    // One texture holds skin and clothes; the shader's mask keeps the clothes.
                    block.SetColor(SkinColorId, color);
                    slot.renderer.SetPropertyBlock(block, slot.materialIndex);
                    continue;
                }
                block.SetColor("_Color", color);       // Built-in Standard shader
                block.SetColor("_BaseColor", color);   // URP Lit, if the project moves to URP
                if (replaceSkinTexture)
                {
                    block.SetTexture("_MainTex", Texture2D.whiteTexture);
                    block.SetTexture("_BaseMap", Texture2D.whiteTexture);
                }
                slot.renderer.SetPropertyBlock(block, slot.materialIndex);
            }
            Current = color;
        }

        [Tooltip("Seconds for the colour to blend to a newly picked tone.")]
        [SerializeField] float blendSeconds = 0.6f;

        Coroutine blending;

        /// <summary>The palette id of the tone shown, e.g. "cocoa".</summary>
        public string ToneId { get; private set; }

        /// <summary>
        /// Shows a palette tone. With animate, the colour blends over
        /// blendSeconds with a soft glow at the midpoint, so the child sees
        /// their choice "arrive" on Keza in the room.
        /// </summary>
        public void SetTone(Data.SkinTone tone, bool animate)
        {
            if (tone == null) return;
            ToneId = tone.id;
            if (blending != null) StopCoroutine(blending);
            blending = null;
            if (!animate || !isActiveAndEnabled) { Apply(tone.Color); return; }
            blending = StartCoroutine(Blend(Current, tone.Color));
            StartCoroutine(Sparkle(tone.Color));
        }

        [Tooltip("Material for the sparkle burst; any opaque material works. Falls back to a copy of the skin material.")]
        [SerializeField] Material sparkleMaterial;

        const int SparkleCount = 12;

        /// <summary>
        /// A small burst of sun-yellow and new-tone dots that rise and fade
        /// around the character. Built from spheres rather than a particle
        /// system, so it needs no extra shader in the Android build.
        /// </summary>
        System.Collections.IEnumerator Sparkle(Color tone)
        {
            var bounds = CharacterBounds();
            var material = sparkleMaterial;
            if (material == null && slots.Count > 0) material = slots[0].renderer.sharedMaterials[slots[0].materialIndex];
            var sun = new Color(0.965f, 0.718f, 0.235f);   // Theme.Sun

            var dots = new Transform[SparkleCount];
            var directions = new Vector3[SparkleCount];
            float size = bounds.size.y * 0.06f;
            for (int i = 0; i < SparkleCount; i++)
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(dot.GetComponent<Collider>());
                dot.name = "Sparkle";
                var r = dot.GetComponent<Renderer>();
                if (material != null) r.material = new Material(material);
                r.material.color = i % 2 == 0 ? sun : tone;
                float angle = i * Mathf.PI * 2f / SparkleCount;
                directions[i] = new Vector3(Mathf.Cos(angle), 0.8f, Mathf.Sin(angle));
                dot.transform.position = bounds.center + Vector3.up * bounds.extents.y * 0.4f;
                dot.transform.localScale = Vector3.one * size;
                dots[i] = dot.transform;
            }

            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
            {
                for (int i = 0; i < SparkleCount; i++)
                {
                    if (dots[i] == null) continue;
                    dots[i].position += directions[i] * bounds.size.y * 0.9f * Time.deltaTime;
                    dots[i].localScale = Vector3.one * size * (1f - t);
                }
                yield return null;
            }
            foreach (var d in dots) if (d != null) Destroy(d.gameObject);
        }

        Bounds CharacterBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one * 0.5f);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        System.Collections.IEnumerator Blend(Color from, Color to)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, blendSeconds))
            {
                var c = Color.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                float glow = Mathf.Sin(t * Mathf.PI) * 0.25f;   // brightens a little half way through
                Apply(Color.Lerp(c, Color.white, glow));
                yield return null;
            }
            Apply(to);
            blending = null;
        }

        /// <summary>How many material slots were recognised as skin (0 means this model can't be tinted).</summary>
        public int SkinSlotCount
        {
            get
            {
                if (!searched) FindSkin();
                return slots.Count;
            }
        }

        /// <summary>Looks through the character's renderers for skin. Call again if parts are added.</summary>
        public void FindSkin()
        {
            searched = true;
            slots.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;   // the name label
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] != null ? materials[i].name.ToLowerInvariant() : "";
                    bool masked = materials[i] != null && materials[i].HasProperty(SkinColorId);
                    bool skin = masked || IsSkin(materialName) || (materials.Length == 1 && IsSkin(r.name.ToLowerInvariant()));
                    if (skin) slots.Add(new Slot { renderer = r, materialIndex = i, masked = masked });
                }
            }
            // Start blends from the colour the model was made with.
            if (slots.Count > 0 && Current == default(Color))
            {
                var m = slots[0].renderer.sharedMaterials[slots[0].materialIndex];
                Current = m == null ? Color.gray
                    : m.HasProperty(SkinColorId) ? m.GetColor(SkinColorId)
                    : m.HasProperty("_Color") ? m.color : Color.gray;
            }
        }

        bool IsSkin(string lowerName)
        {
            foreach (var word in ignoreKeywords) if (lowerName.Contains(word)) return false;
            foreach (var word in skinKeywords) if (lowerName.Contains(word)) return true;
            return false;
        }
    }
}
