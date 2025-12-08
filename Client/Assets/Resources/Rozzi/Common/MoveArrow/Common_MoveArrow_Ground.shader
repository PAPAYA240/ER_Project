Shader "Unlit/Common_MoveArrow_Ground"
{
    Properties
    {
        _Color          ("Color", Color) = (0.5,0.5,0.5,1)
        _MainTex        ("Hex Texture", 2D) = "white" {}   // FX_BI_Hexa_01
        _MaskTex        ("Radial Mask", 2D) = "white" {}   // Fx_Glow_07 (바깥 검, 중심 흰)
        _MulVal_rgb     ("RGB Multiplier", Float) = 1
        _MulVal_alpha   ("Alpha Multiplier", Float) = 1
        _CenterGlow     ("Center Glow Strength", Float) = 2 // 1이 기본, 값↑ = 중앙 더 밝게

        [Space(20)]
        [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)]
        _ZTestMode ("ZTest Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTestMode]
        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _MaskTex;
            float4    _MaskTex_ST;
            fixed4    _Color;
            float     _MulVal_rgb;
            float     _MulVal_alpha;
            float     _CenterGlow;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;     // Particle System Color / Color over Lifetime
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;        // 파티클 색 전달
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Hex 패턴
                fixed4 mainCol = tex2D(_MainTex, i.uv);

                // 바깥으로 갈수록 0이 되는 Radial 마스크
                float2 maskUV = TRANSFORM_TEX(i.uv, _MaskTex);
                fixed  radial = tex2D(_MaskTex, maskUV).r;

                // 기본 색: MainTex * Material Color * Particle Color
                fixed3 rgb = _Color.rgb * i.color.rgb;

                // 중앙 Glow 강화 (radial 1 = 중심, 0 = 바깥)
                // lerp(1, _CenterGlow, radial) 로 중심만 더 곱해줌
                float glowFactor = lerp(1.0, _CenterGlow, radial);
                rgb *= glowFactor * _MulVal_rgb;

                // 알파: 바깥으로 갈수록 흐려지도록 radial 곱
                fixed alpha =
                    mainCol.r *
                    _Color.a *
                    i.color.a *   // Color over Lifetime의 알파 반영
                    radial *
                    _MulVal_alpha;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
