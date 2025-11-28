Shader "Custom/Beacon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.9, 0.9, 0.9, 1)
        _AllyColor ("Ally Color", Color) = (0.2, 0.5, 1.0, 1)
        _EnemyColor ("Enemy Color", Color) = (1.0, 0.0, 0.0, 1)
        _CaptureProgress ("Capture Progress", Range(0, 1)) = 0
        _CaptureTeam ("Capture Team", Int) = 0
        _OwningTeam ("Owning Team", Int) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _BaseColor;
            fixed4 _AllyColor;
            fixed4 _EnemyColor;
            float _CaptureProgress;
            int _CaptureTeam;
            int _OwningTeam;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float fill = step(i.uv.x, _CaptureProgress);
                
                // 배경색 결정: 소유 팀이 있으면 소유 팀 색상, 없으면 기본 색상
                fixed4 backgroundColor = _BaseColor;
                if (_OwningTeam == 1) backgroundColor = _AllyColor;
                else if (_OwningTeam == 2) backgroundColor = _EnemyColor;
                
                fixed4 finalColor = backgroundColor;
                
                if (_CaptureTeam == 1) // 아군 점령 중
                {
                    finalColor = lerp(backgroundColor, _AllyColor, fill);
                }
                else if (_CaptureTeam == 2) // 적군 점령 중
                {
                    finalColor = lerp(backgroundColor, _EnemyColor, fill);
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
}