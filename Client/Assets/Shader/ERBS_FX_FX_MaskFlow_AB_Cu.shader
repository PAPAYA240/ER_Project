Shader "ERBS_FX/FX_MaskFlow_AB_Cu" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_MainTex ("MainTex", 2D) = "white" {}
		_MaskTex ("MaskTex", 2D) = "white" {}
		_MulVal_rgb ("RGB channel multiplier", Float) = 5
		_MulVal_alpha ("Alpha channel multiplier", Float) = 3
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		// ⚠️ 수정: Opaque에서 TransparentCutout으로 변경하여 투명도 처리
		Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
		LOD 200

		Pass
		{
			// ⚠️ Cull Mode와 ZTest Mode는 Properties에서 가져오도록 설정 (기존 코드에 없었음)
			Cull [_Cull] 
			ZTest [_ZTestMode]
			
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
				float4 mainTexColor = _MainTex.Sample(sampler_MainTex, input.uv.xy);

				// 🔥 검정색 배경 제거 로직 (Color Keying)
				// 텍스처의 RGB 합이 0.01보다 작으면 (거의 검정색이면) 픽셀을 버립니다.
				if (mainTexColor.r + mainTexColor.g + mainTexColor.b < 1) // 0.1로 문턱값 상향
				{
				    discard; 
				}

				return mainTexColor * _Color;
			}

			ENDHLSL
		}
	}
}