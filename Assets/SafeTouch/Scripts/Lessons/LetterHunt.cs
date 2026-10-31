using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// The S-T-O-P letters hidden around the room, collected by moving to
    /// them: the guide reaches one (dragged, sent with a tap, or walked with
    /// the arrow buttons), or the child walks up to it holding the phone.
    /// The right letter flies up into a row above where the guide started,
    /// with its meaning hanging under it; the wrong one wobbles. Used by the
    /// STOP lesson (each letter starts its step) and the Memory Quest.
    /// </summary>
    public class LetterHunt : MonoBehaviour
    {
        public static readonly string[] Letters = { "S", "T", "O", "P" };
        static readonly string[] MeaningKeys = { "stop.s.meaning", "stop.t.meaning", "stop.o.meaning", "stop.p.meaning" };
        static readonly string[] MeaningEnglish = { "Say no", "Tell a trusted adult", "Open up and explain", "Protect yourself" };
        static readonly float[] Angles = { 45f, 135f, 225f, 315f };   // around the guide, some behind the child
        const float PhoneReach = 0.8f;   // metres from the phone: the child has walked up to the letter
        const float GuideReach = 0.22f;  // metres across the floor: the guide stands under the letter

        readonly List<GameObject> blocks = new List<GameObject>();
        readonly Dictionary<int, float> shakenAt = new Dictionary<int, float>();
        GameManager game;
        ARCharacter guide;
        Vector3 home, rowCentre, rowRight;
        Action<int> collected;
        Action<int> wrong;

        /// <summary>The letter to collect next (0 = S); 4 when all are collected.</summary>
        public int Next { get; private set; }

        /// <summary>False while the lesson does a letter's step: letters wait until hunting again.</summary>
        public bool Hunting { get; set; }

        public IList<GameObject> Blocks { get { return blocks; } }
        public bool Done { get { return Next >= Letters.Length; } }

        public static string Meaning(int i)
        {
            return Localization.T(MeaningKeys[i], MeaningEnglish[i]);
        }

        /// <summary>Hides the four letters around the guide. collected(i) runs when letter i is picked up; wrongLetter(i) when the guide reaches a letter out of order.</summary>
        public void Begin(GameManager gameManager, ARCharacter character, Action<int> onCollected, Action<int> onWrong)
        {
            Clear();
            game = gameManager;
            guide = character;
            collected = onCollected;
            wrong = onWrong;
            home = guide.transform.position;
            rowCentre = home + Vector3.up * 0.95f;
            rowRight = Vector3.ProjectOnPlane(game.AR.CameraTransform.right, Vector3.up).normalized;
            var angles = Angles.OrderBy(a => UnityEngine.Random.value).ToArray();
            for (int i = 0; i < Letters.Length; i++)
            {
                float radius = UnityEngine.Random.Range(0.9f, 1.2f);
                var pos = game.AR.PointAroundCharacter(radius, angles[i]) + Vector3.up * UnityEngine.Random.Range(0.25f, 0.4f);
                var block = game.AR.SpawnSign(Letters[i], Theme.Unsafe, pos);
                blocks.Add(block);
                StartCoroutine(Bob(block.transform, i, i * 0.7f));
            }
            Next = 0;
            Hunting = true;
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var b in blocks) if (b != null) Destroy(b);
            blocks.Clear();
            shakenAt.Clear();
            Hunting = false;
            Next = 0;
        }

        void OnDisable()
        {
            Clear();
        }

        /// <summary>Letters float gently up and down until collected.</summary>
        IEnumerator Bob(Transform block, int index, float phase)
        {
            var baseY = block.position.y;
            while (block != null && index >= Next)
            {
                var p = block.position;
                p.y = baseY + Mathf.Sin(Time.time * 2f + phase) * 0.03f;
                block.position = p;
                yield return null;
            }
        }

        void Update()
        {
            if (!Hunting || Done || guide == null) return;
            for (int i = Next; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block == null) continue;
                var floorGap = Vector3.ProjectOnPlane(guide.transform.position - block.transform.position, Vector3.up).magnitude;
                bool guideThere = floorGap <= GuideReach;
                bool phoneThere = game.AR.CanWalkAround && Vector3.Distance(game.AR.CameraTransform.position, block.transform.position) <= PhoneReach;
                if (!guideThere && !phoneThere) continue;

                if (i == Next) { guide.Stop(); Collect(i); return; }

                // The wrong letter wobbles (not more than every 2 seconds).
                float last;
                if (shakenAt.TryGetValue(i, out last) && Time.time - last < 2f) continue;
                shakenAt[i] = Time.time;
                FxAnimator.Shake(block);
                game.Audio.PlaySFX("tryagain");
                if (wrong != null) wrong(i);
                return;
            }
        }

        void Collect(int index)
        {
            Hunting = false;
            var block = blocks[index];
            ARFx.Burst(block.transform.position, Theme.Sun);
            var floorGlow = block.transform.Find("Glow");
            if (floorGlow != null) Destroy(floorGlow.gameObject);
            game.Audio.PlaySFX("correct");
            guide.Bounce();
            var target = rowCentre + rowRight * (index - 1.5f) * 0.3f;
            string meaning = Meaning(index);
            StartCoroutine(Fly(block.transform, target, () =>
            {
                // The meaning hangs under the letter; every other one a little lower so they never overlap.
                float drop = index % 2 == 0 ? 0.18f : 0.3f;
                var label = ARFx.Label(meaning, target - Vector3.up * drop, Theme.Unsafe, 0.85f);
                label.transform.SetParent(block.transform, true);
            }));
            Next++;
            if (collected != null) collected(index);
        }

        IEnumerator Fly(Transform block, Vector3 target, Action landed)
        {
            var start = block.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime * 1.6f)
            {
                if (block == null) yield break;
                block.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.15f;
                yield return null;
            }
            if (block == null) yield break;
            block.position = target;
            if (landed != null) landed();
        }

        /// <summary>"S is behind you": where a letter is, from the phone's point of view.</summary>
        public string Direction(int index)
        {
            if (index < 0 || index >= blocks.Count || blocks[index] == null) return "";
            var cam = game.AR.CameraTransform;
            var to = Vector3.ProjectOnPlane(blocks[index].transform.position - cam.position, Vector3.up);
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            float angle = Vector3.SignedAngle(forward, to, Vector3.up);
            if (Mathf.Abs(angle) < 35f) return Localization.T("review.grab.ahead", "in front of you");
            if (Mathf.Abs(angle) > 130f) return Localization.T("review.grab.behind", "behind you");
            return angle > 0f ? Localization.T("review.grab.right", "to your right") : Localization.T("review.grab.left", "to your left");
        }

        /// <summary>The "Take me to S" button: the next letter wobbles and the guide walks to it.</summary>
        public void TakeGuideToNext()
        {
            if (Done || blocks[Next] == null) return;
            FxAnimator.Shake(blocks[Next]);
            ARFx.Burst(blocks[Next].transform.position, Theme.Sun, 8);
            var spot = blocks[Next].transform.position;
            spot.y = guide.transform.position.y;
            guide.WalkTo(spot, null);
        }

        /// <summary>Tapping a letter sends the guide walking to it; reaching it picks it up.</summary>
        public bool HandleTap(ARTap tap)
        {
            if (!Hunting) return false;
            for (int i = Next; i < blocks.Count; i++)
            {
                if (!tap.Touched(blocks[i])) continue;
                var spot = blocks[i].transform.position;
                spot.y = guide.transform.position.y;
                guide.WalkTo(spot, null);
                return true;
            }
            return false;
        }

        /// <summary>The guide walks back under the finished word.</summary>
        public void GuideHome(Action arrived)
        {
            guide.WalkTo(home, arrived);
        }
    }
}
