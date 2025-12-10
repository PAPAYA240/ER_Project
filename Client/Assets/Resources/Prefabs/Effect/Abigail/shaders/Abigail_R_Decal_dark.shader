Shader "Abigail/R_Decal_dark"
{
    Properties
    {
        _MainTex("Mask Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _FlowSpeed("Flow Speed", Float) = 0.1
        _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.44
        _NoiseScale("Noise Scale", Float) = 2.0
        _MaskCutoff("Mask Cutoff", Range(0, 1)) = 0.5

        _DissolveTex("Dissolve Noise", 2D) = "white" {}
        _DissolveScale("Dissolve Scale", Float) = 3.0

        _StartTime("Start Time", Float) = 0.0
        _DissolveDuration("Dissolve Duration", Float) = 1.1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata_particle
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f_particle
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NoiseTex;
            float _NoiseScale;
            float _NoiseIntensity;
            float _MaskCutoff;
            float _FlowSpeed;

            sampler2D _DissolveTex;
            float _DissolveScale;

            float _StartTime;
            float _DissolveDuration;

            v2f_particle vert(appdata_particle v)
            {
                v2f_particle o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color  = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f_particle i) : SV_Target
            {
                float maskValue = tex2D(_MainTex, i.texcoord).r;
                if (maskValue < _MaskCutoff)
                    discard;

                float t = saturate((_Time.y - _StartTime) / _DissolveDuration);

                float2 duv = i.texcoord * _DissolveScale;
                duv += float2(_Time.y * 0.25, 0.0);

                float d = tex2D(_DissolveTex, duv).r;

                if (d < t)
                    discard;

                float2 noiseUV = i.texcoord * _NoiseScale;
                noiseUV.x += _Time.y * _FlowSpeed;

                float noise = tex2D(_NoiseTex, noiseUV).r;
                float dark = lerp(1.0, noise, _NoiseIntensity);

                return i.color * dark;
            }

            ENDCG
        }
    }
}
