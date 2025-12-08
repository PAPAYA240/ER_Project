Shader "Unlit/Common_Ping_Bound"
{
    Properties
    {
        _Color          ("Tint Color", Color) = (1,1,1,1)
        _MainTex        ("MainTex", 2D) = "white" {}
        _MaskTex        ("MaskTex", 2D) = "white" {}

        _MulVal_rgb     ("RGB Multiplier",   Float) = 1
        _MulVal_alpha   ("Alpha Multiplier", Float) = 1

        _ShrinkSpeed    ("Shrink Speed",     Float)        = 1.5   // 전체 애니메이션 속도
        _MinScale       ("Min Scale",        Range(0.1,1)) = 0.3   // 가장 작아질 크기
        _ScaleCurve     ("Scale Curve",      Range(1,4))   = 2.0   // 커브(클수록 초반 느리고 후반 빨라짐)

        _RingCount      ("Ring Count",       Range(1,3))   = 3     // 동시에 보일 링 개수
        _RingSpacing    ("Ring Spacing",     Range(0.1,1)) = 0.3   // 링 사이 시간 간격(0~1)

        _PulseAmp       ("Brightness Pulse", Range(0,2))   = 0.3   // 밝기 숨쉬기(0이면 고정)
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
                float4 color      : COLOR;     // StartColor
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
                float  _ScaleCurve;

                float  _RingCount;
                float  _RingSpacing;

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
                OUT.uv = IN.uv;
                OUT.color = IN.color; // StartColor
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 기본 uv + ST
                float2 uvMain0 = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvMask0 = IN.uv * _MaskTex_ST.xy + _MaskTex_ST.zw;

                float2 centeredMain = uvMain0 - 0.5;
                float2 centeredMask = uvMask0 - 0.5;

                // 전역 시간 (여러 링에 공유)
                float tGlobal = _Time.y * _ShrinkSpeed;

                half3 accumRGB = half3(0,0,0);
                half  accumA   = 0;

                // 최대 3개의 링
                [unroll]
                for (int i = 0; i < 3; ++i)
                {
                    if (i >= (int)_RingCount)
                        break;

                    // 각 링의 시작 시간을 어긋나게 해서 여러 개가 동시에 보이게
                    float t = frac(tGlobal - i * _RingSpacing); // 0~1

                    // 아직 시작 전이나(이론상 0~1이므로 필요 없지만) 버릴 조건이 있으면 여기서 continue 가능

                    // 1 → MinScale 로만 줄어드는 스케일, 초반 느리고 후반 빨라지게
                    float eased  = pow(t, _ScaleCurve);                // ease-in
                    float scale  = lerp(1.0, _MinScale, eased);
                    scale = max(scale, 0.01);

                    float2 sampleMainUV = centeredMain / scale + 0.5;
                    float2 sampleMaskUV = centeredMask / scale + 0.5;

                    // 텍스쳐 영역 밖이면 이 링은 무시 (다른 링은 계속)
                    if (sampleMainUV.x < 0.0 || sampleMainUV.x > 1.0 ||
                        sampleMainUV.y < 0.0 || sampleMainUV.y > 1.0 ||
                        sampleMaskUV.x < 0.0 || sampleMaskUV.x > 1.0 ||
                        sampleMaskUV.y < 0.0 || sampleMaskUV.y > 1.0)
                    {
                        continue;
                    }

                    half4 mainC = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleMainUV);
                    half  mask  = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, sampleMaskUV).r;

                    half  a = mainC.a * mask;
                    half3 rgb = mask;

                    // 링끼리는 단순히 더해서 중첩(필요하면 max로 바꿔도 됨)
                    accumRGB += rgb;
                    accumA   = max(accumA, a);
                }

                // 어떤 링도 그려질 게 없으면 discard
                if (accumA <= 0.0001h)
                    clip(-1);

                // 기본 색 / StartColor 반영
                half4 col = half4(accumRGB, accumA);
                half4 tint = _Color * IN.color;
                col.rgb *= tint.rgb;
                col.a   *= tint.a;

                // 살짝 밝기 숨쉬기 (싫으면 _PulseAmp = 0)
                float tNorm = frac(tGlobal);
                float brightnessPulse = 1.0 + sin(tNorm * 6.28318) * _PulseAmp;
                col.rgb *= brightnessPulse * _MulVal_rgb;
                col.a   *= _MulVal_alpha;

                clip(col.a - 0.001h);
                return col;
            }
            ENDHLSL
        }
    }
}
