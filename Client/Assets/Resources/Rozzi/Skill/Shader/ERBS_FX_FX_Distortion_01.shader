Shader "ERBS_FX/FX_Distortion_01" {
	Properties {
		_Map ("Map", 2D) = "white" {}
		_Rotatespeed ("Rotate speed", Range(-5, 5)) = -0.1121815
		_normal ("normal ", Range(0, 3)) = 1.236291
		_node_3266 ("node_3266", 2D) = "white" {}
		_RefractionValue ("Refraction Value", Range(-0.5, 0.5)) = 0.1256844
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
	Fallback "Diffuse"
}