Shader "ERBS_FX/FX_ParticleAB" {
	Properties {
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_MainTex ("MainTex", 2D) = "white" {}
		[Space(20)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
	}
	SubShader{
		Tags { 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
			"IgnoreProjector"="True"
		}
		LOD 200
		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
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
				float4 color : COLOR;
			};
			
			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float4 pos : SV_POSITION;
			};
			
			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				output.color = input.color;
				return output;
			}
			
			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;
			
			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};
			
			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				float4 texColor = _MainTex.Sample(sampler_MainTex, input.uv.xy);
				float4 finalColor = texColor * _Color * input.color;
				
				// 텍스처의 알파 사용 + 밝기 기반 알파 추가
				float brightness = (texColor.r + texColor.g + texColor.b) / 3.0;
				finalColor.a = texColor.a * _Color.a * input.color.a * brightness;
				
				return finalColor;
			}
			ENDHLSL
		}
	}
}