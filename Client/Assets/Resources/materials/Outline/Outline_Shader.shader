Shader "Custom/Outline_Shader"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _Width ("Outline Width", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _OutlineColor;

            float _Width;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 법선 벡터와 카메라 시선 벡터의 내적을 계산합니다.
                // 법선이 카메라를 정면으로 바라볼 때 1에 가깝고,
                // 옆으로 돌아설 때 0에 가깝습니다.
                float dotProduct = dot(v.normal, ObjSpaceViewDir(v.vertex));
                
                // 내적 값이 0에 가까울수록 (즉, 외곽선에 가까울수록) 더 많이 밀어냅니다.
                float pushDistance = _Width * (1 - dotProduct);
                
                float4 pos = v.vertex;
                pos.xyz += v.normal * pushDistance;
                
                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}