Shader "Abigail/AxeTrail"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _FlowStrength ("Flow Strength", Float) = 0.2
        _FlowSpeed ("Flow Speed", Float) = 0.5
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.1
        _ColorIntensity ("Color Intensity", Range(0, 2)) = 1.0
        _BrightnessCutoff ("Brightness Cutoff", Range(0, 1)) = 0.1
        [Toggle] _UseFlowmap ("Use Flow Map", Float) = 1
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _USEFLOWMAP_ON
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FlowMap;
            float4 _FlowMap_ST;
            float _FlowStrength;
            float _FlowSpeed;
            float _AlphaThreshold;
            float _ColorIntensity;
            float _BrightnessCutoff;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                #ifndef _USEFLOWMAP_ON
                fixed4 texColor = tex2D(_MainTex, i.uv);
                #else
                float2 flowUV = i.uv * _FlowMap_ST.xy + _FlowMap_ST.zw;
                float2 flowVector = tex2D(_FlowMap, flowUV).rg * 2.0 - 1.0;
                
                float flowTime = _Time.y * _FlowSpeed;
                float phase0 = frac(flowTime);
                float phase1 = frac(flowTime + 0.5);
                
                float lerpFactor = abs((0.5 - phase0) * 2.0);
                
                float2 flowOffset0 = flowVector * phase0 * _FlowStrength;
                float2 flowOffset1 = flowVector * phase1 * _FlowStrength;
                
                fixed4 texColor0 = tex2D(_MainTex, i.uv + flowOffset0);
                fixed4 texColor1 = tex2D(_MainTex, i.uv + flowOffset1);
                
                fixed4 texColor = lerp(texColor0, texColor1, lerpFactor);
                #endif
                
                // 텍스쳐의 밝기 계산
                float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                
                // 밝기 임계값으로 알파 조절
                float alphaFromBrightness = smoothstep(_BrightnessCutoff, _BrightnessCutoff + 0.2, brightness);
                
                // 텍스쳐 알파와 밝기 알파 결합
                float textureAlpha = texColor.a * alphaFromBrightness;
                
                // 파티클 알파와 결합
                float finalAlpha = textureAlpha * i.color.a;
                
                if (finalAlpha < _AlphaThreshold)
                {
                    discard;
                }
                
                // 어두운 부분 처리
                fixed3 adjustedColor = texColor.rgb;
                
                if (brightness < _BrightnessCutoff)
                {
                    adjustedColor = fixed3(0, 0, 0);
                }
                else
                {
                    float t = (brightness - _BrightnessCutoff) / (1.0 - _BrightnessCutoff);
                    adjustedColor = texColor.rgb * (1.0 + t * 0.5);
                }
                
                // 파티클 색상 적용
                fixed3 finalRGB = adjustedColor * /* i.color.rgb * */ _ColorIntensity;
                
                fixed4 finalColor = fixed4(finalRGB, finalAlpha);
                
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Transparent/VertexLit"
}