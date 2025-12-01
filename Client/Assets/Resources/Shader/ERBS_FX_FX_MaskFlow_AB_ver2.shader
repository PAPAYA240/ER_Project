Shader "ERBS_FX/FX_MaskFlow_AB_ver2" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_MainTex ("MainTex", 2D) = "white" {}
		_MaskTex ("MaskTex", 2D) = "white" {}
		_MulVal_rgb ("RGB channel multiplier", Float) = 5
		_MulVal_alpha ("Alpha channel multiplier", Float) = 3
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
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
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			Texture2D<float4> _MaskTex;
			SamplerState sampler_MainTex;
			SamplerState sampler_MaskTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				float4 result = _MainTex.Sample(sampler_MainTex, input.uv.xy) * _MaskTex.Sample(sampler_MaskTex, input.uv.xy) * _Color;
				if(result.x < 0.4)
					discard;

				return result;
				//return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _MaskTex.Sample(sampler_MaskTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
}