Shader "ERBS_FX/FX_Dissolve_05_Cu" {
	Properties {
		_Color ("_Color", Vector) = (0,0,0,0)
		_MulVal_BrightColor ("_MulVal_BrightColor", Float) = 1
		_MulVal_BrightBlack ("_MulVal_BrightBlack", Float) = 1.2
		_MainTex ("_MainTex", 2D) = "white" {}
		_NoiseTex ("_NoiseTex", 2D) = "white" {}
		[Toggle()] _Noise_Polar ("_Noise_Polar", Float) = 0
		_Noise_Dist ("_Noise_Dist", Float) = -0.29
		_Noise_Speed ("_Noise_Speed", Vector) = (-0.1,0,-0.1,0)
		_Dissolve_Rotation ("_Dissolve_Rotation", Float) = 1
		[Toggle()] _Dissolve_EdgeOrCenter ("_Dissolve_EdgeOrCenter", Float) = 0
		[Toggle()] _Dissolve_EdgeOrCenter_Re ("_Dissolve_EdgeOrCenter_Re", Float) = 0
		[Toggle()] _Dissolve_Circle ("_Dissolve_Circle", Float) = 0
		[Toggle()] _Dissolve_Circle_Re ("_Dissolve_Circle_Re", Float) = 0
		_Dissolve_Soft ("_Dissolve_Soft", Range(0.1, 3)) = 1
		
		// 새로 추가된 Radial Gradient 속성
		[Space(20)]
		[Header(Radial Gradient Transparency)]
		[Toggle()] _UseRadialGradient ("Use Radial Gradient", Float) = 1
		_RadialCenter ("Radial Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
		_RadialCenterAlpha ("Center Alpha", Range(0, 1)) = 1
		_RadialEdgeAlpha ("Edge Alpha", Range(0, 1)) = 0
		_RadialGradientPower ("Gradient Power", Range(0.1, 5)) = 1
		_RadialGradientRadius ("Gradient Radius", Range(0.1, 2)) = 1
		
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 4
		[Toggle] _ZWrite ("ZWrite", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
	}
	
	SubShader{
		 Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    
		Blend SrcAlpha One
		ZWrite Off
		Cull Off  
		ZTest LEqual

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _USERADIALGRADIENT_ON
			
			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;
			
			// 새로 추가된 변수
			float _UseRadialGradient;
			float4 _RadialCenter;
			float _RadialCenterAlpha;
			float _RadialEdgeAlpha;
			float _RadialGradientPower;
			float _RadialGradientRadius;
			
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
				float4 texColor = _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
				
				// Radial Gradient 계산
				if (_UseRadialGradient > 0.5)
				{
					// UV 좌표에서 중심점까지의 거리 계산
					float2 centerOffset = input.uv - _RadialCenter.xy;
					float distance = length(centerOffset) / _RadialGradientRadius;
					
					// 거리를 0~1 범위로 클램프
					distance = saturate(distance);
					
					// Gradient Power 적용 (부드러움 조절)
					distance = pow(distance, _RadialGradientPower);
					
					// 중심 알파와 가장자리 알파 사이를 보간
					float gradientAlpha = lerp(_RadialCenterAlpha, _RadialEdgeAlpha, distance);
					
					// 최종 알파값 적용
					texColor.a *= gradientAlpha;
				}
				
				return texColor;
			}
			ENDHLSL
		}
	}
}