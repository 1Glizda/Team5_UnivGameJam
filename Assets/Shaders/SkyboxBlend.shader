Shader "Custom/Skybox6SidedBlend"
{
    Properties
    {
        _Blend ("Blend", Range(0.0, 1.0)) = 0.0
        
        [Header(Skybox 1)]
        _FrontTex1 ("Front 1", 2D) = "white" {}
        _BackTex1 ("Back 1", 2D) = "white" {}
        _LeftTex1 ("Left 1", 2D) = "white" {}
        _RightTex1 ("Right 1", 2D) = "white" {}
        _UpTex1 ("Up 1", 2D) = "white" {}
        _DownTex1 ("Down 1", 2D) = "white" {}
        
        [Header(Skybox 2)]
        _FrontTex2 ("Front 2", 2D) = "white" {}
        _BackTex2 ("Back 2", 2D) = "white" {}
        _LeftTex2 ("Left 2", 2D) = "white" {}
        _RightTex2 ("Right 2", 2D) = "white" {}
        _UpTex2 ("Up 2", 2D) = "white" {}
        _DownTex2 ("Down 2", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float _Blend;
            sampler2D _FrontTex1, _BackTex1, _LeftTex1, _RightTex1, _UpTex1, _DownTex1;
            sampler2D _FrontTex2, _BackTex2, _LeftTex2, _RightTex2, _UpTex2, _DownTex2;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 Sample6Sided(float3 uv, sampler2D f, sampler2D b, sampler2D l, sampler2D r, sampler2D u, sampler2D d)
            {
                float3 absUV = abs(uv);
                fixed4 color = fixed4(0,0,0,1);
                
                if (absUV.x > absUV.y && absUV.x > absUV.z)
                {
                    if (uv.x > 0) color = tex2D(r, (uv.zy / uv.x + 1.0) * 0.5);
                    else color = tex2D(l, (float2(uv.z, -uv.y) / -uv.x + 1.0) * 0.5);
                }
                else if (absUV.y > absUV.z)
                {
                    if (uv.y > 0) color = tex2D(u, (uv.xz / uv.y + 1.0) * 0.5);
                    else color = tex2D(d, (float2(uv.x, -uv.z) / -uv.y + 1.0) * 0.5);
                }
                else
                {
                    if (uv.z > 0) color = tex2D(f, (float2(-uv.x, uv.y) / uv.z + 1.0) * 0.5);
                    else color = tex2D(b, (uv.xy / -uv.z + 1.0) * 0.5);
                }
                return color;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 uv = normalize(i.texcoord);
                fixed4 col1 = Sample6Sided(uv, _FrontTex1, _BackTex1, _LeftTex1, _RightTex1, _UpTex1, _DownTex1);
                fixed4 col2 = Sample6Sided(uv, _FrontTex2, _BackTex2, _LeftTex2, _RightTex2, _UpTex2, _DownTex2);
                return lerp(col1, col2, _Blend);
            }
            ENDCG
        }
    }
}
