Shader "Custom/Outliner"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,0,0,1)
        _Width ("Outline Width", Range(0.0, 0.1)) = 0.03
        
        [Header(Stencil)]
        _StencilRef ("Stencil Reference", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Front
            ZWrite On
            ZTest LEqual
            
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Keep
            }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            half4 _Color;
            float _Width;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalOS = normalize(input.normalOS);
                float3 expandedPos = input.positionOS.xyz + normalOS * _Width;
                output.positionCS = TransformObjectToHClip(expandedPos);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}