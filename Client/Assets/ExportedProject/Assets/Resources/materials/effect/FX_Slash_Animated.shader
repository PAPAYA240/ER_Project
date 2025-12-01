Shader "ERBS_FX/FX_Slash_Animated"
{
    Properties
    {
        _Color ("Color", Color) = (0.1,0.4,0.8,1)
        _EmissStrength ("Emiss Strength", Float) = 2
        _MainTex ("Main Texture", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _FlowStrength ("Flow Strength", Float) = 0.3
        _FlowSpeed ("Flow Speed", Float) = 1
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

                // 오른쪽 → 왼쪽 FlowMap UV 반전
                float2 flowUV = v.uv;
                flowUV.x = 1.0 - flowUV.x;
                o.uvFlow = flowUV + _Time.y * _FlowSpeed;

                o.color = v.color; // Particle Alpha
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float4 flow = tex2D(_FlowMap, i.uvFlow);

                float emiss = flow.r * _FlowStrength * _EmissStrength;
                float4 col = tex * _Color + tex * emiss;

                // Particle Lifetime Alpha 적용 → 이동하면서 서서히 사라짐
                col.a *= i.color.a;

                if(col.a < _AlphaCutoff) discard;

                return col;
            }

            ENDHLSL
        }
    }

    Fallback "Transparent/Diffuse"
}