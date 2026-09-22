using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SafeTouch.AR
{
    // NFR4/NFR5: keeps a young child oriented with a short, calm status message
    // instead of raw AR tracking states.
    public class ARSessionStatusUI : MonoBehaviour
    {
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARPlaneTapToPlace tapToPlace;
        [SerializeField] private TMP_Text statusText;

        private void Update()
        {
            if (statusText == null) return;

            if (tapToPlace != null && tapToPlace.PlacedInstance != null)
            {
                statusText.text = "Great! Tap somewhere else if you want to move it.";
                return;
            }

            switch (ARSession.state)
            {
                case ARSessionState.None:
                case ARSessionState.CheckingAvailability:
                    statusText.text = "Getting ready...";
                    break;
                case ARSessionState.Unsupported:
                    statusText.text = "This device can't run the AR lesson.";
                    break;
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    statusText.text = "Setting things up, one moment...";
                    break;
                case ARSessionState.SessionInitializing:
                    statusText.text = "Move your phone slowly to look around.";
                    break;
                case ARSessionState.SessionTracking:
                    statusText.text = HasDetectedPlane()
                        ? "Tap the highlighted floor to place your helper!"
                        : "Move your phone slowly to find a flat surface.";
                    break;
                default:
                    statusText.text = string.Empty;
                    break;
            }
        }

        private bool HasDetectedPlane()
        {
            if (planeManager == null) return false;
            foreach (var _ in planeManager.trackables)
            {
                return true;
            }
            return false;
        }
    }
}
