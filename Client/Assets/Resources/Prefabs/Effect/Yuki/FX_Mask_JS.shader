Shader "ERBS_FX/FX_Mask_JS"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        _MainTex ("MainTex", 2D) = "white" {}
        _MaskTex ("MaskTex (Alpha Mask)", 2D) = "white" {}
        _MulVal_rgb ("RGB Multiplier", Float) = 1
        _MulVal_alpha ("Alpha Multiplier", Float) = 1

        _FlowSpeedX ("UV Flow Speed X", Float) = 0.1
        _FlowSpeedY ("UV Flow Speed Y", Float) = 0.0

        [Space(20)]
        [Enum(LESS,0,GREATER,1,LEQUAL,2,GEQUAL,3,EQUAL,4,NOTEQUAL,5,ALWAYS,6)]
        _ZTestMode ("ZTest Mode", Float) = 2
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [_ZTestMode]
        Cull [_Cull]

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _MulVal_rgb;
            float _MulVal_alpha;
            float _FlowSpeedX;
            float _FlowSpeedY;

            float4 _MainTex_ST;

            sampler2D _MainTex;
            sampler2D _MaskTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);

                // UV Flow 추가
                uv += float2(_FlowSpeedX, _FlowSpeedY) * _Time.y;

                o.uv = uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col  = tex2D(_MainTex, i.uv);
                float mask = tex2D(_MaskTex, i.uv).r;

                col.rgb *= _MulVal_rgb;
                float alpha = col.a * mask * _MulVal_alpha;

                return float4(col.rgb * _Color.rgb, alpha * _Color.a);
            }

            ENDHLSL
        }
    }
}