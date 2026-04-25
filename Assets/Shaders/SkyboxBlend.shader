Shader "Skybox/CrossfadeBlend"
{
    Properties
    {
        _Blend ("Blend", Range(0.0, 1.0)) = 0.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        
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
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float _Blend;
            float _Rotation;
            
            TEXTURE2D(_FrontTex1); SAMPLER(sampler_FrontTex1);
            TEXTURE2D(_BackTex1); SAMPLER(sampler_BackTex1);
            TEXTURE2D(_LeftTex1); SAMPLER(sampler_LeftTex1);
            TEXTURE2D(_RightTex1); SAMPLER(sampler_RightTex1);
            TEXTURE2D(_UpTex1); SAMPLER(sampler_UpTex1);
            TEXTURE2D(_DownTex1); SAMPLER(sampler_DownTex1);
            
            TEXTURE2D(_FrontTex2); SAMPLER(sampler_FrontTex2);
            TEXTURE2D(_BackTex2); SAMPLER(sampler_BackTex2);
            TEXTURE2D(_LeftTex2); SAMPLER(sampler_LeftTex2);
            TEXTURE2D(_RightTex2); SAMPLER(sampler_RightTex2);
            TEXTURE2D(_UpTex2); SAMPLER(sampler_UpTex2);
            TEXTURE2D(_DownTex2); SAMPLER(sampler_DownTex2);

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                float2 xz = mul(m, vertex.xz);
                return float3(xz.x, vertex.y, xz.y);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.texcoord = RotateAroundYInDegrees(v.positionOS.xyz, -_Rotation);
                return o;
            }

            float4 SampleFace(float2 uv, TEXTURE2D_PARAM(tex, samp))
            {
                // Edge padding: slightly scale the UVs to prevent seam bleeding
                return SAMPLE_TEXTURE2D(tex, samp, uv * 0.5 + 0.5);
            }

            float4 Sample6Sided(float3 uv, 
                TEXTURE2D_PARAM(f, sf), TEXTURE2D_PARAM(b, sb), 
                TEXTURE2D_PARAM(l, sl), TEXTURE2D_PARAM(r, sr), 
                TEXTURE2D_PARAM(u, su), TEXTURE2D_PARAM(d, sd))
            {
                float3 absUV = abs(uv);
                float maxAxis = max(absUV.x, max(absUV.y, absUV.z));
                
                if (maxAxis == absUV.x)
                {
                    if (uv.x > 0) return SampleFace(float2(-uv.z, -uv.y) / uv.x, TEXTURE2D_ARGS(r, sr));
                    else return SampleFace(float2(uv.z, -uv.y) / -uv.x, TEXTURE2D_ARGS(l, sl));
                }
                else if (maxAxis == absUV.y)
                {
                    if (uv.y > 0) return SampleFace(float2(uv.x, uv.z) / uv.y, TEXTURE2D_ARGS(u, su));
                    else return SampleFace(float2(uv.x, -uv.z) / -uv.y, TEXTURE2D_ARGS(d, sd));
                }
                else
                {
                    if (uv.z > 0) return SampleFace(float2(uv.x, -uv.y) / uv.z, TEXTURE2D_ARGS(f, sf));
                    else return SampleFace(float2(-uv.x, -uv.y) / -uv.z, TEXTURE2D_ARGS(b, sb));
                }
            }

            float4 frag (Varyings i) : SV_Target
            {
                float3 uv = normalize(i.texcoord);
                float4 col1 = Sample6Sided(uv, 
                    TEXTURE2D_ARGS(_FrontTex1, sampler_FrontTex1), TEXTURE2D_ARGS(_BackTex1, sampler_BackTex1), 
                    TEXTURE2D_ARGS(_LeftTex1, sampler_LeftTex1), TEXTURE2D_ARGS(_RightTex1, sampler_RightTex1), 
                    TEXTURE2D_ARGS(_UpTex1, sampler_UpTex1), TEXTURE2D_ARGS(_DownTex1, sampler_DownTex1));
                    
                float4 col2 = Sample6Sided(uv, 
                    TEXTURE2D_ARGS(_FrontTex2, sampler_FrontTex2), TEXTURE2D_ARGS(_BackTex2, sampler_BackTex2), 
                    TEXTURE2D_ARGS(_LeftTex2, sampler_LeftTex2), TEXTURE2D_ARGS(_RightTex2, sampler_RightTex2), 
                    TEXTURE2D_ARGS(_UpTex2, sampler_UpTex2), TEXTURE2D_ARGS(_DownTex2, sampler_DownTex2));
                    
                return lerp(col1, col2, _Blend);
            }
            ENDHLSL
        }
    }
}
