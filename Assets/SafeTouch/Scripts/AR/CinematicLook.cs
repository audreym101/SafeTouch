using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// The film look of the 3D world, on the camera while the playground shows (Stage3D adds and removes it):
    /// a filmic tone curve, a little more contrast and colour, cool shadows and warm light, and a soft vignette.
    /// Ages 9 to 11 get a deeper, more dramatic grade. One full-screen pass (Resources/Looks/Cinematic.shader);
    /// on a phone that cannot run it, the picture is simply shown as it is.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicLook : MonoBehaviour
    {
        public float exposure = 0.85f, contrast = 1.1f, saturation = 1.06f, vignette = 0.25f;
        public Color shadowTint = new Color(0.96f, 0.99f, 1.03f), lightTint = new Color(1.03f, 1.01f, 0.97f);

        Material material;
        bool unsupported;

        /// <summary>The grade for each path: bright and gentle for 6 to 8, deeper and warmer for 9 to 11.</summary>
        public void Preset(bool older)
        {
            exposure = older ? 0.78f : 0.85f;
            contrast = older ? 1.16f : 1.1f;
            saturation = older ? 1.1f : 1.06f;
            vignette = older ? 0.42f : 0.25f;
            shadowTint = older ? new Color(0.9f, 0.97f, 1.06f) : new Color(0.97f, 0.99f, 1.03f);
            lightTint = older ? new Color(1.07f, 1.01f, 0.92f) : new Color(1.03f, 1.01f, 0.97f);
        }

        /// <summary>The night grade, for every age: a little lifted so nothing is lost in the dark, blue shadows, warm lamplight.</summary>
        public void NightPreset()
        {
            exposure = 1.05f;
            contrast = 1.08f;
            saturation = 0.9f;
            vignette = 0.45f;
            shadowTint = new Color(0.86f, 0.94f, 1.14f);
            lightTint = new Color(1.08f, 1f, 0.9f);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null && !unsupported)
            {
                var shader = Resources.Load<Shader>("Looks/Cinematic");
                if (shader != null && shader.isSupported) material = new Material(shader) { hideFlags = HideFlags.DontSave };
                else unsupported = true;
            }
            if (material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }
            material.SetFloat("_Exposure", exposure);
            material.SetFloat("_Contrast", contrast);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Vignette", vignette);
            material.SetFloat("_Aspect", source.width / (float)Mathf.Max(source.height, 1));
            material.SetVector("_ShadowTint", shadowTint);
            material.SetVector("_LightTint", lightTint);
            Graphics.Blit(source, destination, material);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
