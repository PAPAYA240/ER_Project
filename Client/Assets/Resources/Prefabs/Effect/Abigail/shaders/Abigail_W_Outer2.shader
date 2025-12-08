Shader "Abigail/W_Outer2"
{
    Properties
    {
        _MainTex ("Main Texture (Mask)", 2D) = "white" {}
        _StartTime ("Start Time", Float) = 0.0
        _Duration ("Duration", Float) = 0.57
        _InvertY ("Invert Y", Float) = 0.0
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 2)) = 1.0
        _EdgeWidth ("Edge Width", Float) = 0.01
        _MaskThreshold ("Mask Threshold", Range(0, 1)) = 0.5
        
        // 빛나는 효과 관련 프로퍼티
        _BrightnessStart ("Brightness Start", Range(0.5, 2)) = 1.0
        _BrightnessEnd ("Brightness End", Range(1, 5)) = 2.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _StartTime;
            float _Duration;
            float _InvertY;
            float _AlphaMultiplier;
            float _EdgeWidth;
            float _MaskThreshold;
            
            // 빛나는 효과 관련
            float _BrightnessStart;
            float _BrightnessEnd;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Color over Lifetime
                float4 customData1 : TEXCOORD1; // Custom Data 1
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 colorOverLifetime : COLOR0;
                float4 customColor : COLOR1;
                float time : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.colorOverLifetime = v.color;
                o.customColor = float4(v.customData1.rgb, 1.0);
                o.time = _Time.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 메인 텍스처(마스크) 샘플링
                fixed4 tex = tex2D(_MainTex, i.uv);
                
                // 흑백 마스크 텍스처에서 밝기 값 추출
                float maskValue = tex.r; // R 채널 사용 (흑백이므로 R=G=B)
                
                // 0.5 미만이면 discard
                if (maskValue < _MaskThreshold)
                    discard;
                
                // 시간 계산
                float elapsedTime = i.time - _StartTime;
                float t = max(0, elapsedTime);
                
                // 진행도 계산
                float initialFill = 0.3;
                float progressTime = 0.25;
                float prog = (_Duration > 0.0001) ? saturate(t / progressTime) : 1.0;
                
                // Y 좌표 조정
                float uvY = i.uv.y;
                if (_InvertY >= 0.5) 
                    uvY = 1.0 - uvY;
                
                // 채우기 레벨 계산
                float fillLevel = initialFill + (1.0 - initialFill) * prog;
                float edge = _EdgeWidth;
                float fillMask = smoothstep(fillLevel + edge, fillLevel - edge, uvY);
                
                // 기본 색상 계산: Color over Lifetime × Custom Data
                fixed4 baseColor = i.colorOverLifetime * i.customColor;
                
                // ============================
                // 더 강력한 빛나는 효과
                // ============================
                
                // 1. 진행도에 따른 밝기 증가 (선형 보간)
                float progressBrightness = lerp(_BrightnessStart, _BrightnessEnd, prog);
                
                // 2. 펄스 효과 (시간에 따라 진동)
                float pulse = 1.0 + sin(i.time * _PulseSpeed) * _PulseAmount;
                
                // 3. 최종 밝기 계산
                float finalBrightness = progressBrightness * pulse;
                
                // 4. 밝기 적용 - 색상을 포화시키지 않고 그대로 곱하기
                fixed3 brightColor = baseColor.rgb * finalBrightness;
                
                // 5. HDR 효과처럼 보이도록 하려면 약간의 색상 보정
                // (선택사항: 더 화려한 효과를 원하면 사용)
                // brightColor = brightColor / (1.0 + brightColor);
                
                // 차오른 부분의 밝아진 색상
                fixed4 filledColor = fixed4(brightColor, baseColor.a);
                
                // 안 차오른 부분 처리
                fixed4 finalColor;
                if (fillMask > 0.5) // 차오른 부분
                {
                    finalColor = filledColor;
                }
                else // 안 차오른 부분
                {
                    finalColor = fixed4(0, 0, 0, baseColor.a); // 검은색 RGB, baseColor의 알파
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}