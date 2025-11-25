Shader "ERBS_BG/BG_VertexBlend" {
	Properties {
		_Color ("BASE Tint (RGB), Tint Fade (A)", Vector) = (0.5,0.5,0.5,0)
		_MainTex ("BASE Albedo (RGB), Tint Mask (A)", 2D) = "white" {}
		[Normal] _BumpMap ("BASE Normal (RGB)", 2D) = "bump" {}
		_Roughness ("BASE Roughness", Range(0, 1)) = 1
		_ColorB ("Layer_B Tint (RGB), Tint Fade (A)", Vector) = (0.5,0.5,0.5,0)
		[NoScaleOffset] _layer1Tex ("LAYER_B Albedo (RGB), Tint Mask (A)", 2D) = "white" {}
		[NoScaleOffset] [Normal] _layer1Norm ("LAYER_B Normal (RGB)", 2D) = "bump" {}
		_layer1Tiling ("LAYER_B Tiling", Float) = 1
		_layer1Rough ("LAYER_B Roughness", Range(0, 1)) = 1
		[NoScaleOffset] _BlendMask ("BLEND_Mask (R)", 2D) = "white" {}
		_BlendTile ("BLEND_Tiling", Float) = 1
		_Choke ("BLEND_Choke", Range(0, 60)) = 15
		_Crisp ("BLEND_Crispyness", Range(1, 20)) = 5
		[NoScaleOffset] _DetailAlbedo ("DETAIL_Albedo (R)", 2D) = "grey" {}
		[NoScaleOffset] [Normal] _DetailNormal ("DETAIL_Normal (RGB)", 2D) = "bump" {}
		_DetailNormalStrength ("DETAIL_Normal Strength", Range(0, 1)) = 0.4
		_DetailTiling ("DETAIL_Tiling", Float) = 2
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
	Fallback "Standard"
}