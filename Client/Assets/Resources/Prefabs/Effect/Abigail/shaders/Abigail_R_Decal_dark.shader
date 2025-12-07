Shader "Abigail/R_Decal_dark"
{
    Properties
    {
        _MainTex("Mask Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.5
        _NoiseScale("Noise Scale", Float) = 1.0
        _MaskCutoff("Mask Cutoff", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        
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
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float _DissolveAmount;
            float _NoiseStrength;
            float _NoiseScale;
            float _MaskCutoff;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float maskValue = tex2D(_MainTex, i.uv).r;
                
                if (maskValue < _MaskCutoff)
                {
                    discard;
                }
                
                float2 noiseUV = i.uv * _NoiseScale;
                float noiseValue = tex2D(_NoiseTex, noiseUV).r;
                
                float finalNoise = noiseValue * maskValue;
                
                float dissolveValue = lerp(finalNoise, 1.0 - finalNoise, _NoiseStrength);
                
                float alpha = step(_DissolveAmount, dissolveValue);
                
                clip(alpha - 0.5);
                
                fixed4 finalColor = i.color;
                
                return finalColor;
            }
            ENDCG
        }
    }
}