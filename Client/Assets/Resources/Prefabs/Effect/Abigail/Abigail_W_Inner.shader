Shader "Abigail/W_Inner"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _NoiseScale("Noise Scale", Range(0.1, 1)) = 0.3
        _Brightness("Brightness", Range(1, 20)) = 10.0
        _MaskBoost("Mask Boost", Range(1, 10)) = 5.0
        _AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
                float2 noiseUV : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            
            sampler2D _NoiseTex;
            sampler2D _MaskTex;
            float4 _Color;
            float _NoiseScale;
            float _Brightness;
            float _MaskBoost;
            float _AlphaCutoff;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.noiseUV = v.uv / _NoiseScale;
                o.vertexColor = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MaskTex, i.uv);
                float maskValue = (mask.r + mask.g + mask.b) / 3.0;
                
                maskValue *= _MaskBoost;
                maskValue = saturate(maskValue);
                
                if (maskValue < _AlphaCutoff) discard;
                
                fixed4 noise = tex2D(_NoiseTex, i.noiseUV);
                float noiseValue = (noise.r + noise.g + noise.b) / 3.0;
                
                fixed4 col = _Color;
                col.rgb *= _Brightness;
                col.rgb *= (0.5 + noiseValue * 0.5);
                
                col.a = maskValue;
                
                col *= i.vertexColor;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}