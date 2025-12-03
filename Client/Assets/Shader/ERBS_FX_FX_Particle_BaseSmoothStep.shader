Shader "ERBS_FX/Smoothstep_Dissolve_JS"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)

        _MainTex ("Main Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}

        _Speed ("Rotation Speed", Float) = 1

        _Dissolve ("Dissolve (0=Full,1=Gone)", Range(0,1)) = 0
        _Smooth ("Smoothstep Width", Range(0,0.2)) = 0.05

        _OuterStrength ("Outer Flow Strength", Range(0,3)) = 1.0
        _OuterRadius ("Outer Radius", Float) = 0.5
    }

    SubShader
    {
        Tags{
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;

            float4 _Color;
            float _Speed;
            float _Dissolve;
            float _Smooth;

            float _OuterStrength;
            float _OuterRadius;

            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---------------------------
                // 1. 회전
                // ---------------------------
                float t = _Time.y * _Speed;
                float s = sin(t);
                float c = cos(t);

                float2 uv = i.uv - 0.5;
                float2 ruv = float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                ) + 0.5;

                // ---------------------------
                // 2. 기본 텍스처
                // ---------------------------
                float4 mainCol = tex2D(_MainTex, ruv);
                float mask = tex2D(_MaskTex, ruv).r;

                // ---------------------------
                // 3. Smoothstep Dissolve
                // ---------------------------
                float dissolveEdge = smoothstep(_Dissolve - _Smooth, _Dissolve + _Smooth, mask);

                // ---------------------------
                // 4. 도넛 바깥 방향 흐름 추가
                // (worldPos 거리 기반)
                // ---------------------------

                float3 center = float3(0,0,0);           // local origin (donut center)
                float dist = length(i.worldPos - center);

                float outerGlow = saturate( (dist - _OuterRadius) * _OuterStrength );

                // 바깥쪽에서 알파 증가
                dissolveEdge = saturate(dissolveEdge + outerGlow);

                mainCol.a *= dissolveEdge;

                return mainCol * _Color;
            }
            ENDCG
        }
    }
}