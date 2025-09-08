Shader "Unlit/FogShader"
{
    // --------------- (A) Properties: 머티리얼 인스펙터에 노출될 변수들 ---------------
    Properties
    {
        //_BaseMap ("Base Minimap Texture", 2D) = "white" {} // 기본 미니맵 이미지 텍스처
        _VisionMask ("Vision Mask (R8)", 2D) = "white" {}   // R8 시야 마스크 텍스처
    }

    // --------------- (B) SubShader: 셰이더의 핵심 렌더링 로직 ---------------
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // 투명 오브젝트임을 명시
        LOD 100 // Level of Detail (LOD) - 최소한의 품질 수준

        // --- (C) Blend: 투명도 계산 방식 설정 ---
        Blend SrcAlpha OneMinusSrcAlpha // Source Alpha (현재 픽셀의 알파)와 (1-Source Alpha)를 이용한 블렌딩

        // --------------- (D) Pass: 실제 렌더링을 한 번 수행하는 부분 ---------------
        Pass
        {
            CGPROGRAM // CG/HLSL 코드 시작
            #pragma vertex vert // 'vert' 함수를 정점 셰이더로 사용 선언
            #pragma fragment frag // 'frag' 함수를 프래그먼트 셰이더로 사용 선언
            #include "UnityCG.cginc" // 유니티 내장 셰이더 유틸리티 함수 포함 (변수 선언, 행렬 변환 등)

            // --- (E) 데이터 구조체: 셰이더 함수 간에 데이터를 전달 ---
            struct appdata // AppData: CPU(어플리케이션)에서 GPU(셰이더)로 넘겨주는 정점 데이터
            {
                float4 vertex : POSITION; // 정점의 위치 (객체 로컬 공간)
                float2 uv : TEXCOORD0;    // UV 좌표 (텍스처 매핑용)
            };

            struct v2f // VertexToFragment: 정점 셰이더에서 프래그먼트 셰이더로 넘겨주는 데이터
            {
                float2 uv : TEXCOORD0;    // UV 좌표
                float4 vertex : SV_POSITION; // 클립 공간(화면) 상의 정점 위치
            };

            // --- (F) Properties에 선언된 변수들을 CGPROGRAM 안에서 사용하기 위한 선언 ---
            //sampler2D _BaseMap;        // _BaseMap 텍스처 변수
            //float4 _BaseMap_ST;        // _BaseMap의 스케일/오프셋 정보 ( tilingOffset )
            sampler2D _VisionMask;     // _VisionMask 텍스처 변수
            float4 _VisionMask_ST;     // _VisionMask의 스케일/오프셋 정보

            // --- (G) Vertex Shader: 정점(Vertex)별 연산 ---
            v2f vert (appdata v) // appdata (CPU->GPU)를 받아 v2f (Vertex->Fragment)를 반환
            {
                v2f o;
                // 정점 위치를 월드 공간 -> 뷰 공간 -> 클립 공간으로 변환
                o.vertex = UnityObjectToClipPos(v.vertex);
                // 텍스처 UV 좌표에 tiling/offset 적용
                o.uv = TRANSFORM_TEX(v.uv, _VisionMask); 
                return o;
            }

            // --- (H) Fragment Shader: 픽셀(Fragment)별 연산 ---
            fixed4 frag (v2f i) : SV_Target // v2f (Vertex->Fragment)를 받아 fixed4 (RGBA 색상) 반환
            {

                //_CropUV_StartX ("Crop UV Start X", Float) = 0.0513
                //_CropUV_StartY ("Crop UV Start Y", Float) = 0.1016
                //_CropUV_EndX ("Crop UV End X", Float) = 0.9486
                //_CropUV_EndY ("Crop UV End Y", Float) = 0.8984
                if (i.uv.x <  0.0513 || i.uv.x > 0.9486 ||
                    i.uv.y < 0.1016 || i.uv.y > 0.8984)
                    discard;

                // 기본 미니맵 텍스처에서 색상 샘플링 (i.uv 좌표 사용)
                fixed4 baseColor = fixed4(0, 0, 0, 0.5);
                // R8 시야 마스크 텍스처에서 값 샘플링 후 빨간색 채널(R)만 가져옴
                // R8은 R 채널에만 의미 있는 값을 저장하므로 .r로 접근
                fixed visionValue = tex2D(_VisionMask, i.uv).r;

                // 시야 마스크 값을 이용하여 기본 색상의 알파(투명도)를 조절
                //baseColor.a *= visionValue; // visionValue가 0이면 완전 투명, 1이면 완전 불투명

                // 또는, 특정 임계치 이하면 완전히 투명하게 처리 (미니맵의 '밝혀지지 않은' 부분)
                if (visionValue > 0.1) // visionValue가 0.1보다 작으면
                {
                    discard; // 해당 픽셀을 렌더링에서 제외 (즉, 완전 투명)
                }

                return baseColor; // 최종적으로 계산된 RGBA 색상 반환
            }
            ENDCG // CG/HLSL 코드 끝
        }
    }
}