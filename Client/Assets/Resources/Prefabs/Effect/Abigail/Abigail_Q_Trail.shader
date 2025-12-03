Shader "Abigail/Q_Trail"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (0.75, 0.40, 1.0, 1) // ¹à°í ¼±¸íÇÑ ÀÚÁÖºû
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.025
        _AlphaCutoff("Alpha Cutoff", Range(0,1)) = 0.07
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _TintColor;
            float _OutlineWidth;
            float _AlphaCutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _TintColor;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainCol = tex2D(_MainTex, i.uv) * i.color;
                float mask = tex2D(_MaskTex, i.uv).r;

                float lowerBound = 1.0 - _OutlineWidth * 1.2;
                float upperBound = 1.0;
                float outlineFactor = smoothstep(lowerBound, upperBound, mask);

                float alpha = lerp(mask, 1.0, outlineFactor);

                clip(alpha - _AlphaCutoff);

                fixed4 col = mainCol;
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}