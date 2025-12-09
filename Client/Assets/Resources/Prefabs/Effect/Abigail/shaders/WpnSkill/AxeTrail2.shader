Shader "Abigail/AxeTrail2"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _MaskTex ("MaskTex", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 메인 텍스처 UV
                float2 mainUV = TRANSFORM_TEX(i.uv, _MainTex);
                float2 maskUV = TRANSFORM_TEX(i.uv, _MaskTex);
                
                // 메인 텍스처 (흑백)
                fixed4 mainTex = tex2D(_MainTex, mainUV);
                float mainValue = mainTex.r;
                
                // 컷오프
                if (mainValue < _Cutoff) discard;
                
                // 마스크 텍스처
                fixed4 maskTex = tex2D(_MaskTex, maskUV);
                float maskValue = maskTex.r;
                
                // 최종 색상
                fixed4 finalColor = i.color; // Color over Lifetime 그대로
                finalColor.a *= mainValue * maskValue; // 알파에만 마스크 적용
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}