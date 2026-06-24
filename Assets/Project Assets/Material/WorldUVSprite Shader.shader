Shader "Custom/WorldUVSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _PatternTex ("Pattern", 2D) = "white" {}
        // 값이 클수록 패턴 1타일이 월드에서 더 크게 보임 (units per tile)
        _PatternTiling ("Pattern Tiling (units/tile)", Vector) = (4, 4, 0, 0)
        _PatternOffset ("Pattern Offset", Vector) = (0, 0, 0, 0)
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float2 worldXY     : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_PatternTex); SAMPLER(sampler_PatternTex);

            float4 _PatternTiling;
            float4 _PatternOffset;
            float4 _Color;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldXY     = worldPos.xy;   // ← 핵심: 메시 UV 대신 월드 좌표
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 1) 스프라이트 알파 = 블록 모양 마스크
                half spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                // 2) 월드 좌표로 패턴 샘플 → 인접 블록끼리 무늬 자동 정렬
                float2 patternUV = IN.worldXY / _PatternTiling.xy + _PatternOffset.xy;
                half4  pattern   = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, patternUV);

                // 3) 패턴 색 × 틴트, 알파는 스프라이트 마스크로
                half4 col = pattern * _Color * IN.color;
                col.a     = spriteAlpha * _Color.a * IN.color.a;
                return col;
            }
            ENDHLSL
        }
    }
}
