Shader "Abigail/Q_Range"
{
    Properties
    {
        _Color("Color", Color) = (0.615, 0.306, 0.929, 0.95) // 보라 (#9D4EDD), 기본 알파 약간 높임
        _MainTex("Ring Texture", 2D) = "white" {}
        _PulseSpeed("Pulse Speed", Float) = 1.5
        _PulseAmp("Pulse Amp", Range(0,1)) = 0.12
        _MinBrightness("Min Brightness", Range(0.0, 1.0)) = 0.85
        _MaxBrightness("Max Brightness", Range(1.0, 3.0)) = 1.15
        _CenterPower("Center Contrast", Range(0.5, 4.0)) = 1.8 // 중심 강조 강도
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.05
        _EffectTime("Effect Time", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 particleColor : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _PulseSpeed;
            float _PulseAmp;
            float _MinBrightness;
            float _MaxBrightness;
            float _CenterPower;
            float _AlphaCutoff;
            float _EffectTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.particleColor = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 텍스처 마스크 값: 밝기 채널 사용 (r 채널 또는 rgb 평균)
                float mask = tex.r; // 마스크가 r에 들어있다면 이게 가장 안정적
                // 만약 알파로 마스크를 쓴다면: mask = tex.a;

                // 중심을 더 진하게: 마스크를 거듭 제곱(또는 pow)하여 중앙 강조
                float center = pow(saturate(mask), _CenterPower);

                // 펄스: EffectTime 기반으로 부드럽게 변동 (작게만 흔들리게)
                float pulse = sin(_EffectTime * _PulseSpeed) * 0.5 + 0.5; // 0..1
                float brightness = lerp(_MinBrightness, _MaxBrightness, pulse);
                brightness = lerp(brightness, 1.0 + _PulseAmp * (pulse - 0.5), 0.6); // 살짝 더 자연스러운 펄스 혼합

                // 최종 컬러: 중심 강조 반영
                float3 baseCol = _Color.rgb * brightness;
                // 중심(진하기)과 외곽(약하게) 섞기: center가 크면 색 강해짐
                float3 finalRGB = baseCol * lerp(0.6, 1.4, center); // 중앙은 최대 1.4배 정도 강해짐

                // 알파: mask 기반으로 외곽(검은부분) 완전 제거
                // calculatedAlpha는 텍스처 밝기 * 파티클 알파 * 컬러 알파
                float calcAlpha = mask * i.particleColor.a * _Color.a;

                // 중앙부는 약간 더 불투명하게 (옵션)
                calcAlpha *= lerp(0.9, 1.0, center);

                // 컷오프: 바깥 검은 영역 제거
                clip(calcAlpha - _AlphaCutoff);

                return fixed4(finalRGB, calcAlpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}