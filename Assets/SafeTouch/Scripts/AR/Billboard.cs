using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>Turns a name label to face the phone so children can always read it.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var away = transform.position - cam.transform.position;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
        }
    }
}
