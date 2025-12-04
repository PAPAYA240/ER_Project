Shader "Abigail/W_Inner"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _DissolveMask("Dissolve Mask (Gradient)", 2D) = "white" {}
        
        _TintColor("Tint Color", Color) = (0.615, 0.305, 0.866, 1)
        
        // 타일링 설정
        _NoiseTiling("Noise Tiling", Float) = 3.0
        _MaskTiling("Mask Tiling", Float) = 1.0
        
        // 디버깅 옵션
        [Toggle]_ShowMaskOnly("Show Mask Only", Float) = 0
        [Toggle]_ShowNoiseOnly("Show Noise Only", Float) = 0
        [Toggle]_ShowDissolveOnly("Show Dissolve Only", Float) = 0
        [Toggle]_InvertMask("Invert Mask", Float) = 0
        
        // 효과 설정
        _Brightness("Brightness", Float) = 3.0
        _Contrast("Contrast", Float) = 1.5
        _MaskThreshold("Mask Threshold", Range(0, 1)) = 0.1
        _MaskSoftness("Mask Softness", Range(0, 0.5)) = 0.1
        
        // 노이즈 효과
        _NoiseScrollSpeed("Noise Scroll Speed", Float) = 0.2
        _NoiseIntensity("Noise Intensity", Float) = 2.0
        
        // 디졸브 효과 (그라데이션 활용)
        _DissolveProgress("Dissolve Progress", Range(0, 1)) = 0.0
        _DissolveDirection("Dissolve Direction", Range(-1, 1)) = 1.0
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.01, 0.5)) = 0.1
        _DissolveGlowColor("Dissolve Glow Color", Color) = (1, 0.8, 0.3, 1)
        _DissolveGlowIntensity("Dissolve Glow Intensity", Range(0, 5)) = 2.0
        
        // 그라데이션 효과
        _GradientPower("Gradient Power", Range(0.1, 3)) = 1.0
        _GradientOffset("Gradient Offset", Range(-1, 1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _SHOWMASKONLY_ON
            #pragma shader_feature _SHOWNOISEONLY_ON
            #pragma shader_feature _SHOWDISSOLVEONLY_ON
            #pragma shader_feature _INVERTMASK_ON
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float2 maskUV : TEXCOORD2;
                float4 vertexColor : COLOR;
            };
            
            sampler2D _NoiseTex;
            sampler2D _MaskTex;
            sampler2D _DissolveMask;
            float4 _TintColor;
            
            float _NoiseTiling;
            float _MaskTiling;
            
            float _Brightness;
            float _Contrast;
            float _MaskThreshold;
            float _MaskSoftness;
            
            float _NoiseScrollSpeed;
            float _NoiseIntensity;
            
            float _DissolveProgress;
            float _DissolveDirection;
            float _DissolveEdgeWidth;
            float4 _DissolveGlowColor;
            float _DissolveGlowIntensity;
            
            float _GradientPower;
            float _GradientOffset;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // 타일링 적용된 UV
                o.noiseUV = v.uv * _NoiseTiling;
                o.maskUV = v.uv * _MaskTiling;
                
                // 노이즈 UV 스크롤
                float time = _Time.y;
                o.noiseUV.x += time * _NoiseScrollSpeed * 0.1;
                o.noiseUV.y += time * _NoiseScrollSpeed * 0.05;
                
                o.vertexColor = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // 1. 텍스처 샘플링
                fixed4 noiseTex = tex2D(_NoiseTex, i.noiseUV);
                fixed4 maskTex = tex2D(_MaskTex, i.maskUV);
                fixed4 dissolveMask = tex2D(_DissolveMask, i.uv);  // 원본 UV 사용
                
                // 2. 밝기 값 추출
                float noiseValue = (noiseTex.r + noiseTex.g + noiseTex.b) / 3.0;
                float maskValue = (maskTex.r + maskTex.g + maskTex.b) / 3.0;
                float dissolveValue = (dissolveMask.r + dissolveMask.g + dissolveMask.b) / 3.0;
                
                // 3. 디버깅 모드
                #ifdef _SHOWMASKONLY_ON
                    return fixed4(maskValue, maskValue, maskValue, 1);
                #endif
                
                #ifdef _SHOWNOISEONLY_ON
                    return fixed4(noiseValue, noiseValue, noiseValue, 1);
                #endif
                
                #ifdef _SHOWDISSOLVEONLY_ON
                    return fixed4(dissolveValue, dissolveValue, dissolveValue, 1);
                #endif
                
                // 4. 마스크 처리
                #ifdef _INVERTMASK_ON
                    maskValue = 1.0 - maskValue;
                #endif
                
                // 부드러운 마스크 임계값
                float finalMask = smoothstep(_MaskThreshold - _MaskSoftness, 
                                            _MaskThreshold + _MaskSoftness, 
                                            maskValue);
                
                // 마스크가 없으면 완전히 제거
                if (finalMask < 0.01)
                {
                    discard;
                }
                
                // 5. 노이즈 강도 적용
                noiseValue *= _NoiseIntensity;
                
                // 6. 디졸브 마스크 처리 (그라데이션 활용)
                // 그라데이션 방향 조정
                float gradientValue = dissolveValue;
                
                // 그라데이션 파워 및 오프셋 적용
                gradientValue = pow(gradientValue, _GradientPower);
                gradientValue = saturate(gradientValue + _GradientOffset);
                
                // 디졸브 진행도에 따른 컷오프
                float dissolveCutoff = step(gradientValue, _DissolveProgress);
                
                // 디졸브 에지 효과
                float dissolveEdge = smoothstep(_DissolveProgress - _DissolveEdgeWidth,
                                               _DissolveProgress + _DissolveEdgeWidth,
                                               gradientValue);
                
                // 디졸브 에지 빛 효과
                float edgeGlowFactor = 1.0 - dissolveEdge;
                float3 edgeGlow = _DissolveGlowColor.rgb * edgeGlowFactor * _DissolveGlowIntensity;
                
                // 7. 색상 계산
                fixed4 col = _TintColor;
                
                // 대비 적용
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                
                // 노이즈와 디졸브 결합
                float combinedEffect = noiseValue * dissolveEdge;
                col.rgb *= combinedEffect * _Brightness;
                
                // 에지 빛 효과 추가
                col.rgb += edgeGlow;
                
                // 8. 알파 계산
                // 마스크 × 디졸브 에지
                col.a = finalMask * dissolveEdge * _TintColor.a;
                
                // 디졸브 컷오프 부분은 완전히 투명하게
                col.a *= (1.0 - dissolveCutoff);
                
                // 알파 체크
                clip(col.a - 0.05);
                
                // 9. 파티클 색상 적용
                col *= i.vertexColor;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Unlit/Transparent"
}