Shader "ERBS_FX/FX_AdditiveUI_Glow_JS"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("MainTex", 2D) = "white" {}
        _GlowStrength ("Glow Strength", Range(0,2)) = 0.5
        _GlowWidth ("Glow Width", Range(0,3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;
            float4 _MainTex_ST;

            struct Vertex_Stage_Input
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Vertex_Stage_Output
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            Vertex_Stage_Output vert(Vertex_Stage_Input input)
            {
                Vertex_Stage_Output o;
                o.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
                return o;
            }

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;

            float4 _Color;
            float _GlowStrength;
            float _GlowWidth;

            float4 frag(Vertex_Stage_Output input) : SV_Target
            {
                float2 uv = input.uv;

                // 원본 텍스처 샘플
                float4 tex = _MainTex.Sample(sampler_MainTex, uv);

                float2 offset = float2(_GlowWidth * 0.005, _GlowWidth * 0.005);

                float4 glow =
                    _MainTex.Sample(sampler_MainTex, uv + float2( offset.x, 0)) +
                    _MainTex.Sample(sampler_MainTex, uv + float2(-offset.x, 0)) +
                    _MainTex.Sample(sampler_MainTex, uv + float2(0,  offset.y)) +
                    _MainTex.Sample(sampler_MainTex, uv + float2(0, -offset.y));

                glow *= 0.25; // 평균값
                glow.rgb *= _Color.rgb * _GlowStrength;

                // 본체 색
                tex.rgb *= _Color.rgb;

                // 알파 페이드
                tex *= _Color.a;

                // Additive 결과 = 원본 + 글로우
                return tex + glow * _Color.a;
            }

            ENDHLSL
        }
    }
}