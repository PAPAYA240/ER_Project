Shader "ERBS_FX/Smoothstep_Dissolve_V2_JS"
{
	Properties
	{
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_MaskTex ("Mask Texture", 2D) = "white" {}
		_Speed ("Rotation Speed", Float) = 1
		
		_Dissolve ("Dissolve (0=Full,1=Gone)", Range(0,1)) = 0
		_Smooth ("Smoothstep Width", Range(0,0.2)) = 0.05
		} 

		SubShader
		{ 
			Tags {
				"RenderType"="Transparent"
				"Queue"="Transparent"
			}
				
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off 
			ZWrite Off 

			Pass 
			{ 
				CGPROGRAM 
				#pragma vertex vert 
				#pragma fragment frag
				
				#include "UnityCG.cginc"
				
				sampler2D _MainTex; 
				sampler2D _MaskTex; 

				float4 _MainTex_ST; 
				float4 _MaskTex_ST; 
				float4 _Color; 
				float _Speed; 
				float _Dissolve; 
				float _Smooth; 
				
				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				}; 
				
				struct v2f
				{ 
					float4 pos : SV_POSITION; 
					float2 uv : TEXCOORD0;
				};

				v2f vert(appdata v)
				{ 
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}
				
				fixed4 frag(v2f i) : SV_Target
				{
					// ----------------------- 
					// 1. 회전 적용 
					// ----------------------- 
					float t = _Time.y * _Speed;

					float s = sin(t);
					float c = cos(t); 

					// 중심 기준 회전 
					float2 uv = i.uv - 0.5;
					float2 ruv = float2( uv.x * c - uv.y * s, uv.x * s + uv.y * c ) + 0.5;
					// ----------------------- 
					// 2. 텍스처 샘플링 
					// ----------------------- 
					float4 mainCol = tex2D(_MainTex, ruv); float mask = tex2D(_MaskTex, ruv).r;
					// -----------------------
					// 3. MaskTex 기반 Smoothstep (엣지 부드러움)
					// -----------------------
					float dissolveEdge = smoothstep(_Dissolve - _Smooth, _Dissolve + _Smooth, mask);
					// ----------------------- 
					// 4. Dissolve 기반 알파 컷 
					// ----------------------- 
					mainCol.a *= dissolveEdge;
					// ----------------------- 
					// 5. 최종 색 
					// -----------------------
					return mainCol * _Color; 
				} 
				ENDCG 
			} 
		} 
	}