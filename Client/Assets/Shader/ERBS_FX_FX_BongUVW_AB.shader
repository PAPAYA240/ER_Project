Shader "ERBS_FX/FX_BongUVW_AB" {
	Properties {
		_G_tex ("G_tex", 2D) = "white" {}
		_R_tex ("R_tex", 2D) = "white" {}
		_value ("value", Float) = 2
		_A_Speed ("A_Speed", Float) = 0.3
		_B_Speed ("B_Speed", Float) = 0
		_C_Speed ("C_Speed", Float) = 0
		_D_Speed ("D_Speed", Float) = 0.3
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_B_tex ("B_tex", 2D) = "white" {}
		_stvalue ("st value", Range(0, 5)) = 0
		_RotateSpeed ("Rotate Speed", Range(-5, 5)) = 0
		[Space(30)] [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)] _ZTestMode ("ZTest Mode", Float) = 2
	}
	SubShader{
		Tags { 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}
		LOD 200

		Pass
		{
			ZWrite Off
			Blend One One
			ZTest [_ZTestMode]
			Cull Off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _G_tex;
			float4 _G_tex_ST;
			sampler2D _R_tex;
			float4 _R_tex_ST;
			sampler2D _B_tex;
			float4 _B_tex_ST;
			
			float4 _Color;
			float _value;
			float _A_Speed;
			float _B_Speed;
			float _C_Speed;
			float _D_Speed;
			float _stvalue;
			float _RotateSpeed;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			float2 RotateUV(float2 uv, float angle)
			{
				float2 center = float2(0.5, 0.5);
				float cosAngle = cos(angle);
				float sinAngle = sin(angle);
				float2 offset = uv - center;
				float2 rotated;
				rotated.x = offset.x * cosAngle - offset.y * sinAngle;
				rotated.y = offset.x * sinAngle + offset.y * cosAngle;
				return rotated + center;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// G_tex UV 애니메이션
				float2 uv_G = i.uv * _G_tex_ST.xy + _G_tex_ST.zw;
				uv_G.x += _Time.y * _A_Speed;
				uv_G.y += _Time.y * _B_Speed;
				
				// R_tex UV 애니메이션
				float2 uv_R = i.uv * _R_tex_ST.xy + _R_tex_ST.zw;
				uv_R.x += _Time.y * _C_Speed;
				uv_R.y += _Time.y * _D_Speed;
				
				// 회전
				if (abs(_RotateSpeed) > 0.01)
				{
					float rotationAngle = _Time.y * _RotateSpeed;
					uv_R = RotateUV(uv_R, rotationAngle);
				}
				
				// B_tex
				float2 uv_B = i.uv * _B_tex_ST.xy + _B_tex_ST.zw;
				
				// 텍스처 샘플링
				float4 gTexColor = tex2D(_G_tex, uv_G);
				float4 rTexColor = tex2D(_R_tex, uv_R);
				float4 bTexColor = tex2D(_B_tex, uv_B);
				
				// 합성
				float4 combined = gTexColor * rTexColor;
				
				// B_tex 적용
				if (_stvalue > 0.01)
				{
					combined *= (bTexColor * _stvalue + 1.0);
				}
				
				// 최종 색상
				float4 finalColor = combined * _Color * i.color * _value;
				
				return finalColor;
			}
			ENDCG
		}
	}
}