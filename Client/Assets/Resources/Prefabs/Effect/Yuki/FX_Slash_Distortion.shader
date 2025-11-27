Shader "ERBS_FX/FX_Slash_Distortion"
{
    Properties
    {
        _Color ("Color", Color) = (0.1,0.4,0.8,1)
        _EmissStrength ("Emiss Strength", Float) = 2
        _MainTex ("Main Texture", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _FlowStrength ("Flow Strength", Float) = 0.3
        _FlowSpeed ("Flow Speed", Float) = 1
        _DistortionStrength ("Distortion Strength", Float) = 0.05
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.05
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        [Toggle] _ZWrite ("ZWrite Mode", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha One
            ZWrite [_ZWrite]
            Cull [_Cull]
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _FlowMap;
            float4 _Color;
            float _FlowStrength;
            float _FlowSpeed;
            float _EmissStrength;
            float _DistortionStrength;
            float _AlphaCutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Particle Lifetime Alpha
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvFlow : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // FlowMap 방향 반전 제거 → 왼쪽 → 오른쪽
                float2 flowUV = v.uv; 
                o.uvFlow = flowUV + _Time.y * _FlowSpeed;

                o.color = v.color; // Particle Lifetime Alpha
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 시간 기반 UV 흔들림
                float t = _Time.y * _FlowSpeed;

                float2 distortion;
                distortion.x = sin(i.uv.y * 10.0 + t) * _DistortionStrength;
                distortion.y = cos(i.uv.x * 10.0 + t) * _DistortionStrength;
                float2 distortedUV = i.uv + distortion;

                // MainTex 샘플
                float4 tex = tex2D(_MainTex, distortedUV);

                // FlowMap 샘플
                float4 flow = tex2D(_FlowMap, i.uvFlow);

                // Emission 계산
                float emiss = flow.r * _FlowStrength * _EmissStrength;
                float4 col = tex * _Color + tex * emiss;

                // Particle Lifetime Alpha 적용
                col.a *= i.color.a;

                // Tail Fade (왼쪽 머리 → 오른쪽 꼬리)
                float tailFade = i.uv.x;
                col.a *= tailFade;

                // Alpha Cutoff
                if(col.a < _AlphaCutoff) discard;

                return col;
            }

            ENDHLSL
        }
    }

    Fallback "Transparent/Diffuse"
}
