Shader "ERBS_FX/FX_Particle_BaseSmoothStep_ver2" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_Rotate ("Rotate", Float) = 0
		_MaskTex ("MaskTex", 2D) = "white" {}
		_MainColor_To_Alpha_Amount ("MainColor_To_Alpha_Amount", Range(0, 1)) = 0
		_MainAlpha_To_R ("MainAlpha_To_R", Range(0, 1)) = 1
		_MulVal_Color ("MulVal_Color", Float) = 1
		[Toggle(_WITHPARTICLECUSTOM_ON)] _WithParticleCustom ("WithParticleCustom", Float) = 0
		_ScrollSpeed ("ScrollSpeed", Vector) = (0,0,0,0)
		_MaskScrollSpeed ("MaskScrollSpeed", Vector) = (0,0,0,0)
		[Toggle(_ISNOISE_ON)] _IsNoise ("IsNoise", Float) = 0
		[KeywordEnum(Simple,Complex,NormalComplex)] _IsNoiseComplex ("IsNoiseComplex", Float) = 1
		_NoiseTex ("NoiseTex", 2D) = "black" {}
		[KeywordEnum(NoiseUV_Used,BaseUV_Follow)] _AssignNoiseUV ("AssignNoiseUV", Float) = 0
		_MulVal_Noise ("MulVal_Noise", Float) = 0.05
		_NoiseScale_1 ("NoiseScale_1", Float) = 1
		_NoiseScrollSpeed_1 ("NoiseScrollSpeed_1", Vector) = (-1,-1,0,0)
		_NoiseScale_2 ("NoiseScale_2", Float) = 2
		_NoiseScrollSpeed_2 ("NoiseScrollSpeed_2", Vector) = (1,1,0,0)
		[Toggle(_ISDISSOLVE_ON)] _IsDissolve ("IsDissolve", Float) = 0
		[Toggle(_ISSMOOTHDISSOLVE_ON)] _IsSmoothDissolve ("IsSmoothDissolve", Float) = 0
		_DissolveMask ("DissolveMask", 2D) = "white" {}
		_MulAlpha ("MulAlpha", Float) = 1
		[KeywordEnum(DissolveUV_Used,BaseWarpUV_Follow)] _AssignDissolveUV ("AssignDissolveUV", Float) = 0
		[KeywordEnum(OnlyDissolve,BaseRedXDissolve)] _DissolveAlpha ("DissolveAlpha", Float) = 1
		_DissolveStep ("DissolveStep", Range(-1, 1)) = 1
		_DissolveSmoothRange ("DissolveSmoothRange", Range(0, 1)) = 0.2
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
		[Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DestBlend ("DestBlend", Float) = 10
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
			Texture2D<float4> _MaskTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _MaskTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	//CustomEditor "ASEMaterialInspector"
}