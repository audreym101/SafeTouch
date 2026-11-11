// The film look of the 3D world (CinematicLook): exposure and a filmic (ACES) tone curve, contrast and
// colour, cool shadows and warm light, and a soft vignette. One cheap pass, for phones.
Shader "Hidden/SafeTouch/Cinematic"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure, _Contrast, _Saturation, _Vignette, _Aspect;
            float3 _ShadowTint, _LightTint;

            float3 Filmic(float3 x)
            {
                // Narkowicz's fit of the ACES curve: rich mid-tones, highlights that roll off instead of clipping.
                return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                c = c * c;                                  // the picture is in gamma: about linear
                c = Filmic(c * _Exposure);
                c = sqrt(c);                                // and back
                float grey = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(grey.xxx, c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;
                c *= lerp(_ShadowTint, _LightTint, smoothstep(0.1, 0.9, grey));
                float2 d = (i.uv - 0.5) * float2(_Aspect, 1.0);
                c *= 1.0 - _Vignette * smoothstep(0.35, 0.95, length(d) * 1.25);
                return fixed4(saturate(c), 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
