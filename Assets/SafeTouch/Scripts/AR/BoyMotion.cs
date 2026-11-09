using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Brings the boy built by BoyModel to life without animation files. When he moves, his legs step,
    /// his knees bend, his arms swing against his legs and he bobs a little; standing still, he breathes and
    /// looks about; on Wave (when he says hello or is happy) he raises his hand and waves.
    /// It reads how fast the boy moves each frame, so walking, being dragged and running all just work.
    /// </summary>
    public class BoyMotion : MonoBehaviour
    {
        public Transform body, torso, head, shoulderL, shoulderR, elbowL, elbowR, hipL, hipR, kneeL, kneeR;

        Vector3 lastPosition;
        float speed, phase, waveStart = -10f, bodyY;
        bool started;

        /// <summary>Raises the right hand and waves for a moment.</summary>
        public void Wave()
        {
            waveStart = Time.time;
        }

        void LateUpdate()
        {
            if (body == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            if (!started)
            {
                started = true;
                lastPosition = transform.position;
                bodyY = body.localPosition.y;
            }

            // How fast he moves over the floor, smoothed so a single jump never jerks the legs.
            var moved = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up).magnitude / dt;
            lastPosition = transform.position;
            speed = Mathf.Lerp(speed, Mathf.Min(moved, 2f), 1f - Mathf.Exp(-10f * dt));
            float walk = Mathf.Clamp01(speed / 0.2f);
            phase += dt * (5f + speed * 14f) * walk;
            float s = Mathf.Sin(phase);

            // Legs: forward is a negative turn about x; the knee bends as each foot swings through.
            Turn(hipL, -s * 26f * walk, 0f);
            Turn(hipR, s * 26f * walk, 0f);
            Turn(kneeL, Mathf.Max(0f, Mathf.Sin(phase - 0.7f)) * 42f * walk, 0f);
            Turn(kneeR, Mathf.Max(0f, -Mathf.Sin(phase - 0.7f)) * 42f * walk, 0f);

            // Arms swing against the legs, a little out from the body, elbows soft; idle, they sway gently.
            float t = Time.time;
            float sway = Mathf.Sin(t * 1.3f) * 2.5f * (1f - walk);
            Turn(shoulderL, s * 24f * walk + sway, -5f);
            Turn(shoulderR, -s * 24f * walk - sway, 5f);
            Turn(elbowL, -(8f + 14f * walk), 0f);
            Turn(elbowR, -(8f + 14f * walk), 0f);

            // A small bob with each step, and breathing when standing.
            float scale = Mathf.Max(transform.lossyScale.y, 0.0001f);
            var p = body.localPosition;
            p.y = bodyY + Mathf.Abs(s) * 0.007f * walk / scale;
            body.localPosition = p;
            if (torso != null)
            {
                float breath = Mathf.Sin(t * 2.2f) * 0.012f * (1f - walk);
                torso.localScale = new Vector3(1f + breath * 0.5f, 1f + breath, 1f + breath);
                torso.localRotation = Quaternion.Euler(4f * walk, 0f, 0f);   // leans into the walk
            }
            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 2f * (1f - walk), Mathf.Sin(t * 0.45f) * 6f * (1f - walk), 0f);

            // The wave: the right arm goes up to the side and the forearm waves, then comes back down.
            float w = Time.time - waveStart;
            if (w < 1.6f && shoulderR != null && elbowR != null)
            {
                float up = Mathf.SmoothStep(0f, 1f, Mathf.Min(w / 0.25f, (1.6f - w) / 0.3f));
                shoulderR.localRotation = Quaternion.Slerp(shoulderR.localRotation, Quaternion.Euler(0f, 0f, 155f), up);
                elbowR.localRotation = Quaternion.Slerp(elbowR.localRotation, Quaternion.Euler(0f, 0f, Mathf.Sin(w * 14f) * 28f), up);
            }
        }

        static void Turn(Transform joint, float forward, float outward)
        {
            if (joint != null) joint.localRotation = Quaternion.Euler(forward, 0f, outward);
        }
    }
}
