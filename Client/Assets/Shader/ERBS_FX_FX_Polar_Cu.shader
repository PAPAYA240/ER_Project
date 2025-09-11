Shader "ERBS_FX/FX_Polar_Cu" {
	Properties {
		_Color ("_Color", Vector) = (0,0,0,0)
		_MulVal_BrightBlack ("_MulVal_BrightBlack", Float) = 2.5
		_MulVal_BrightColor ("_MulVal_BrightColor", Float) = 2
		_MulVal_MainTexBlack ("_MulVal_MainTexBlack", Float) = 0
		_MainTex ("_MainTex", 2D) = "white" {}
		[Toggle()] _MainTexClamp ("_MainTexClamp", Float) = 0
		_DissolveTex ("_DissolveTex", 2D) = "white" {}
		_NoiseTex ("_NoiseTex", 2D) = "white" {}
		_NoiseST ("_NoiseST", Vector) = (1,1,0,0)
		_NoiseSpeed ("_NoiseSpeed", Vector) = (-0.1,0,0,0)
		_PowVal_NoiseRange ("_PowVal_NoiseRange", Float) = 2
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 6
		[Toggle] _ZWrite ("ZWrite", Float) = 0
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
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
}