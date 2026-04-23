Shader "Custom/AcidKaleidoscope"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Speed ("Swirl Speed", Float) = 1.0
        _Complexity ("Kaleidoscope Arms", Float) = 5.0
        _Saturation ("Saturation", Range(0,1)) = 1.0
        _Brightness ("Brightness", Range(0,1)) = 1.0
        [Toggle] _IsRound ("Make Round (Check for Particles)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Speed;
            float _Complexity;
            float _Saturation;
            float _Brightness;
            float _IsRound;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float radius = length(uv);
                float angle = atan2(uv.y, uv.x);
                
                float time = _Time.y * _Speed;
                float w = sin(angle * _Complexity + time) * cos(radius * 10.0 - time * 2.0);
                
                float hue = frac(w * 0.5 + time * 0.2);
                float3 rgb = hsv2rgb(float3(hue, _Saturation, _Brightness));
                
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // If _IsRound is checked (1), it cuts out a circle. If unchecked (0), it leaves it as a rectangle!
                float circleAlpha = smoothstep(0.5, 0.45, radius);
                float finalShapeAlpha = lerp(1.0, circleAlpha, _IsRound);
                
                return fixed4(rgb * i.color.rgb, texColor.a * i.color.a * finalShapeAlpha);
            }
            ENDCG
        }
    }
}
