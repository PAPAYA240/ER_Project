Shader "ERBS_BG/BG_AutodeskInteractive_Optimized" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_AddColor ("Color on lightmap", Vector) = (1,1,1,1)
		_MaskColor ("Alpha Channel Mask Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _MaskTex ("Merged Values Texture", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 1
		_Smoothness ("Smoothness", Range(0, 1)) = 1
		_BumpScale ("Normal Scale", Float) = 1
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_OcclusionStrength ("Occlusion Strength", Float) = 0
		[Toggle(_EMISSION)] _IsEmission ("Is Emission", Float) = 0
		[Toggle(_ANIMATEDEMISSION)] _IsAnimatedEmission ("Animated Emission", Float) = 0
		_AnimatedEmissionTex ("Emissive Animation Gradient Texture", 2D) = "white" {}
		_ScrollSpeedEmit ("Emissive Animation Scroll Speed", Float) = 0.5
		[Enum(UnityEngine.MaterialGlobalIlluminationFlags)] _GIFlag ("Global Illumination", Float) = 0
		_EmissiveValue ("EmissiveValue", Float) = 0
		[HDR] _EmissionColor ("EmissionColor", Vector) = (0,0,0,0)
		_EmissiveMask ("EmissiveMask", 2D) = "white" {}
		[Header(Forward Rendering Options)] [ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1
		[ToggleOff] _GlossyReflections ("Reflections", Float) = 1
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
	Fallback "Diffuse"
}