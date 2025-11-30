Shader "ERBS_FX/FX_MeshTrail_Mask2" {
	Properties {
		_Glow ("Glow", Float) = 0
		_Color ("Color", Vector) = (1,1,1,1)
		_Noise ("Noise Strength", Float) = 0
		_Noise01 ("Noise01", 2D) = "white" {}
		_Noise01UV_TilingSpeed ("Noise01 UV_Tiling & Speed", Vector) = (1,1,0,0)
		_Noise02 ("Noise02", 2D) = "white" {}
		_Noise02UV_TilingSpeed ("Noise02 UV_Tiling & Speed", Vector) = (1,1,0,0)
		_MainTex ("MainTex", 2D) = "white" {}
		_MainTexUV_TilingSpeed ("MainTex UV_Tiling & Speed", Vector) = (1,1,0,0)
		_MaskTex ("MaskTex", 2D) = "white" {}
		_MaskTex02 ("MaskTex02", 2D) = "white" {}
		_MaskUV_Tiling ("Mask UV Tiling", Vector) = (1,1,0,0)
		[Toggle] _IsPolar ("Does MaskTex02 using Polar Coordinates?", Float) = 0
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