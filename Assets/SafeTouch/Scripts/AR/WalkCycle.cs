using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Legs and arms that swing as a person moves, for the models with a skeleton but no walking clip
    /// (Keza, Ganza, the classmate: a Mixamo skeleton with only a greeting). It watches how far the person
    /// really moved each frame, however they moved (walking to a spot, dragged by a finger, walked by a
    /// lesson), so the feet keep pace with the ground and stop when the person stops: no gliding.
    ///
    /// It runs after the Animator and turns the hip, knee, shoulder and elbow bones a little around the
    /// person's own right-hand axis, so it works whichever way the bones point inside the model. When the
    /// Animator does not set a bone again in a frame, last frame's turn is taken off first, so turns
    /// never add up.
    /// </summary>
    public class WalkCycle : MonoBehaviour
    {
        class Bone
        {
            public Transform t;
            public Quaternion before, after;
            public bool turned;
        }

        Bone leftHip, rightHip, leftKnee, rightKnee, leftShoulder, rightShoulder, leftElbow, rightElbow;
        Vector3 last;
        float phase, amount, height;

        /// <summary>Adds the walk to a person whose model has hip, knee, shoulder and elbow bones; null if it has not.</summary>
        public static WalkCycle AddTo(GameObject person)
        {
            var bones = person.GetComponentsInChildren<Transform>(true);
            var hipL = Find(bones, "LeftUpLeg"); var hipR = Find(bones, "RightUpLeg");
            var kneeL = Find(bones, "LeftLeg"); var kneeR = Find(bones, "RightLeg");
            if (hipL == null || hipR == null || kneeL == null || kneeR == null) return null;
            var walk = person.GetComponent<WalkCycle>();
            if (walk == null) walk = person.AddComponent<WalkCycle>();
            walk.leftHip = new Bone { t = hipL };
            walk.rightHip = new Bone { t = hipR };
            walk.leftKnee = new Bone { t = kneeL };
            walk.rightKnee = new Bone { t = kneeR };
            var armL = Find(bones, "LeftArm"); var armR = Find(bones, "RightArm");
            var elbowL = Find(bones, "LeftForeArm"); var elbowR = Find(bones, "RightForeArm");
            if (armL != null && armR != null)
            {
                walk.leftShoulder = new Bone { t = armL };
                walk.rightShoulder = new Bone { t = armR };
            }
            if (elbowL != null && elbowR != null)
            {
                walk.leftElbow = new Bone { t = elbowL };
                walk.rightElbow = new Bone { t = elbowR };
            }
            return walk;
        }

        /// <summary>A bone whose name is the given one, with or without a rig prefix ("mixamorig:LeftUpLeg").</summary>
        static Transform Find(Transform[] bones, string name)
        {
            foreach (var b in bones)
            {
                string n = b.name;
                if (n == name || n.EndsWith(":" + name) || n.EndsWith("_" + name) || n.EndsWith(" " + name)) return b;
            }
            return null;
        }

        void Start()
        {
            last = transform.position;
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            height = 0.5f;
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                height = Mathf.Max(bounds.size.y, 0.1f);
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var moved = Vector3.ProjectOnPlane(transform.position - last, Vector3.up);
            last = transform.position;
            float distance = moved.magnitude;
            if (distance > height) distance = 0f;   // put somewhere new, not walked there

            // Full steps at a brisk walk; the swing grows and settles over a fraction of a second.
            float speed = distance / dt;
            amount = Mathf.MoveTowards(amount, Mathf.Clamp01(speed / (height * 0.45f)), dt * 5f);
            phase += distance / (height * 0.8f) * Mathf.PI * 2f;   // one stride (two steps) is a little less than the person's height

            var axis = transform.right;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float legs = 28f * amount, arms = 22f * amount;
            // Turning around the right-hand axis: a negative angle swings a hanging limb forwards.
            Turn(leftHip, axis, -s * legs);
            Turn(rightHip, axis, s * legs);
            // The knee bends as the leg swings through.
            Turn(leftKnee, axis, Mathf.Max(0f, c) * 45f * amount + 4f * amount);
            Turn(rightKnee, axis, Mathf.Max(0f, -c) * 45f * amount + 4f * amount);
            // Arms swing against the legs, elbows a little bent.
            Turn(leftShoulder, axis, s * arms);
            Turn(rightShoulder, axis, -s * arms);
            Turn(leftElbow, axis, -12f * amount);
            Turn(rightElbow, axis, -12f * amount);
        }

        static void Turn(Bone bone, Vector3 axis, float degrees)
        {
            if (bone == null || bone.t == null) return;
            // The Animator did not set this bone again: take last frame's turn off before adding this one.
            if (bone.turned && Quaternion.Angle(bone.t.localRotation, bone.after) < 0.01f) bone.t.localRotation = bone.before;
            bone.before = bone.t.localRotation;
            if (Mathf.Abs(degrees) > 0.01f) bone.t.rotation = Quaternion.AngleAxis(degrees, axis) * bone.t.rotation;
            bone.after = bone.t.localRotation;
            bone.turned = true;
        }
    }
}
