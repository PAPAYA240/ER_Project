Shader "Unlit/Common_MoveArrow_Arrow"
{
    Properties
    {
        _Color      ("Tint Color", Color) = (1,1,1,1)
        _MainTex    ("Arrow Tex (alpha only)", 2D) = "white" {}
        _MaskTex    ("Flow Mask Tex", 2D) = "white" {}
        _Speed      ("Flow Speed", Float) = 1
        _ArrowIntensity ("Arrow Intensity", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]
        ZTest [_ZTestMode]

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

            fixed4 _Color;
            float  _Speed;
            float  _ArrowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;     // Particle System vertex color (Color over Lifetime)
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvMask : TEXCOORD1;
                fixed4 color  : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                // 기본 UV
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);

                // 마스크 UV는 안쪽으로 흐르게 Y축으로 스크롤
                float2 muv = TRANSFORM_TEX(v.uv, _MaskTex);
                // 위 → 아래로 흐르게 하고 싶으면 +, 아래 → 위면 - 로 바꿔봐
                muv.x += _Time.y * _Speed;
                o.uvMask = muv;

                // 파티클 Color over Lifetime * 셰이더 Tint
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1) 화살표 텍스쳐는 흑/백 값으로만 알파를 만듦
                fixed4 arrowSample = tex2D(_MainTex, i.uvMain);

                // RGB → 밝기로 알파 추출 (흰색 화살표 부분만 1에 가깝게)
                float arrowAlpha = arrowSample.r/* dot(arrowSample.rgb, float3(0.299, 0.587, 0.114)) */;

                // 화살표 색은 그냥 흰색 + 강도
                fixed3 arrowRGB = fixed3(1,1,1) * _ArrowIntensity;

                // 2) 마스크 텍스쳐(GlowLine)를 스크롤해서 흐르는 느낌
                float maskVal = tex2D(_MaskTex, i.uvMask).r; // 회색값만 사용

                // 3) 최종 알파 = 화살표 마스크 * 흐름 마스크 * 파티클 색 알파
                float alpha = arrowAlpha * maskVal * i.color.a;

                // 4) 최종 색 = 화살표 색 * 파티클 Color over Lifetime
                fixed3 rgb = arrowRGB * i.color.rgb;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
