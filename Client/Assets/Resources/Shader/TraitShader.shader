Shader "Unlit/TraitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IsNonColor ("Grayscale Blend (0=Color, 1=Grayscale)", Range(0.0, 1.0)) = 0 
        _Color ("Tint Color", Color) = (1,1,1,1) // <-- 전체 색조 및 알파 조절을 위한 프로퍼티 추가
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" // <-- Opaque 대신 Transparent로 변경
            "Queue"="Transparent"      // <-- 렌더링 큐를 Transparent로 지정
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // <-- 알파 블렌딩 적용! [【3】](https://docs.unity3d.com/kr/530/Manual/SL-Blend.html)[【7】](https://docs.unity3d.com/2018.4/Documentation/Manual/SL-Blend.html)[【8】](https://docs.unity3d.com/2018.4/Documentation/Manual/SL-Blend.html)
            ZWrite Off // <-- 투명 오브젝트는 깊이 버퍼에 쓰지 않아 일반적으로 더 잘 작동합니다.

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _IsNonColor; 
            fixed4 _Color; // <-- CGPROGRAM 안에서 변수 선언

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 원본 텍스처 색상 샘플링
                fixed4 col = tex2D(_MainTex, i.uv) * _Color; // <-- _Color를 곱하여 전체 색조와 투명도 적용

                // 2. 흑백 색상 계산 (광도(Luminosity) 방식)
                fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                fixed4 grayCol = fixed4(gray, gray, gray, col.a); 

                // 3. lerp 함수를 사용하여 원본 색상과 흑백 색상을 블렌딩
                fixed4 finalCol = lerp(col, grayCol, _IsNonColor);

                // 4. 안개 효과 적용
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
}