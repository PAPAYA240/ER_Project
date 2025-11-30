Shader "Unlit/FX_BuffGlow_JS"
{
    Properties
    {
        _MainTex ("Glow Mask", 2D) = "white" {}   // 원형 마스크 텍스처
        _Color ("Glow Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1.0
        _Scale ("Scale", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend One One       // Additive 블렌딩
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _Intensity;
            float _Scale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 마스크 텍스처 샘플
                fixed4 mask = tex2D(_MainTex, i.uv);

                // Glow 색상 적용 + Intensity
                fixed4 col = _Color * mask.a * _Intensity;

                return col;
            }
            ENDCG
        }
    }
}