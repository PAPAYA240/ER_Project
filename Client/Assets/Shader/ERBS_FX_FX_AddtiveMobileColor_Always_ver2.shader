Shader "ERBS_FX/FX_AddtiveMobileColor_Always_ver2" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_MainTex ("MainTex", 2D) = "white" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200

		Pass
		{
			// 알파 블렌딩을 위한 명령어 추가
			Blend SrcAlpha OneMinusSrcAlpha // 일반적인 알파 블렌딩 (원본에 따라 투명도 조절)
			// 또는
			// Blend One One // 애디티브 블렌딩 (색상을 더하는 방식, 빛 이펙트에 주로 사용)

			// Z-Writing 끄기 (선택 사항 - 투명 오브젝트 렌더링 시 Z-Fighting 방지)
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
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				float4 result = _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
				if(result.x < 0.1)
					discard;

				return result;
			}

			ENDHLSL
		}
	}
}