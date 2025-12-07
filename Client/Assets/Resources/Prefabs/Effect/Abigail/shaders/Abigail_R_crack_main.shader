Shader "Abigail/R_crack_main"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveWidth ("Dissolve Width", Range(0, 0.2)) = 0.1
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 2
        _UseMask ("Use Mask", Range(0, 1)) = 1
        _TimeScale ("Time Scale", Float) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _DissolveAmount;
            float _DissolveWidth;
            float4 _EdgeColor;
            float _EdgeIntensity;
            float _UseMask;
            float _TimeScale;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 시간에 따른 변화
                float time = _Time.y * _TimeScale;
                
                // 마스크 텍스처 샘플링
                float mask = tex2D(_MaskTex, i.uv).r;
                
                // 노이즈 텍스처 샘플링 (UV에 시간 더해서 애니메이션)
                float2 noiseUV = i.uv + float2(time * 0.1, time * 0.1);
                float noise = tex2D(_NoiseTex, noiseUV).r;
                
                // 마스크와 노이즈 결합
                float combinedNoise = noise;
                if (_UseMask > 0.5)
                {
                    combinedNoise = lerp(noise, noise * mask, _UseMask);
                }
                
                // 디졸브 계산
                float dissolve = step(combinedNoise - _DissolveAmount, 0);
                
                // 가장자리 계산
                float edge = smoothstep(0, _DissolveWidth, combinedNoise - _DissolveAmount);
                float edgeGlow = (1 - edge) * _EdgeIntensity;
                
                // 메인 텍스처 샘플링
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // 디졸브 적용
                col.a *= dissolve;
                
                // 가장자리 색상 추가
                col.rgb += edgeGlow * _EdgeColor.rgb * _EdgeColor.a;
                
                // 완전히 디졸브된 부분 클리핑
                clip(col.a - 0.001);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Color"
}