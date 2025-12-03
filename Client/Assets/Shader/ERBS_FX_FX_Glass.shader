Shader "ERBS_FX/FX_Glass" {
	Properties {
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
		[Enum(OFF,0,ON,1)] _ZWriteMode ("ZWrite Mode", Float) = 1
		[HDR] _Color ("Color", Vector) = (1,1,1,1)
		_Position ("Position", Vector) = (0,0,0,0)
		_BPositionAdd (" BPositionAdd", Vector) = (0,0,0,0)
		_ReflectTex ("ReflectTex", Cube) = "white" {}
		_Range ("Range", Float) = 0.82
		[Toggle] _VertexColor ("VertexColor", Float) = 0
		[Toggle] _CloseTrueOrFalse ("CloseTrueOrFalse", Float) = 0
		[Toggle] _ISPARTICLESYTEM ("ISPARTICLESYTEM", Float) = 0
		_MaxOffsetDistance ("MaxOffsetDistance", Float) = 10
		_RangeMask ("RangeMask", Vector) = (1,1,1,0)
		_RotationMax ("RotationMax", Float) = 10
		_RoatationMult ("RoatationMult", Float) = 2
		_MainNoise ("MainNoise", 2D) = "white" {}
		_Mask ("Mask", 2D) = "white" {}
		_MainNoiseTex_Speed_U ("MainNoiseTex_Speed_U", Float) = 0
		_MainNoiseTex_Speed_V ("MainNoiseTex_Speed_V", Float) = 0
		_MainNoiseIntensity ("MainNoiseIntensity", Float) = 1
		[HideInInspector] _texcoord2 ("", 2D) = "white" {}
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
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
	//CustomEditor "ASEMaterialInspector"
}