using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// The boy helper, built from simple shapes when no boy model has been set on ARManager: a boy of about
    /// seven, the same height as Keza (about 0.55 m), with a child's real proportions (the head about a fifth
    /// of his height), short hair with a fringe, eyebrows, a nose and a smile, a T-shirt with short sleeves,
    /// shorts and trainers. His shoulders, elbows, hips and knees are joints, so BoyMotion can swing his arms
    /// and legs as he walks, let him breathe when he stands and wave hello.
    /// His skin parts are named "Skin", so the skin tone the child picks colours them like Keza's.
    /// </summary>
    public static class BoyModel
    {
        const float Height = 0.55f;

        static readonly Color Shirt = new Color(0.24f, 0.56f, 0.85f), ShirtBand = new Color(0.15f, 0.36f, 0.6f),
            Shorts = new Color(0.17f, 0.24f, 0.4f), Shoe = new Color(0.96f, 0.96f, 0.98f), Sole = new Color(0.85f, 0.35f, 0.25f),
            Hair = new Color(0.1f, 0.07f, 0.05f), StartSkin = new Color(0.55f, 0.36f, 0.22f), Iris = new Color(0.16f, 0.1f, 0.07f),
            Lips = new Color(0.45f, 0.2f, 0.16f);

        /// <summary>
        /// Builds the boy. scale: the girl model's own scale, so both helpers behave the same (bubbles, touch);
        /// he is always Height tall whatever it is.
        /// </summary>
        public static GameObject Build(Material baseMaterial, float scale)
        {
            var root = new GameObject("Boy guide");
            root.transform.localScale = Vector3.one * scale;
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localScale = Vector3.one * (Height / 0.55f) / Mathf.Max(scale, 0.0001f);   // build in metres

            Material skin = Paint(baseMaterial, StartSkin, "Skin"), shirt = Paint(baseMaterial, Shirt, "Clothes shirt"),
                band = Paint(baseMaterial, ShirtBand, "Clothes band"), shorts = Paint(baseMaterial, Shorts, "Clothes shorts"),
                shoe = Paint(baseMaterial, Shoe, "Clothes shoes"), sole = Paint(baseMaterial, Sole, "Clothes soles"),
                hair = Paint(baseMaterial, Hair, "Hair"), white = Paint(baseMaterial, Color.white, "Eye white"),
                iris = Paint(baseMaterial, Iris, "Eye iris"), lips = Paint(baseMaterial, Lips, "Mouth");

            var motion = root.AddComponent<BoyMotion>();

            // Torso: a joint at the waist, so he can breathe and lean a little.
            var torso = Joint(body, "Torso", new Vector3(0f, 0.28f, 0f));
            Part(torso, PrimitiveType.Capsule, "Shirt", new Vector3(0f, 0.08f, 0f), new Vector3(0.13f, 0.085f, 0.085f), shirt);
            // The shoulder line, from one sleeve to the other, so the arms join the body.
            Part(torso, PrimitiveType.Capsule, "Shirt shoulders", new Vector3(0f, 0.132f, 0f), new Vector3(0.052f, 0.086f, 0.054f), shirt, new Vector3(0f, 0f, 90f));
            Part(torso, PrimitiveType.Cylinder, "Collar", new Vector3(0f, 0.162f, 0.002f), new Vector3(0.05f, 0.004f, 0.048f), band);
            Part(torso, PrimitiveType.Cylinder, "Shorts top", new Vector3(0f, -0.005f, 0f), new Vector3(0.13f, 0.03f, 0.085f), shorts);
            Part(torso, PrimitiveType.Cylinder, "Skin neck", new Vector3(0f, 0.17f, 0f), new Vector3(0.036f, 0.016f, 0.036f), skin);

            // Head: a little taller than wide, with the face to the front (+z).
            var head = Joint(torso, "Head", new Vector3(0f, 0.18f, 0f));
            Part(head, PrimitiveType.Sphere, "Skin head", new Vector3(0f, 0.05f, 0f), new Vector3(0.09f, 0.104f, 0.094f), skin);
            Part(head, PrimitiveType.Sphere, "Hair", new Vector3(0f, 0.069f, -0.011f), new Vector3(0.095f, 0.074f, 0.096f), hair);
            Part(head, PrimitiveType.Sphere, "Hair fringe", new Vector3(0f, 0.088f, 0.028f), new Vector3(0.07f, 0.026f, 0.04f), hair);
            Part(head, PrimitiveType.Sphere, "Skin nose", new Vector3(0f, 0.044f, 0.047f), new Vector3(0.011f, 0.013f, 0.011f), skin);
            // A smile: the middle of the mouth, and its two corners turned up.
            Part(head, PrimitiveType.Capsule, "Mouth", new Vector3(0f, 0.026f, 0.048f), new Vector3(0.0045f, 0.008f, 0.004f), lips, new Vector3(0f, 0f, 90f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(head, PrimitiveType.Capsule, "Mouth corner", new Vector3(side * 0.0085f, 0.0282f, 0.0465f), new Vector3(0.004f, 0.0045f, 0.004f), lips, new Vector3(0f, 0f, 90f + side * 35f));
                Part(head, PrimitiveType.Sphere, "Skin ear", new Vector3(side * 0.046f, 0.048f, -0.004f), new Vector3(0.013f, 0.024f, 0.016f), skin);
                Part(head, PrimitiveType.Sphere, "Eye white", new Vector3(side * 0.018f, 0.056f, 0.043f), new Vector3(0.016f, 0.013f, 0.008f), white);
                Part(head, PrimitiveType.Sphere, "Eye iris", new Vector3(side * 0.018f, 0.0555f, 0.0465f), new Vector3(0.0095f, 0.0095f, 0.004f), iris);
                Part(head, PrimitiveType.Sphere, "Eye light", new Vector3(side * 0.0165f, 0.058f, 0.0487f), Vector3.one * 0.0032f, white);
                // Brows a little raised at the outside: a friendly look, not a frown.
                Part(head, PrimitiveType.Capsule, "Hair brow", new Vector3(side * 0.019f, 0.0705f, 0.046f), new Vector3(0.0035f, 0.009f, 0.003f), hair, new Vector3(0f, 0f, 90f - side * 10f));
            }
            motion.head = head;
            motion.torso = torso;

            foreach (float side in new[] { -1f, 1f })
            {
                // Arm: shoulder joint, a short sleeve over the upper arm, elbow joint, forearm and hand.
                var shoulder = Joint(torso, side < 0 ? "Shoulder L" : "Shoulder R", new Vector3(side * 0.078f, 0.135f, 0f));
                Part(shoulder, PrimitiveType.Sphere, "Sleeve", new Vector3(0f, -0.004f, 0f), new Vector3(0.05f, 0.05f, 0.05f), shirt);
                Part(shoulder, PrimitiveType.Capsule, "Sleeve arm", new Vector3(0f, -0.028f, 0f), new Vector3(0.042f, 0.026f, 0.042f), shirt);
                Part(shoulder, PrimitiveType.Capsule, "Skin upper arm", new Vector3(0f, -0.05f, 0f), new Vector3(0.03f, 0.05f, 0.03f), skin);
                var elbow = Joint(shoulder, side < 0 ? "Elbow L" : "Elbow R", new Vector3(0f, -0.1f, 0f));
                Part(elbow, PrimitiveType.Capsule, "Skin forearm", new Vector3(0f, -0.045f, 0f), new Vector3(0.027f, 0.047f, 0.027f), skin);
                Part(elbow, PrimitiveType.Sphere, "Skin hand", new Vector3(0f, -0.098f, 0.003f), new Vector3(0.026f, 0.032f, 0.02f), skin);

                // Leg: hip joint, shorts over the thigh, knee joint, shin and trainer.
                var hip = Joint(body, side < 0 ? "Hip L" : "Hip R", new Vector3(side * 0.034f, 0.27f, 0f));
                Part(hip, PrimitiveType.Capsule, "Shorts leg", new Vector3(0f, -0.04f, 0f), new Vector3(0.056f, 0.045f, 0.058f), shorts);
                Part(hip, PrimitiveType.Capsule, "Skin thigh", new Vector3(0f, -0.075f, 0f), new Vector3(0.038f, 0.06f, 0.038f), skin);
                var knee = Joint(hip, side < 0 ? "Knee L" : "Knee R", new Vector3(0f, -0.13f, 0f));
                Part(knee, PrimitiveType.Capsule, "Skin shin", new Vector3(0f, -0.055f, 0f), new Vector3(0.032f, 0.058f, 0.032f), skin);
                Part(knee, PrimitiveType.Sphere, "Shoe", new Vector3(0f, -0.118f, 0.014f), new Vector3(0.042f, 0.03f, 0.074f), shoe);
                Part(knee, PrimitiveType.Cube, "Sole", new Vector3(0f, -0.132f, 0.014f), new Vector3(0.04f, 0.007f, 0.068f), sole);

                if (side < 0) { motion.shoulderL = shoulder; motion.elbowL = elbow; motion.hipL = hip; motion.kneeL = knee; }
                else { motion.shoulderR = shoulder; motion.elbowR = elbow; motion.hipR = hip; motion.kneeR = knee; }
            }
            motion.body = body;

            // Taps and drags find him with this, like Keza's hit box.
            var hit = body.gameObject.AddComponent<CapsuleCollider>();
            hit.center = new Vector3(0f, 0.28f, 0f);
            hit.height = 0.58f;
            hit.radius = 0.12f;

            // The name tag over his head, facing the phone.
            var tag = new GameObject("Name label");
            tag.transform.SetParent(body, false);
            tag.transform.localPosition = new Vector3(0f, 0.62f, 0f);
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

        static Transform Joint(Transform parent, string name, Vector3 position)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        static Material Paint(Material baseMaterial, Color color, string name)
        {
            var m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
            m.name = name;   // the skin tone finds "Skin"; the clothes keep their colours
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", name == "Eye iris" || name == "Eye white" ? 0.7f : 0.18f);
            return m;
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default(Vector3))
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
