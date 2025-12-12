Shader "ERBS_CHR/Toon_CharacterNy"
{
	Properties
	{
		[HideInInspector] _simpleUI ("SimpleUI", Float) = 0
		[HideInInspector] _utsVersion ("Version", Float) = 2.07
		[HideInInspector] _utsTechnique ("Technique", Float) = 0
		[Toggle(_)] _Is_MaskedColor ("Masked Color", Float) = 0
		[Enum(OFF,0,FRONT,1,BACK,2)] _CullMode ("Cull Mode", Float) = 2
		_OverColor ("Overay Color", Vector) = (1,1,1,1)
		_AddValOffset ("Overray Offset", Float) = 0
		_CurrPos ("Current Position", Float) = 0
		_MainTex ("BaseMap", 2D) = "white" {}
		[HideInInspector] _BaseMap ("BaseMap", 2D) = "white" {}
		_BaseColor ("BaseColor", Vector) = (1,1,1,1)
		[HideInInspector] _Color ("Color", Vector) = (1,1,1,1)
		_1st_ShadeMap ("1st_ShadeMap", 2D) = "white" {}
		[Toggle(_)] _Use_BaseAs1st ("Use BaseMap as 1st_ShadeMap", Float) = 0
		_1st_ShadeColor ("1st_ShadeColor", Vector) = (1,1,1,1)
		[HideInInspector] _2nd_ShadeMap ("2nd_ShadeMap", 2D) = "white" {}
		_2nd_ShadeColor ("2nd_ShadeColor", Vector) = (1,1,1,1)
		[Toggle(_)] _Set_SystemShadowsToBase ("Set_SystemShadowsToBase", Float) = 1
		_Tweak_SystemShadowsLevel ("Tweak_SystemShadowsLevel", Range(-0.5, 0.5)) = 0
		_BaseColor_Step ("BaseColor_Step", Range(0, 1)) = 0.5
		_BaseShade_Feather ("Base/Shade_Feather", Range(0.0001, 1)) = 0.0001
		_ShadeColor_Step ("ShadeColor_Step", Range(0, 1)) = 0
		_1st2nd_Shades_Feather ("1st/2nd_Shades_Feather", Range(0.0001, 1)) = 0.0001
		[HideInInspector] _1st_ShadeColor_Step ("1st_ShadeColor_Step", Range(0, 1)) = 0.5
		[HideInInspector] _1st_ShadeColor_Feather ("1st_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
		[HideInInspector] _2nd_ShadeColor_Step ("2nd_ShadeColor_Step", Range(0, 1)) = 0
		[HideInInspector] _2nd_ShadeColor_Feather ("2nd_ShadeColor_Feather", Range(0.0001, 1)) = 0.0001
		_StepOffset ("Step_Offset (ForwardAdd Only)", Range(-0.5, 0.5)) = 0
		[Toggle(_)] _Is_Filter_HiCutPointLightColor ("PointLights HiCut_Filter (ForwardAdd Only)", Float) = 0
		_Set_1st_ShadePosition ("Set_1st_ShadePosition", 2D) = "white" {}
		_HighColor ("HighColor", Vector) = (0,0,0,1)
		_HighColor_Tex ("HighColor_Tex", 2D) = "white" {}
		[Toggle(_)] _Is_LightColor_HighColor ("Is_LightColor_HighColor", Float) = 1
		[Toggle(_)] _Is_NormalMapToHighColor ("Is_NormalMapToHighColor", Float) = 0
		_HighColor_Power ("HighColor_Power", Range(0, 1)) = 0
		[Toggle(_)] _Is_SpecularToHighColor ("Is_SpecularToHighColor", Float) = 0
		[Toggle(_)] _Is_BlendAddToHiColor ("Is_BlendAddToHiColor", Float) = 0
		[Toggle(_)] _Is_UseTweakHighColorOnShadow ("Is_UseTweakHighColorOnShadow", Float) = 0
		_TweakHighColorOnShadow ("TweakHighColorOnShadow", Range(0, 1)) = 0
		_Set_HighColorMask ("Set_HighColorMask", 2D) = "white" {}
		_Tweak_HighColorMaskLevel ("Tweak_HighColorMaskLevel", Range(-1, 1)) = 0
		[Toggle(_)] _RimLight ("RimLight", Float) = 0
		_RimLightColor ("RimLightColor", Vector) = (1,1,1,1)
		[Toggle(_)] _Is_LightColor_RimLight ("Is_LightColor_RimLight", Float) = 1
		[Toggle(_)] _Is_NormalMapToRimLight ("Is_NormalMapToRimLight", Float) = 0
		_RimLight_Power ("RimLight_Power", Range(0, 1)) = 0.1
		_RimLight_InsideMask ("RimLight_InsideMask", Range(0.0001, 1)) = 0.0001
		[Toggle(_)] _RimLight_FeatherOff ("RimLight_FeatherOff", Float) = 0
		[Toggle(_)] _LightDirection_MaskOn ("LightDirection_MaskOn", Float) = 0
		_Tweak_LightDirection_MaskLevel ("Tweak_LightDirection_MaskLevel", Range(0, 0.5)) = 0
		[Toggle(_)] _Add_Antipodean_RimLight ("Add_Antipodean_RimLight", Float) = 0
		_Ap_RimLightColor ("Ap_RimLightColor", Vector) = (1,1,1,1)
		[Toggle(_)] _Is_LightColor_Ap_RimLight ("Is_LightColor_Ap_RimLight", Float) = 1
		_Ap_RimLight_Power ("Ap_RimLight_Power", Range(0, 1)) = 0.1
		[Toggle(_)] _Ap_RimLight_FeatherOff ("Ap_RimLight_FeatherOff", Float) = 0
		_Set_RimLightMask ("Set_RimLightMask", 2D) = "white" {}
		_Tweak_RimLightMaskLevel ("Tweak_RimLightMaskLevel", Range(-1, 1)) = 0
		[Toggle(_)] [HideInInspector] _MatCap ("MatCap", Float) = 0
		[KeywordEnum(SIMPLE,ANIMATION)] [HideInInspector] _EMISSIVE ("EMISSIVE MODE", Float) = 0
		_Emissive_Tex ("Emissive_Tex", 2D) = "white" {}
		[HDR] _Emissive_Color ("Emissive_Color", Vector) = (0,0,0,1)
		[Toggle(_)] _Is_ColorShift ("Activate ColorShift", Float) = 0
		[HDR] _ColorShift ("ColorSift", Vector) = (0,0,0,1)
		_ColorShift_Speed ("ColorShift_Speed", Float) = 0
		[Toggle(_)] _Is_ViewShift ("Activate ViewShift", Float) = 0
		[HDR] _ViewShift ("ViewSift", Vector) = (0,0,0,1)
		[Toggle(_)] _Is_ViewCoord_Scroll ("Is_ViewCoord_Scroll", Float) = 0
		_Outline_Width ("Outline_Width", Float) = 0
		_Farthest_Distance ("Farthest_Distance", Float) = 100
		_Nearest_Distance ("Nearest_Distance", Float) = 0.5
		_Outline_Sampler ("Outline_Sampler", 2D) = "white" {}
		_Outline_Color ("Outline_Color", Vector) = (0.5,0.5,0.5,1)
		[Toggle(_)] _Is_OutlineTex ("Is_OutlineTex", Float) = 0
		_OutlineTex ("OutlineTex", 2D) = "white" {}
		_Offset_Z ("Offset_Camera_Z", Float) = 0
		_BakedNormal ("Baked Normal for Outline", 2D) = "white" {}
		_GI_Intensity ("GI_Intensity", Range(0, 1)) = 0
		_Unlit_Intensity ("Unlit_Intensity", Range(0.001, 4)) = 1
		_Limit_Unlit_Intensity ("Limit Diffuse Reflection", Range(0.001, 4)) = 0.15
		[Toggle(_)] _Is_BLD ("Advanced : Activate Built-in Light Direction", Float) = 0
		_Offset_X_Axis_BLD (" Offset X-Axis (Built-in Light Direction)", Range(-1, 1)) = -0.05
		_Offset_Y_Axis_BLD (" Offset Y-Axis (Built-in Light Direction)", Range(-1, 1)) = 0.09
		[Toggle(_)] _Inverse_Z_Axis_BLD (" Inverse Z-Axis (Built-in Light Direction)", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("ZTest Mode", Float) = 4
		[HDR] _OccludedColor ("X-Ray Color", Color) = (0.5, 0.8, 1, 1)
		_StencilRef ("Stencil Reference", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
		LOD 200

		Pass
		{
			Name "OccludedPass"
			Tags { "LightMode" = "SRPDefaultUnlit" }
			
			ZWrite Off
			ZTest Greater
			Blend One Zero
			Cull Back

			Stencil
			{
			    Ref [_StencilRef]
			    Comp NotEqual
			    Pass Keep
			    ReadMask [_StencilReadMask]
			    Fail Keep        // 추가: 스텐실 테스트 실패 시에도 Keep
			}
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			half4 _OccludedColor;

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				return _OccludedColor;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			
			ZWrite On
			ZTest LEqual
			Cull Back
			
			Stencil
			{
				Ref [_StencilRef]
				Comp [_StencilComp]
				Pass [_StencilOp]
				ReadMask [_StencilReadMask]
				WriteMask [_StencilWriteMask]
			}
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 positionCS : SV_POSITION;
			};

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_1st_ShadeMap);
			SAMPLER(sampler_1st_ShadeMap);
			TEXTURE2D(_Emissive_Tex);
			SAMPLER(sampler_Emissive_Tex);

			float4 _MainTex_ST;
			half4 _BaseColor;
			half4 _1st_ShadeColor;
			half4 _2nd_ShadeColor;
			half4 _Emissive_Color;
			half4 _OverColor;
			half _AddValOffset;
			half _CurrPos;
			half _BaseColor_Step;
			half _BaseShade_Feather;
			half _ShadeColor_Step;
			half _1st2nd_Shades_Feather;
			half _Use_BaseAs1st;
			half _Unlit_Intensity;
			half _GI_Intensity;

			Varyings vert(Attributes input)
			{
				Varyings output;
				
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				
				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
				
				Light mainLight = GetMainLight();
				half3 normalWS = normalize(input.normalWS);
				half NdotL = dot(normalWS, mainLight.direction);
				
				half lightIntensity = NdotL * 0.5 + 0.5;
				half toonStep = step(_BaseColor_Step, lightIntensity);
				
				if (_BaseShade_Feather > 0.0001)
				{
					toonStep = smoothstep(_BaseColor_Step - _BaseShade_Feather * 0.5, 
					                      _BaseColor_Step + _BaseShade_Feather * 0.5, 
					                      lightIntensity);
				}
				
				half4 shadeMap = _Use_BaseAs1st > 0.5 ? baseColor : SAMPLE_TEXTURE2D(_1st_ShadeMap, sampler_1st_ShadeMap, input.uv);
				half4 firstShade = shadeMap * _1st_ShadeColor;
				
				half shade2Step = step(_ShadeColor_Step, lightIntensity);
				if (_1st2nd_Shades_Feather > 0.0001)
				{
					shade2Step = smoothstep(_ShadeColor_Step - _1st2nd_Shades_Feather * 0.5, 
					                        _ShadeColor_Step + _1st2nd_Shades_Feather * 0.5, 
					                        lightIntensity);
				}
				half4 secondShade = shadeMap * _2nd_ShadeColor;
				
				half4 shadedColor = lerp(secondShade, firstShade, shade2Step);
				half4 finalColor = lerp(shadedColor, baseColor, toonStep);
				
				finalColor.rgb *= _Unlit_Intensity;
				
				half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) * _GI_Intensity * 0.3;
				finalColor.rgb += ambient * baseColor.rgb;
				
				half4 emissive = SAMPLE_TEXTURE2D(_Emissive_Tex, sampler_Emissive_Tex, input.uv) * _Emissive_Color;
				finalColor.rgb += emissive.rgb;
				
				half maskValue = saturate((_CurrPos - _AddValOffset) * 10.0);
				finalColor.rgb = lerp(finalColor.rgb, _OverColor.rgb, _OverColor.a * maskValue);
				
				return finalColor;
			}
			ENDHLSL
		}
	}
	
	Fallback "Hidden/Universal Render Pipeline/FallbackError"
}