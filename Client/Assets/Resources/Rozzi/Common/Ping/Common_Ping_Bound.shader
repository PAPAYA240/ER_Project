Shader "Unlit/Common_Ping_Bound"
{
    Properties
    {
        _Color          ("Tint Color", Color) = (1,1,1,1)
        _MainTex        ("MainTex", 2D) = "white" {}
        _MaskTex        ("MaskTex", 2D) = "white" {}

        _MulVal_rgb     ("RGB Multiplier",   Float) = 1
        _MulVal_alpha   ("Alpha Multiplier", Float) = 1

        _ShrinkSpeed    ("Shrink Speed",     Float)      = 1.5   // 줄어드는 속도
        _MinScale       ("Min Scale",        Range(0.1,1)) = 0.3  // 가장 작아질 크기
        _PulseAmp       ("Pulse Amplitude",  Range(0,2))  = 0.5   // 밝기 요동 세기
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;     // Start Color
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float  _MulVal_rgb;
                float  _MulVal_alpha;
                float  _ShrinkSpeed;
                float  _MinScale;
                float  _PulseAmp;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Tiling/Offset 적용용 기본 uv
                OUT.uv = IN.uv;
                OUT.color = IN.color; // Start Color
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 0~1 반복되는 타이머
                float cycle = frac(_Time.y * _ShrinkSpeed);
            
                // 1(큰 원) → _MinScale(작은 원) 으로 줄어듦
                float scale = lerp(1.0, _MinScale, cycle);
                scale = max(scale, 0.01);      // 0으로 나누기 방지
            
                // 기본 uv + 타일/오프셋
                float2 uvMain = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvMask = IN.uv * _MaskTex_ST.xy + _MaskTex_ST.zw;
            
                float2 centeredMain = uvMain - 0.5;
                float2 centeredMask = uvMask - 0.5;
            
                // scale 이 작아질수록 링이 안쪽으로 줄어드는 것처럼 보임
                float2 sampleMainUV = centeredMain / scale + 0.5;
                float2 sampleMaskUV = centeredMask / scale + 0.5;
            
                // ★ 0~1 범위 밖은 버려서 주변에 생기는 반복 원 제거
                if (sampleMainUV.x < 0.0 || sampleMainUV.x > 1.0 ||
                    sampleMainUV.y < 0.0 || sampleMainUV.y > 1.0 ||
                    sampleMaskUV.x < 0.0 || sampleMaskUV.x > 1.0 ||
                    sampleMaskUV.y < 0.0 || sampleMaskUV.y > 1.0)
                {
                    clip(-1); // 완전 discard
                }
            
                half4 mainC = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleMainUV);
                half  mask  = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, sampleMaskUV).r;
            
                // 기본 색 = Tint * StartColor
                half4 col = _Color * IN.color;
            
                // 살짝 요동치는 밝기 (수치 싫으면 _PulseAmp = 0)
                float pulse = 1.0 + sin(cycle * 6.28318) * _PulseAmp; // 2π
                col.rgb *= pulse * _MulVal_rgb;
            
                col.a *= mask * _MulVal_alpha;
            
                clip(col.a - 0.001h);
                return col;
            }
            ENDHLSL
        }
    }
}
