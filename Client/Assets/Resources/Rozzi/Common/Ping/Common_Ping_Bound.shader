Shader "Unlit/Common_Ping_Bound"
{
    Properties
    {
        _Color          ("Tint Color", Color) = (1,1,1,1)
        _MainTex        ("MainTex", 2D) = "white" {}
        _MaskTex        ("MaskTex", 2D) = "white" {}

        // 아래 값들은 스크립트에서 SetFloat 하고 있다면 그대로 사용됨
        _MulVal_rgb     ("RGB Multiplier", Float)   = 1
        _MulVal_alpha   ("Alpha Multiplier", Float) = 1

        _ShrinkSpeed    ("Shrink Speed", Float) = 1
        _MinScale       ("Min Scale", Float) = 0.15
        _ScaleCurve     ("Scale Curve", Float) = 1

        _RingCount      ("Ring Count (0~3)", Range(0,3)) = 3
        _RingSpacing    ("Ring Spacing", Float) = 0.33

        _PulseAmp       ("Pulse Amplitude", Float) = 0.25

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("ZTest", Float) = 4
    }

    SubShader
    {
        // MoveArrow와 동일하게 URP 태그 빼고 빌트인 방식으로
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;

            sampler2D _MaskTex;
            float4    _MaskTex_ST;

            fixed4 _Color;

            float  _MulVal_rgb;
            float  _MulVal_alpha;

            float  _ShrinkSpeed;
            float  _MinScale;
            float  _ScaleCurve;

            float  _RingCount;
            float  _RingSpacing;

            float  _PulseAmp;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 기본 UV 변환
                float2 uvMain0 = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvMask0 = i.uv * _MaskTex_ST.xy + _MaskTex_ST.zw;

                // 가운데(0.5, 0.5)를 기준으로 원형 스케일링
                float2 centeredMain = uvMain0 - 0.5;
                float2 centeredMask = uvMask0 - 0.5;

                // 시간에 따른 전체 진행도
                float tGlobal = _Time.y * _ShrinkSpeed;

                // 링 개수는 0~3 사이
                float ringCount = clamp(_RingCount, 0.0, 3.0);

                float3 accumRGB = float3(0,0,0);
                float  accumA   = 0.0;

                [unroll]
                for (int idx = 0; idx < 3; ++idx)
                {
                    float fi = (float)idx;

                    // 현재 링 인덱스가 ringCount 안에 들어오면 1, 아니면 0
                    float enabled = step(fi, ringCount - 0.5);

                    // 링별로 시차를 둠
                    float t      = frac(tGlobal - fi * _RingSpacing);
                    float eased  = pow(t, _ScaleCurve);
                    float scale  = lerp(1.0, _MinScale, eased);
                    scale        = max(scale, 0.01);

                    float2 sampleMainUV = centeredMain / scale + 0.5;
                    float2 sampleMaskUV = centeredMask / scale + 0.5;

                    // 0~1 범위 안인지 체크
                    float inMain =
                        step(0.0, sampleMainUV.x) * step(0.0, sampleMainUV.y) *
                        step(sampleMainUV.x, 1.0) * step(sampleMainUV.y, 1.0);

                    float inMask =
                        step(0.0, sampleMaskUV.x) * step(0.0, sampleMaskUV.y) *
                        step(sampleMaskUV.x, 1.0) * step(sampleMaskUV.y, 1.0);

                    float inside = inMain * inMask;
                    float weight = enabled * inside;

                    fixed4 mainC = tex2D(_MainTex, sampleMainUV);
                    fixed  mask  = tex2D(_MaskTex, sampleMaskUV).r;

                    float a   = mainC.a * mask * weight;
                    float3 rgb = mask * weight;

                    accumRGB += rgb;
                    accumA   = max(accumA, a);
                }

                if (accumA <= 0.0001)
                    discard;

                fixed4 col  = fixed4(accumRGB, accumA);
                fixed4 tint = _Color * i.color;

                col.rgb *= tint.rgb;
                col.a   *= tint.a;

                // 전체 Ping에 약간의 밝기 펄스
                float tNorm = frac(tGlobal);
                float brightnessPulse = 1.0 + sin(tNorm * 6.28318) * _PulseAmp;

                col.rgb *= brightnessPulse * _MulVal_rgb;
                col.a   *= _MulVal_alpha;

                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
}
