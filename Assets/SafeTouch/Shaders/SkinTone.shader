// Recolours only the skin of a textured character. A mask texture (red
// channel) marks skin; masked pixels take _SkinColor, scaled by how light or
// dark the original pixel was compared with _SkinReference, so shading
// painted into the texture (cheeks, knuckles, shadows) is kept. Clothes,
// hair and eyes are untouched.
Shader "SafeTouch/Skin Tone"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _SkinMask ("Skin mask (R)", 2D) = "black" {}
        _SkinColor ("Skin colour", Color) = (0.553, 0.333, 0.141, 1)
        _SkinReference ("Painted skin colour", Color) = (0.95, 0.69, 0.69, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SkinMask;
        fixed4 _Color;
        fixed4 _SkinColor;
        fixed4 _SkinReference;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half mask = tex2D(_SkinMask, IN.uv_MainTex).r;
            half shade = Luminance(c.rgb) / max(Luminance(_SkinReference.rgb), 0.01);
            fixed3 skin = saturate(_SkinColor.rgb * shade);
            o.Albedo = lerp(c.rgb, skin, mask);
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
