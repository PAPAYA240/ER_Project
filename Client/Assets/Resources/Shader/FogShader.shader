Shader "Unlit/FogShader"
{
    // --------------- (A) Properties: ��Ƽ���� �ν����Ϳ� ����� ������ ---------------
    Properties
    {
        _VisionMask ("Vision Mask (R8)", 2D) = "white" {}   // R8 �þ� ����ũ �ؽ�ó

        // --- ���⿡ Stencil ���� Properties�� �߰��մϴ� ---
        _StencilComp ("Stencil Comparison", Float) = 8       // UnityEngine.Rendering.CompareFunction.Always
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0          // UnityEngine.Rendering.StencilOp.Keep
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15 // R, G, B, A
        // --------------------------------------------------
    }

    // --------------- (B) SubShader: ���̴��� �ٽ� ������ ���� ---------------
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" } // ���� ������Ʈ���� ���
        LOD 100 // Level of Detail (LOD) - �ּ����� ǰ�� ����

        // --- (C) Blend: ����� ��� ��� ���� ---
        Blend SrcAlpha OneMinusSrcAlpha // Source Alpha (���� �ȼ��� ����)�� (1-Source Alpha)�� �̿��� �����

        // --------------- (D) Pass: ���� �������� �� �� �����ϴ� �κ� ---------------
        Pass
        {
             // --- ���⿡ Stencil ����� �߰��մϴ� ---
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            // ------------------------------------

            CGPROGRAM // CG/HLSL �ڵ� ����
            #pragma vertex vert // 'vert' �Լ��� ���� ���̴��� ��� ����
            #pragma fragment frag // 'frag' �Լ��� �����׸�Ʈ ���̴��� ��� ����
            #include "UnityCG.cginc" // ����Ƽ ���� ���̴� ��ƿ��Ƽ �Լ� ���� (���� ����, ��� ��ȯ ��)

            // --- (E) ������ ����ü: ���̴� �Լ� ���� �����͸� ���� ---
            struct appdata // AppData: CPU(���ø����̼�)���� GPU(���̴�)�� �Ѱ��ִ� ���� ������
            {
                float4 vertex : POSITION; // ������ ��ġ (��ü ���� ����)
                float2 uv : TEXCOORD0;    // UV ��ǥ (�ؽ�ó ���ο�)
            };

            struct v2f // VertexToFragment: ���� ���̴����� �����׸�Ʈ ���̴��� �Ѱ��ִ� ������
            {
                float2 uv : TEXCOORD0;    // UV ��ǥ
                float4 vertex : SV_POSITION; // Ŭ�� ����(ȭ��) ���� ���� ��ġ
            };

            // --- (F) Properties�� ����� �������� CGPROGRAM �ȿ��� ����ϱ� ���� ���� ---
            //sampler2D _BaseMap;        // _BaseMap �ؽ�ó ����
            //float4 _BaseMap_ST;        // _BaseMap�� ������/������ ���� ( tilingOffset )
            sampler2D _VisionMask;     // _VisionMask �ؽ�ó ����
            float4 _VisionMask_ST;     // _VisionMask�� ������/������ ����

            // --- (G) Vertex Shader: ����(Vertex)�� ���� ---
            v2f vert (appdata v) // appdata (CPU->GPU)�� �޾� v2f (Vertex->Fragment)�� ��ȯ
            {
                v2f o;
                // ���� ��ġ�� ���� ���� -> �� ���� -> Ŭ�� �������� ��ȯ
                o.vertex = UnityObjectToClipPos(v.vertex);
                // �ؽ�ó UV ��ǥ�� tiling/offset ����
                o.uv = TRANSFORM_TEX(v.uv, _VisionMask); 
                return o;
            }

            // --- (H) Fragment Shader: �ȼ�(Fragment)�� ���� ---
            fixed4 frag (v2f i) : SV_Target // v2f (Vertex->Fragment)�� �޾� fixed4 (RGBA ����) ��ȯ
            {

                //_CropUV_StartX ("Crop UV Start X", Float) = 0.0513
                //_CropUV_StartY ("Crop UV Start Y", Float) = 0.1016
                //_CropUV_EndX ("Crop UV End X", Float) = 0.9486
                //_CropUV_EndY ("Crop UV End Y", Float) = 0.8984
                if (i.uv.x <  0.0513 || i.uv.x > 0.9486 ||
                    i.uv.y < 0.1016 || i.uv.y > 0.8984)
                    discard;

                // �⺻ �̴ϸ� �ؽ�ó���� ���� ���ø� (i.uv ��ǥ ���)
                fixed4 baseColor = fixed4(0, 0, 0, 0.5);
                // R8 �þ� ����ũ �ؽ�ó���� �� ���ø� �� ������ ä��(R)�� ������
                // R8�� R ä�ο��� �ǹ� �ִ� ���� �����ϹǷ� .r�� ����
                fixed visionValue = tex2D(_VisionMask, i.uv).r;

                // �þ� ����ũ ���� �̿��Ͽ� �⺻ ������ ����(�����)�� ����
                //baseColor.a *= visionValue; // visionValue�� 0�̸� ���� ����, 1�̸� ���� ������

                // �Ǵ�, Ư�� �Ӱ�ġ ���ϸ� ������ �����ϰ� ó�� (�̴ϸ��� '�������� ����' �κ�)
                if (visionValue > 0.1) // visionValue�� 0.1���� ������
                {
                    discard; // �ش� �ȼ��� ���������� ���� (��, ���� ����)
                }

                return baseColor; // ���������� ���� RGBA ���� ��ȯ
            }
            ENDCG // CG/HLSL �ڵ� ��
        }
    }
}