Shader "Custom/BeaconOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _Width ("Outline Width", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Cull Front
            Offset 0, 0

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
                
                float dotProduct = dot(v.normal, ObjSpaceViewDir(v.vertex));
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