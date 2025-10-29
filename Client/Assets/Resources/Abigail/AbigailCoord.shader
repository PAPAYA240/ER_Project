Shader "Custom/AbigailCoord"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 위쪽 컬러 샘플링
                float2 uvColor = float2(i.uv.x, i.uv.y * 0.5 + 0.5);
                fixed4 col = tex2D(_MainTex, uvColor);

                // 아래쪽 알파 샘플링
                float2 uvAlpha = float2(i.uv.x, i.uv.y * 0.5);
                fixed4 mask = tex2D(_MainTex, uvAlpha);

                col.a = mask.r; // R 채널을 알파로 사용
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}


