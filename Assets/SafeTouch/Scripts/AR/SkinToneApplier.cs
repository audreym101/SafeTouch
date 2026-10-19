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
        }

        bool IsSkin(string lowerName)
        {
            foreach (var word in ignoreKeywords) if (lowerName.Contains(word)) return false;
            foreach (var word in skinKeywords) if (lowerName.Contains(word)) return true;
            return false;
        }
    }
}
