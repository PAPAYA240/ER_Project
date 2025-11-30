Shader "ERBS_FX/MaskRotater_Time"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _Mask ("Mask Texture (Same as MainTex)", 2D) = "white" {}
        _Speed ("Rotation Speed", Float) = 2
        _DistortStrength ("Distortion Strength", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _Mask;
            float4 _Mask_ST;

            float _Speed;
            float _DistortStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvMask : TEXCOORD1;
            };

            // ==========================
            //  Pivot 기준 회전 함수
            // ==========================
            float2 RotateUV(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                return float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // 일반 Main 텍스처 UV
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);

                // 마스크 회전 UV (pivot 0.5)
                float2 uv = TRANSFORM_TEX(v.uv, _Mask);
                float2 centered = uv - 0.5;

                float angle = _Time.y * _Speed;
                centered = RotateUV(centered, angle);

                o.uvMask = centered + 0.5;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mainCol = tex2D(_MainTex, i.uvMain);
                float4 maskCol = tex2D(_Mask, i.uvMask);
            
                float2 distortion = (maskCol.rg - 0.5) * _DistortStrength;
            
                float4 final = tex2D(_MainTex, i.uvMain + distortion);
            
                final.a *= mainCol.a;
            
                final.a *= maskCol.a;
            
                return final * _Color;
            }
            ENDHLSL
        }
    }
}
