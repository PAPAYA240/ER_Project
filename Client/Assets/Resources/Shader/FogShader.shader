Shader "Unlit/FogShader"
{
    Properties
    {
        _VisionMask ("Vision Mask (R8)", 2D) = "white" {}   

        _StencilComp ("Stencil Comparison", Float) = 8       
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0          
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15 
    }

    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" } // ���� ������Ʈ���� ���
        LOD 100 

        Blend SrcAlpha OneMinusSrcAlpha 

        
        Pass
        {
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
          

            CGPROGRAM 
            #pragma vertex vert 
            #pragma fragment frag 
            #include "UnityCG.cginc" 

           
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


            sampler2D _VisionMask;     
            float4 _VisionMask_ST;     

            v2f vert (appdata v) 
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _VisionMask); 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target 
            {

                //_CropUV_StartX ("Crop UV Start X", Float) = 0.0513
                //_CropUV_StartY ("Crop UV Start Y", Float) = 0.1016
                //_CropUV_EndX ("Crop UV End X", Float) = 0.9486
                //_CropUV_EndY ("Crop UV End Y", Float) = 0.8984
                if (i.uv.x <  0.0513 || i.uv.x > 0.9486 ||
                    i.uv.y < 0.1016 || i.uv.y > 0.8984)
                    discard;

                fixed4 baseColor = fixed4(0, 0, 0, 0.5);

                fixed visionValue = tex2D(_VisionMask, i.uv).r;

                if (visionValue > 0.1) 
                {
                    discard; 
                }

                return baseColor; 
            }
            ENDCG 
        }
    }
}