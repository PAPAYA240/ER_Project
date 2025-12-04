Shader "Custom/NY_Dissolve"
{
    Properties
    {
        [Header(Main Textures)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        
        [Header(Dissolve Settings)]
        _DissolveTexture ("Dissolve Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveTexture_gradiant ("Dissolve Gradient", 2D) = "white" {}
        
        [Header(Edge Settings)]
        _EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        _EdgeEmission ("Edge Emission", Range(0, 10)) = 2
        
        [Header(Mask Textures)]
        _MaskTexture01 ("Mask Texture 01", 2D) = "white" {}
        _MaskTexture02 ("Mask Texture 02", 2D) = "white" {}
        
        [Header(Alpha Settings)]
        _MulVal_Alpha ("Alpha Multiply Value", Range(0, 2)) = 1
        [Toggle] _hard ("Hard Edge", Float) = 0
        [Toggle] _LimitCustomY ("Clamp Custom Y", Float) = 0
        
        [Header(Animation Settings)]
        [Toggle] _AnimateDissolve ("Auto Animate Dissolve", Float) = 0
        _DissolveSpeed ("Dissolve Speed", Range(0, 2)) = 0.5
        _ScrollSpeedX ("Scroll Speed X", Range(-2, 2)) = 0
        _ScrollSpeedY ("Scroll Speed Y", Range(-2, 2)) = 0
        
        [Header(Render Settings)]
        [Space(10)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature _HARD_ON
            #pragma shader_feature _LIMITCUSTOMY_ON
            #pragma shader_feature _ANIMATEDISSOLVE_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogCoord : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };
            
            // Textures
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_DissolveTexture);
            SAMPLER(sampler_DissolveTexture);
            
            TEXTURE2D(_DissolveTexture_gradiant);
            SAMPLER(sampler_DissolveTexture_gradiant);
            
            TEXTURE2D(_MaskTexture01);
            SAMPLER(sampler_MaskTexture01);
            
            TEXTURE2D(_MaskTexture02);
            SAMPLER(sampler_MaskTexture02);
            
            // Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DissolveTexture_ST;
                float4 _Color;
                float4 _EdgeColor;
                
                float _DissolveAmount;
                float _EdgeWidth;
                float _EdgeEmission;
                float _MulVal_Alpha;
                float _hard;
                float _LimitCustomY;
                float _DissolveSpeed;
                float _AnimateDissolve;
                float _ScrollSpeedX;
                float _ScrollSpeedY;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogCoord = ComputeFogFactor(positionInputs.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Animate UV with time
                float2 animatedUV = input.uv;
                animatedUV.x += _Time.y * _ScrollSpeedX;
                animatedUV.y += _Time.y * _ScrollSpeedY;
                
                // Sample textures
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half dissolveNoise = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, animatedUV).r;
                half dissolveGradient = SAMPLE_TEXTURE2D(_DissolveTexture_gradiant, sampler_DissolveTexture_gradiant, input.uv).r;
                half mask01 = SAMPLE_TEXTURE2D(_MaskTexture01, sampler_MaskTexture01, input.uv).r;
                half mask02 = SAMPLE_TEXTURE2D(_MaskTexture02, sampler_MaskTexture02, input.uv).r;
                
                // Apply masks
                dissolveNoise *= mask01 * mask02;
                
                // Dissolve calculation
                half dissolveThreshold = _DissolveAmount;
                
                // Auto animate dissolve
                #ifdef _ANIMATEDISSOLVE_ON
                    dissolveThreshold = frac(_Time.y * _DissolveSpeed);
                #endif
                
                #ifdef _LIMITCUSTOMY_ON
                    // Y축 기반 디졸브 (아래에서 위로)
                    float worldY = input.positionWS.y;
                    dissolveThreshold = saturate(worldY * 0.1 + dissolveThreshold);
                #endif
                
                // Hard edge option
                #ifdef _HARD_ON
                    half dissolveFactor = step(dissolveThreshold, dissolveNoise);
                #else
                    half dissolveFactor = smoothstep(dissolveThreshold - 0.05, dissolveThreshold + 0.05, dissolveNoise);
                #endif
                
                // Edge calculation
                half edgeFactor = smoothstep(dissolveThreshold, dissolveThreshold + _EdgeWidth, dissolveNoise) 
                                - smoothstep(dissolveThreshold + _EdgeWidth, dissolveThreshold + _EdgeWidth * 2, dissolveNoise);
                
                // Apply gradient to edge
                edgeFactor *= dissolveGradient;
                
                // Final color
                half4 finalColor = mainTex * _Color * input.color;
                
                // Add edge glow
                half3 edgeGlow = _EdgeColor.rgb * edgeFactor * _EdgeEmission;
                finalColor.rgb += edgeGlow;
                
                // Apply dissolve to alpha
                finalColor.a *= dissolveFactor * _MulVal_Alpha;
                
                // Clip fully dissolved pixels
                clip(finalColor.a - 0.001);
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}