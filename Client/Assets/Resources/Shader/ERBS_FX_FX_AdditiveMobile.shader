Shader "ERBS_FX/FX_AdditiveMobile" {
	Properties {
		_MainTex ("MainTex", 2D) = "white" {}
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil Ref", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
	}
	SubShader{
		Tags { 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
			"IgnoreProjector"="True"
		}
		LOD 200
		Pass
		{
			ZWrite Off
			Blend One One  // Additive Blending
			Cull Off
			
			Stencil
			{
				Ref [_Stencil]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			ColorMask [_ColorMask]
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;
			
			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float4 pos : SV_POSITION;
			};
			
			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				output.color = input.color;
				return output;
			}
			
			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			
			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				float4 texColor = _MainTex.Sample(sampler_MainTex, input.uv.xy);
				float4 finalColor = texColor * input.color;
				
				// Additive 블렌딩에서는 검은색(0,0,0)이 자동으로 투명하게 처리됨
				return finalColor;
			}
			ENDHLSL
		}
	}
}