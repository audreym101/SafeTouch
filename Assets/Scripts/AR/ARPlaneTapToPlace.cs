using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SafeTouch.AR
{
    // FR1: detect a plane through the AR camera. FR2: place a character on the plane and move it.
    [RequireComponent(typeof(ARRaycastManager))]
    public class ARPlaneTapToPlace : MonoBehaviour
    {
        [SerializeField] private GameObject placedPrefab;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private bool hidePlanesAfterFirstPlacement = true;

        private ARRaycastManager raycastManager;
        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        public GameObject PlacedInstance { get; private set; }
        public event Action<GameObject> OnObjectPlaced;

        private void Awake()
        {
            raycastManager = GetComponent<ARRaycastManager>();
        }

        private void Update()
        {
            if (!TryGetTapPosition(out Vector2 tapPosition)) return;
            if (IsPointerOverUI()) return;

            if (raycastManager.Raycast(tapPosition, Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = Hits[0].pose;

                if (PlacedInstance == null)
                {
                    PlacedInstance = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);
                    OnObjectPlaced?.Invoke(PlacedInstance);
                    if (hidePlanesAfterFirstPlacement) SetPlanesVisible(false);
                }
                else
                {
                    // FR2 "and move": tapping a new spot after placement re-positions the character.
                    PlacedInstance.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                }
            }
        }

        private static bool TryGetTapPosition(out Vector2 position)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
                return true;
            }
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            position = default;
            return false;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void SetPlanesVisible(bool visible)
        {
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
        }

        public void ResetPlacement()
        {
            if (PlacedInstance != null) Destroy(PlacedInstance);
            PlacedInstance = null;
            SetPlanesVisible(true);
        }
    }
}
