Shader "ERBS_FX/FX_Slash" {
	Properties {
		_Color ("Color", Vector) = (0.07843138,0.3921569,0.7843137,1)
		_EmissStrength ("EmissStrength", Float) = 1
		_MainTex ("MainTex", 2D) = "white" {}
		_FlowMap ("FlowMap", 2D) = "white" {}
		_FlowStrength ("FlowStrength", Float) = 0.2
		_FlowSpeed ("FlowSpeed", Float) = 0.5
		_BlackAmount ("BlackAmount", Float) = 1
		_RefractionStrength ("RefractionStrength", Float) = 1
		_SharpEdge ("SharpEdge", Float) = 0
		[MaterialToggle] _KeepEdge_NonFlowmap ("KeepEdge_NonFlowmap", Float) = 0
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
		[Toggle] _ZWrite ("ZWrite Mode", Float) = 0
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
	Fallback "Diffuse"
}