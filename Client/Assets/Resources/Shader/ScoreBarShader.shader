Shader "Unlit/ScoreBarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Fill ("Fill Amount", Range(0,1)) = 1
        _CutAmount ("Cut Amount", Range(-1,1)) = 0.5 // 기울기 정도 (tan θ)
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Fill;
            float _CutAmount;
            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float LeftCut = 0; 
                float RightCut = 0;
                if(_CutAmount >= 0)
                {
                    LeftCut = i.uv.y * _CutAmount; 
                    RightCut = i.uv.y * _CutAmount + (1 - _CutAmount) * _Fill;
                    
                    if (i.uv.x < LeftCut || i.uv.x > RightCut)
                        discard;
                }
                else
                {
                    LeftCut = 1 + i.uv.y * _CutAmount  - (1 + _CutAmount) * _Fill;
                    RightCut =  1 + i.uv.y * _CutAmount; 
                    if (i.uv.x < LeftCut || i.uv.x > RightCut)
                        discard;
                }
                


                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                //float4 selectedColor = (0,0,0,0);


                //fixed4 col = selectedColor;
                return col;
            }
            ENDHLSL
        }
    }
}
