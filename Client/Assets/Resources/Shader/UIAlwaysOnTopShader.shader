Shader "Unlit/UIAlwaysOnTopShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // UI Texture (예: 이미지 스프라이트)
        _Color ("Color", Color) = (1,1,1,1) // UI Color (예: 텍스트 색상)
    }
    SubShader
    {
        // Opaque 렌더링 타입과 Geometry 렌더링 큐 사용 (불투명 오브젝트 기본 큐)
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        // 이전의 Blend SrcAlpha OneMinusSrcAlpha 라인 삭제!
        // Blend 모드를 사용하지 않으므로 불투명으로 그려집니다.

        // 렌더링 옵션 (UI는 보통 Cull Off, ZWrite Off)
        Cull Off         // 양면 렌더링 허용
        Lighting Off     // 조명 계산 없음 (Unlit 셰이더의 기본 특성)
        ZWrite Off       // Z-Buffer에 깊이 정보 기록하지 않음 (뒤에 있는 오브젝트를 가리지 않게 함)
        ZTest Always     // 깊이 테스트 무조건 통과 (모든 오브젝트 위에 그려지게 함)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fog // 포그 지원이 필요하면 주석 해제

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // UI 요소의 색상 (vertex color)
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR; // vert에서 frag로 전달
            };

            sampler2D _MainTex;
            fixed4 _Color; // Properties에서 정의한 색상

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color; // UI 컴포넌트 색상과 _Color 곱하기
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col.a = 1.0; // 강제로 알파 값을 1 (완전히 불투명)로 설정
                return col;
            }
            ENDCG
        }
    }
}