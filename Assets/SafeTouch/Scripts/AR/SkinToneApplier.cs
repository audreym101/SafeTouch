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
        }

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
                    bool skin = IsSkin(materialName) || (materials.Length == 1 && IsSkin(r.name.ToLowerInvariant()));
                    if (skin) slots.Add(new Slot { renderer = r, materialIndex = i });
                }
            }
            // Start blends from the colour the model was made with.
            if (slots.Count > 0 && Current == default(Color))
            {
                var m = slots[0].renderer.sharedMaterials[slots[0].materialIndex];
                Current = m != null && m.HasProperty("_Color") ? m.color : Color.gray;
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
