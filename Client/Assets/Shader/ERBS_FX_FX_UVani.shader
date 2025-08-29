Shader "ERBS_FX/FX_UVani" {
	Properties {
		_Tex ("Tex", 2D) = "white" {}
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_Mask ("Mask", 2D) = "white" {}
		_U ("U", Range(-5, 5)) = 0
		_V ("V", Range(-5, 5)) = 0
		_U_copy ("U_copy", Range(-5, 5)) = 0
		_V_copy ("V_copy", Range(-5, 5)) = 0
		_power ("power", Range(0, 5)) = 0
		_MulVal_Col ("Color Multiplier", Float) = 1
		[Space(20)] [Header(Add_ SrcAlpha_One)] [Header(Add_ One_One)] [Header(AlphaBlend_ SrcAlpha_OneMinusSrcAlpha)] [Header(Multiply_ DstColor_Zero)] [Header(BlendAdd_ One_OneMinusSrcAlpha)] [Space(20)] [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend mode", Float) = 1
		[Space(30)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
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

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
}