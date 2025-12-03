Shader "Abigail/Q_Range"
{
    Properties
    {
        _Color("Color", Color) = (0.615, 0.306, 0.929, 0.8) // 보라색 (#9D4EDD)
        _MainTex("Ring Texture", 2D) = "white" {}
        _PulseSpeed("Pulse Speed", Float) = 1.5
        _MinBrightness("Min Brightness", Range(0.0, 1.0)) = 0.85
        _MaxBrightness("Max Brightness", Range(1.0, 3.0)) = 1.2
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.05 // 바깥 검은 부분 제거를 위해 0.05로 기본값 설정
        _EffectTime("Effect Time", Float) = 0.0 // 파티클 시스템의 시간에 맞춰 펄스할 변수 추가
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
            #include "UnityCG.cginc" // 안전하게 .cginc 사용

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
            float _MinBrightness;
            float _MaxBrightness;
            float _AlphaCutoff;
            float _EffectTime; // 스크립트에서 값을 넘겨받을 변수

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

                // _EffectTime 변수를 사용하여 펄스 효과 계산
                float pulse = (sin(_EffectTime * _PulseSpeed) + 1.0) * 0.5;
                float brightness = lerp(_MinBrightness, _MaxBrightness, pulse);

                // 최종 색상 계산
                fixed3 finalRGB = _Color.rgb * brightness;
                finalRGB *= tex.r; // 텍스쳐 밝기 정보로 원형 형태 및 밝기 보정

                // 알파 값 계산 강화 (텍스쳐의 r 채널도 활용하여 검은 부분 투명 처리)
                float calculatedAlpha = tex.r * tex.a * i.particleColor.a * _Color.a;
                
                // 알파 컷오프를 사용하여 명확한 경계 생성
                clip(calculatedAlpha - _AlphaCutoff);

                return fixed4(finalRGB, calculatedAlpha);
            }
            ENDCG
        }
    }
}