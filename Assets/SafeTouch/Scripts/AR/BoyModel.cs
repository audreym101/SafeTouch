using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// The boy guide, built from simple shapes when no boy model has been set on ARManager: a child the same
    /// height as Keza (about 0.55 m) with short dark hair, a blue T-shirt, shorts and trainers, a smile and a
    /// name tag. His skin parts are named "Skin", so the skin tone the child picks colours them like Keza's.
    /// He has no animation clips: he hops when he waves (ARFigure.Bounce), and walks like the other people.
    /// </summary>
    public static class BoyModel
    {
        const float Height = 0.55f;

        static readonly Color Shirt = new Color(0.24f, 0.56f, 0.85f), Shorts = new Color(0.17f, 0.24f, 0.4f),
            Shoe = new Color(0.95f, 0.95f, 0.97f), Sole = new Color(0.85f, 0.35f, 0.25f), Hair = new Color(0.12f, 0.08f, 0.06f),
            StartSkin = new Color(0.55f, 0.36f, 0.22f), Eye = new Color(0.1f, 0.08f, 0.08f), Mouth = new Color(0.4f, 0.16f, 0.12f);

        /// <summary>
        /// Builds the boy. scale: the girl model's own scale, so both guides behave the same (bubbles, touch);
        /// he is always Height tall whatever it is.
        /// </summary>
        public static GameObject Build(Material baseMaterial, float scale)
        {
            var root = new GameObject("Boy guide");
            root.transform.localScale = Vector3.one * scale;
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localScale = Vector3.one / Mathf.Max(scale, 0.0001f);   // build in metres
            body.localScale *= Height / 0.55f;

            Material skin = Paint(baseMaterial, StartSkin, "Skin"), shirt = Paint(baseMaterial, Shirt, "Clothes shirt"),
                shorts = Paint(baseMaterial, Shorts, "Clothes shorts"), shoe = Paint(baseMaterial, Shoe, "Clothes shoes"),
                sole = Paint(baseMaterial, Sole, "Clothes soles"), hair = Paint(baseMaterial, Hair, "Hair"),
                eye = Paint(baseMaterial, Eye, "Eye"), mouth = Paint(baseMaterial, Mouth, "Mouth");

            foreach (float side in new[] { -1f, 1f })
            {
                // Trainers, legs, arms with short sleeves, ears.
                Part(body, PrimitiveType.Sphere, "Shoe", new Vector3(side * 0.045f, 0.022f, 0.012f), new Vector3(0.07f, 0.045f, 0.1f), shoe);
                Part(body, PrimitiveType.Cube, "Sole", new Vector3(side * 0.045f, 0.004f, 0.012f), new Vector3(0.066f, 0.008f, 0.092f), sole);
                Part(body, PrimitiveType.Capsule, "Skin leg", new Vector3(side * 0.045f, 0.1f, 0f), new Vector3(0.048f, 0.06f, 0.048f), skin);
                Part(body, PrimitiveType.Capsule, "Skin arm", new Vector3(side * 0.112f, 0.255f, 0f), new Vector3(0.042f, 0.065f, 0.042f), skin, new Vector3(0f, 0f, side * 12f));
                Part(body, PrimitiveType.Sphere, "Sleeve", new Vector3(side * 0.095f, 0.31f, 0f), new Vector3(0.065f, 0.06f, 0.065f), shirt);
                Part(body, PrimitiveType.Sphere, "Skin hand", new Vector3(side * 0.122f, 0.195f, 0.005f), Vector3.one * 0.04f, skin);
                Part(body, PrimitiveType.Sphere, "Skin ear", new Vector3(side * 0.086f, 0.43f, -0.005f), new Vector3(0.025f, 0.04f, 0.03f), skin);
                // Eyes with a little light in them.
                Part(body, PrimitiveType.Sphere, "Eye", new Vector3(side * 0.03f, 0.44f, 0.074f), new Vector3(0.024f, 0.03f, 0.014f), eye);
                Part(body, PrimitiveType.Sphere, "Eye light", new Vector3(side * 0.033f, 0.447f, 0.081f), Vector3.one * 0.008f, Paint(baseMaterial, Color.white, "Eye light"));
            }
            Part(body, PrimitiveType.Cylinder, "Shorts", new Vector3(0f, 0.18f, 0f), new Vector3(0.155f, 0.035f, 0.105f), shorts);
            Part(body, PrimitiveType.Capsule, "Shirt", new Vector3(0f, 0.27f, 0f), new Vector3(0.165f, 0.085f, 0.115f), shirt);
            Part(body, PrimitiveType.Cylinder, "Skin neck", new Vector3(0f, 0.35f, 0f), new Vector3(0.045f, 0.02f, 0.045f), skin);
            Part(body, PrimitiveType.Sphere, "Skin head", new Vector3(0f, 0.43f, 0f), new Vector3(0.17f, 0.18f, 0.16f), skin);
            // Short hair: a cap over the top and back of the head, leaving the face clear.
            Part(body, PrimitiveType.Sphere, "Hair", new Vector3(0f, 0.47f, -0.015f), new Vector3(0.178f, 0.12f, 0.165f), hair);
            Part(body, PrimitiveType.Capsule, "Mouth", new Vector3(0f, 0.405f, 0.077f), new Vector3(0.011f, 0.02f, 0.008f), mouth, new Vector3(0f, 0f, 90f));

            // Taps and drags find him with this, like Keza's hit box.
            var hit = body.gameObject.AddComponent<CapsuleCollider>();
            hit.center = new Vector3(0f, 0.28f, 0f);
            hit.height = 0.58f;
            hit.radius = 0.13f;

            // The name tag over his head, facing the phone.
            var tag = new GameObject("Name label");
            tag.transform.SetParent(body, false);
            tag.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            tag.AddComponent<Billboard>();
            var text = tag.AddComponent<TextMesh>();
            text.font = ARFx.Font;
            tag.GetComponent<MeshRenderer>().sharedMaterial = ARFx.Font.material;
            text.fontSize = 64;
            text.characterSize = 0.0065f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.12f, 0.16f, 0.27f);

            var guide = root.AddComponent<ARCharacter>();
            guide.UseNameLabel(text);
            guide.KeepOwnColours = true;
            return root;
        }

        /// <summary>Another boy in a story (a cousin, a classmate): his T-shirt in the role's colour.</summary>
        public static void Dress(GameObject boy, Color shirt)
        {
            foreach (var r in boy.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial != null && r.sharedMaterial.name == "Clothes shirt") r.material.color = shirt;
        }

        static Material Paint(Material baseMaterial, Color color, string name)
        {
            var m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
            m.name = name;   // the skin tone finds "Skin"; the clothes keep their colours
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.2f);
            return m;
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default(Vector3))
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
