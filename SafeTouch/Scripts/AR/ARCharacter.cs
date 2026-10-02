using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Keza, the guide character the child places in the room. Lessons add
    /// their props (bubble rings, safe-place marker) under Keza so a new
    /// placement moves everything together.
    /// </summary>
    public class ARCharacter : ARFigure
    {
        readonly List<GameObject> props = new List<GameObject>();

        /// <summary>Creates a flat disc on the floor around Keza (bubble zones, safe place).</summary>
        public GameObject AddDisc(string discName, float radius, Color color, Material material, float heightOffset)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = discName;
            Destroy(disc.GetComponent<Collider>());
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            disc.transform.localScale = new Vector3(radius * 2f, 0.002f, radius * 2f);
            var r = disc.GetComponent<Renderer>();
            if (material != null) r.material = new Material(material);
            r.material.color = color;
            props.Add(disc);
            return disc;
        }

        public void AddProp(GameObject prop)
        {
            props.Add(prop);
        }

        /// <summary>Removes every lesson prop; called when a lesson ends.</summary>
        public void ClearProps()
        {
            foreach (var p in props) if (p != null) Destroy(p);
            props.Clear();
        }
    }
}
