Shader "Custom/Outliner"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,0,0,1)
        _Width ("Outline Width", Range(0.0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Cull Front   // 앞면 제거
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Width;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 norm = normalize(v.normal);
                // 애니메이션 있는 모델도 법선 기준으로 확장
                v.vertex.xyz += norm * _Width;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
