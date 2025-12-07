Shader "Abigail/W_Outer"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _StartTime ("Start Time", Float) = 0.0
        _Duration ("Duration", Float) = 0.57
        _LuminanceCutoff ("Luminance Cutoff", Range(0,1)) = 0.5
        _Feather ("Feather", Range(0,0.2)) = 0.02
        _InvertY ("Invert Y", Float) = 0.0
        _FixedAlpha ("Fixed Alpha", Range(0,1)) = 0.66
        _FlashIntensity ("Flash Intensity", Range(0,5)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _StartTime;
            float _Duration;
            float _LuminanceCutoff;
            float _Feather;
            float _InvertY;
            float _FixedAlpha;
            float _FlashIntensity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; float time : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.time = _Time.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float elapsedTime = i.time - _StartTime;
                float t = max(0, elapsedTime); // 음수 방지

                fixed4 tex = tex2D(_MainTex, i.uv);
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float feather = max(0.0001, _Feather);
                float shapeMask = smoothstep(_LuminanceCutoff - feather, _LuminanceCutoff + feather, lum);

                float initialFill = 0.3;
                float progressTime = 0.25;
                float prog = (_Duration > 0.0001) ? saturate(t / progressTime) : 1.0;
                
                float uvY = i.uv.y;
                if (_InvertY >= 0.5) uvY = 1.0 - uvY;
                
                float fillLevel = initialFill + (1.0 - initialFill) * prog;
                float edge = 0.01;
                float fillMask = smoothstep(fillLevel + edge, fillLevel - edge, uvY);

                if (shapeMask <= 0.001)
                    discard;

                 float finalMask = fillMask;
                
                fixed3 colorFilled = fixed3(1.0, 1.0, 1.0);
                fixed3 colorEmpty = fixed3(0.0, 0.0, 0.0);
                
                float brightness = 1.0 + prog * 0.5;
                fixed3 brightColorFilled = fixed3(brightness, brightness, brightness);
                
                fixed3 baseColor = lerp(colorEmpty, brightColorFilled, finalMask);

                return fixed4(baseColor, _FixedAlpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}