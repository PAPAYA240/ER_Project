Shader "ERBS_FX/FX_Dissolve_05_Cu" {
	Properties {
		_Color ("_Color", Vector) = (0,0,0,0)
		_MulVal_BrightColor ("_MulVal_BrightColor", Float) = 1
		_MulVal_BrightBlack ("_MulVal_BrightBlack", Float) = 1.2
		_MainTex ("_MainTex", 2D) = "white" {}
		_NoiseTex ("_NoiseTex", 2D) = "white" {}
		[Toggle()] _Noise_Polar ("_Noise_Polar", Float) = 0
		_Noise_Dist ("_Noise_Dist", Float) = -0.29
		_Noise_Speed ("_Noise_Speed", Vector) = (-0.1,0,-0.1,0)
		_Dissolve_Rotation ("_Dissolve_Rotation", Float) = 1
		[Toggle()] _Dissolve_EdgeOrCenter ("_Dissolve_EdgeOrCenter", Float) = 0
		[Toggle()] _Dissolve_EdgeOrCenter_Re ("_Dissolve_EdgeOrCenter_Re", Float) = 0
		[Toggle()] _Dissolve_Circle ("_Dissolve_Circle", Float) = 0
		[Toggle()] _Dissolve_Circle_Re ("_Dissolve_Circle_Re", Float) = 0
		_Dissolve_Soft ("_Dissolve_Soft", Range(0.1, 3)) = 1
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