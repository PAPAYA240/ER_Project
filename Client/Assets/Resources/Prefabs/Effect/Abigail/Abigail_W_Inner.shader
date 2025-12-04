Shader "Abigail/W_Inner"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 0.45, 0.85, 1)
        _NoiseScale("Noise Scale", Range(0.01, 10)) = 0.3
        _Brightness("Brightness", Range(0.1, 20)) = 1.0
        _MaskBoost("Mask Boost", Range(0.1, 10)) = 1.0
        _AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.01
        _EffectTime("Effect Time (seconds)", Float) = 0.0
        _FillSpeed("Fill Speed", Float) = 1.0
        _FillEdge("Fill Edge", Range(0.0, 0.5)) = 0.04
        _MinY("Min Y", Float) = 0.0
        _MaxY("Max Y", Float) = 1.0
        _Lifetime("Lifetime (sec)", Float) = 0.6
        _Loop("Loop (0:once,1:repeat)", Float) = 0.0
        _UseMaskAlpha("Use Mask Alpha (0/1)", Float) = 0.0
        _FlipY("Flip Y (0/1)", Float) = 0.0
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
                float normalizedY : TEXCOORD2;
            };

            sampler2D _NoiseTex;
            sampler2D _MaskTex;
            float4 _Color;
            float _NoiseScale;
            float _Brightness;
            float _MaskBoost;
            float _AlphaCutoff;
            float _EffectTime;
            float _FillSpeed;
            float _FillEdge;
            float _MinY;
            float _MaxY;
            float _Lifetime;
            float _Loop;
            float _UseMaskAlpha;
            float _FlipY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.noiseUV = v.uv / max(0.0001, _NoiseScale);
                o.vertexColor = v.color;
                float normalizedY = 0.0;
                if (abs(_MaxY - _MinY) > 1e-6)
                    normalizedY = (v.vertex.y - _MinY) / (_MaxY - _MinY);
                else
                    normalizedY = v.uv.y;
                if (_FlipY >= 0.5) normalizedY = 1.0 - normalizedY;
                o.normalizedY = saturate(normalizedY);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MaskTex, i.uv);
                float maskValue = (_UseMaskAlpha >= 0.5) ? mask.a : ((mask.r + mask.g + mask.b) * 0.3333333);
                maskValue *= _MaskBoost;
                maskValue = saturate(maskValue);

                fixed4 n = tex2D(_NoiseTex, i.noiseUV);
                float n1 = n.r;
                float n2 = tex2D(_NoiseTex, i.noiseUV * 2.05).r * 0.35;
                float n3 = tex2D(_NoiseTex, i.noiseUV * 4.3).r * 0.15;
                float highFreq = saturate(n1 + n2 + n3);

                float lifetime = max(0.0001, _Lifetime);
                float timeRatio = _EffectTime / lifetime;
                float fillProgress;
                if (_Loop >= 0.5)
                    fillProgress = frac(timeRatio) * _FillSpeed;
                else
                    fillProgress = saturate(timeRatio * _FillSpeed);
                fillProgress = saturate(fillProgress);

                float fillMask = 1.0 - smoothstep(fillProgress - _FillEdge, fillProgress + _FillEdge, i.normalizedY);

                fixed4 col = _Color;
                col.rgb *= (0.6 + n1 * 0.4);
                col.rgb *= _Brightness;

                col.a = maskValue * fillMask;
                col *= i.vertexColor;
                if (col.a < _AlphaCutoff) discard;
                return col;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}