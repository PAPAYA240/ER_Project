Shader "ERBS_BG/BG_LinearGauge" {
	Properties {
		_Color ("Color", Vector) = (0,0.4568243,1,1)
		_Color2 ("Color2", Vector) = (1,0,0,1)
		_AddColor ("Color3", Vector) = (1,1,1,1)
		_Gap ("Gap", Float) = 0.7
		_AddValBlur ("Boundary Blur", Float) = 0.01
		_Percentage ("Percentage", Range(0, 1)) = 1
		_Emission ("Emission", Float) = 1
		_MulValEmission ("Boundary Emission Multiplier", Float) = 1
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
	Fallback "Diffuse"
}