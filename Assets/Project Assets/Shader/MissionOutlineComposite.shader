// 마스크 RT를 팽창(dilate)시켜, "원본 실루엣 밖 & 팽창 안쪽" 픽셀에만 단일 강조색을 입히는 풀스크린 합성 셰이더.
// Pass 0: 외곽선 합성, Pass 1: 단순 복사(temp -> camera color 되돌리기용)
Shader "Hidden/MissionOutlineComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        // Core.hlsl 이 TEXTURE2D_X 등 텍스처 매크로를 먼저 정의해야 Blit.hlsl 이 컴파일된다.
        // Blit.hlsl 이 Vert / Varyings / _BlitTexture / sampler_PointClamp / sampler_LinearClamp 를 제공한다.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D(_MissionOutlineMask);
        float4 _OutlineTexelSize; // (1/w, 1/h, w, h) — RenderGraph 글로벌 텍스처는 _TexelSize 자동 채움을 신뢰할 수 없어 직접 전달
        float4 _OutlineColor;
        float _OutlineThickness; // 픽셀 단위 두께 (0 ~ MAX_RADIUS)
        ENDHLSL

        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragOutline

            #define MAX_RADIUS 6

            half4 fragOutline(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 실루엣 내부면 원본 유지 (내부 경계선 없음)
                half center = SAMPLE_TEXTURE2D(_MissionOutlineMask, sampler_PointClamp, uv).r;
                if (center > 0.5)
                    return sceneColor;

                int radius = (int)clamp(_OutlineThickness, 1.0, (float)MAX_RADIUS);
                float radiusSq = (float)(radius * radius);
                float2 texel = _OutlineTexelSize.xy;

                half found = 0;
                [unroll]
                for (int x = -MAX_RADIUS; x <= MAX_RADIUS; x++)
                {
                    [unroll]
                    for (int y = -MAX_RADIUS; y <= MAX_RADIUS; y++)
                    {
                        if (abs(x) > radius || abs(y) > radius) continue;
                        if ((float)(x * x + y * y) > radiusSq) continue;
                        float2 offset = float2(x, y) * texel;
                        found = max(found, SAMPLE_TEXTURE2D(_MissionOutlineMask, sampler_PointClamp, uv + offset).r);
                    }
                }

                if (found > 0.5)
                    return lerp(sceneColor, _OutlineColor, _OutlineColor.a);

                return sceneColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Copy"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragCopy

            half4 fragCopy(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, IN.texcoord);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
