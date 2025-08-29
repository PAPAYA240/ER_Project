Shader "ERBS_FX/FX_Dissolve_ABCu" {
	Properties {
		_DissolveTexture ("DissolveTexture", 2D) = "white" {}
		[Toggle] _hard ("hard", Float) = 0
		[Toggle] _LimitCustomY ("Clamp Custom Y", Float) = 0
		_MaskTexture01 ("MaskTexture01", 2D) = "white" {}
		_MaskTexture02 ("MaskTexture02", 2D) = "white" {}
		_DissolveTexture_gradiant ("DissolveTexture_gradiant", 2D) = "white" {}
		_MulVal_Alpha ("Alpha Multiply Value", Float) = 0.25
		[Space(30)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
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

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
}