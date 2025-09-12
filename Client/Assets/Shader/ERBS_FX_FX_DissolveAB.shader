Shader "ERBS_FX/FX_DissolveAB" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_MulVal_Col ("Color Multiplier", Float) = 1
		_TintColor ("TintColor", Vector) = (1,1,1,1)
		[Space(30)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Transparent" } // RenderType을 투명으로 변경
		LOD 200

		Pass
		{
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off

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
            float4 _TintColor; // Fragment Shader에서 사용할 변수 선언

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
                // _MainTex와 _TintColor를 곱하여 최종 색상 결정
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _TintColor;
			}

			ENDHLSL
		}
	}
}