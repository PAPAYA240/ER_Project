Shader "Abigail/Portal1"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _TintColor ("Portal Color", Color) = (1, 0.3, 0.7, 1)
        _DistortStrength ("Distortion Strength", Range(0,1)) = 0.2
        _Speed ("Noise Speed", Range(0,5)) = 1.0
        _Intensity ("Glow Intensity", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;  // 파티클 StartColor
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            float4 _TintColor;
            float _DistortStrength;
            float _Speed;
            float _Intensity;

            #include "UnityCG.cginc"

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);   // Built-in 호환
                o.uv = v.uv;
                o.color = v.color; 
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 UV = i.uv;

                // 노이즈 움직임
                float2 noiseUV = UV + float2(_Time.y * _Speed, _Time.y * _Speed * 0.5);
                float noise = tex2D(_NoiseTex, noiseUV).r;

                // UV 왜곡
                UV += (noise - 0.5) * _DistortStrength;

                // 마스크
                float mask = tex2D(_MainTex, UV).r;

                // 최종 색상 = 파티클 색 * 핑크 TintColor * 마스크
                float4 color = i.color * _TintColor * mask;

                // 발광 강화
                color.rgb *= (1 + noise * _Intensity);

                return color;
            }

            ENDHLSL
        }
    }
}
