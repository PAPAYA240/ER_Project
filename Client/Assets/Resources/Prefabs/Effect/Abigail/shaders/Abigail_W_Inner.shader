Shader "Abigail/W_Inner"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,0.45,0.85,1)
        _NoiseScale("Noise Scale", Range(0.05,8)) = 1.0
        _NoiseIntensity("Noise Intensity", Range(0,1)) = 0.18
        _Layer1Amp("Layer1 Amp", Range(0,1)) = 0.04
        _Layer2Amp("Layer2 Amp", Range(0,1)) = 0.06
        _Layer3Amp("Layer3 Amp", Range(0,1)) = 0.10
        _Layer1Speed("Layer1 Speed", Range(0,5)) = 0.7
        _Layer2Speed("Layer2 Speed", Range(0,5)) = 1.3
        _Layer3Speed("Layer3 Speed", Range(0,5)) = 2.1
        _Brightness("Brightness", Range(0.1,4)) = 1.0
        _MaskBoost("Mask Boost", Range(0.1,5)) = 1.0
        _AlphaCutoff("Alpha Cutoff", Range(0,1)) = 0.01
        _UseMaskAlpha("Use Mask Alpha (0/1)", Float) = 0.0
        _EffectTime("Effect Time", Float) = 0.0
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 baseNoiseUV : TEXCOORD1; float4 vcol : COLOR; float time : TEXCOORD2; };

            sampler2D _NoiseTex;
            sampler2D _MaskTex;
            float4 _Color;
            float _NoiseScale;
            float _NoiseIntensity;
            float _Layer1Amp;
            float _Layer2Amp;
            float _Layer3Amp;
            float _Layer1Speed;
            float _Layer2Speed;
            float _Layer3Speed;
            float _Brightness;
            float _MaskBoost;
            float _AlphaCutoff;
            float _UseMaskAlpha;
            float _EffectTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vcol = v.color;
                o.baseNoiseUV = v.uv * max(0.0001, _NoiseScale);
                o.time = (_EffectTime > 0.00001) ? _EffectTime : _Time.y;
                return o;
            }

            float sampleNoise(float2 uv) { return tex2D(_NoiseTex, uv).r; }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = i.time;

                float2 l1_off = float2(sin(t * _Layer1Speed * 0.6), cos(t * _Layer1Speed * 0.53)) * _Layer1Amp;
                float2 l2_off = float2(cos(t * _Layer2Speed * 0.45), sin(t * _Layer2Speed * 0.77)) * _Layer2Amp;
                float2 l3_off = float2(sin(t * _Layer3Speed * 0.39 + 1.2), cos(t * _Layer3Speed * 0.61 - 0.7)) * _Layer3Amp;

                float2 uv1 = i.baseNoiseUV + l1_off;
                float2 uv2 = i.baseNoiseUV * 1.8 + l2_off;
                float2 uv3 = i.baseNoiseUV * 0.8 + l3_off;

                float n1 = sampleNoise(uv1);
                float n2 = sampleNoise(uv2);
                float n3 = sampleNoise(uv3);

                float noise = saturate(n1 * 0.45 + n2 * 0.35 + n3 * 0.20);

                fixed4 mask = tex2D(_MaskTex, i.uv);
                float maskValue = (_UseMaskAlpha >= 0.5) ? mask.a : ((mask.r + mask.g + mask.b) * (1.0/3.0));
                maskValue = saturate(maskValue * _MaskBoost);

                float3 baseCol = _Color.rgb * _Brightness;
                float3 noisy = baseCol + (baseCol * _NoiseIntensity * (noise - 0.5));
                noisy *= i.vcol.rgb;

                float vertexAlpha = (i.vcol.a <= 0.0001) ? 1.0 : i.vcol.a;
                float finalAlpha = maskValue * _Color.a * vertexAlpha;

                if (finalAlpha < _AlphaCutoff) discard;
                return fixed4(noisy, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}