Shader "Abigail/R_crack_main"
{
    Properties
    {
        _MainTex ("Crack Texture", 2D) = "white" {}
        _GlowTex ("Glow Line Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
        _Cutoff ("Cutoff Threshold", Range(0, 1)) = 0.1
        _GlowSpeed ("Glow Speed", Range(0, 5)) = 1
        _GlowOffset ("Glow Offset", Range(0, 1)) = 0
        _BrightnessBoost ("Brightness Boost", Range(1, 3)) = 1.5
        
        // 시간에 따른 Cutoff 조정 관련 프로퍼티
        _MaxCutoff ("Max Cutoff", Range(0, 1)) = 1
        _StartTime ("Start Time", Float) = 0
    }
    
    SubShader
    {
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 glowcoord : TEXCOORD1;
                fixed4 color : COLOR;
                float elapsedTime : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _GlowTex;
            float4 _GlowTex_ST;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _Cutoff;
            float _GlowSpeed;
            float _GlowOffset;
            float _BrightnessBoost;
            
            // 시간 관련 프로퍼티
            float _MaxCutoff;
            float _StartTime;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // 글로우 UV 애니메이션
                float2 glowUV = v.texcoord;
                glowUV.x += _Time.y * _GlowSpeed + _GlowOffset;
                o.glowcoord = TRANSFORM_TEX(glowUV, _GlowTex);
                
                o.color = v.color;
                
                // 경과 시간 계산 (플레이 시작 시간부터)
                o.elapsedTime = max(0, _Time.y - _StartTime);
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainTex = tex2D(_MainTex, i.texcoord);
                float mask = mainTex.r;    // 크랙 마스크 값 (0 = 어두움, 1 = 밝음)

                // 시간에 따라 증가하는 컷오프
                float dynamicCutoff = lerp(_Cutoff, _MaxCutoff, saturate(i.elapsedTime / 1.1));

                // mask 값이 cutoff 보다 낮으면 바로 제거
                if (mask < dynamicCutoff)
                    discard;

                // 남아 있는 부분을 조금 더 선명하게 (선택)
                float crackAlpha = saturate((mask - dynamicCutoff) * 5.0);

                fixed4 finalColor = i.color;

                finalColor.a = crackAlpha * (1.0 - dynamicCutoff);

                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/VertexLit"
}