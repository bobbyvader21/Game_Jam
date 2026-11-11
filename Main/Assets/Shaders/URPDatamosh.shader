Shader "Custom/URPDatamosh"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "DatamoshPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_MotionVectorTexture);
            SAMPLER(sampler_MotionVectorTexture);

            TEXTURE2D_X(_HistoryTex);
            SAMPLER(sampler_HistoryTex);

            float _Button;
            float _BlockSize;
            float _NoiseAmount; // 0 = pure clean smear, 1 = classic lossy noise

            float nrand(float x, float y)
            {
                return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // 1. Macroblock UV rounding (set _BlockSize to 1 in Inspector for smooth per-pixel smear)
                float2 blocks = _ScreenParams.xy / max(_BlockSize, 1.0);
                float2 uvr = (_BlockSize > 1.0) ? (round(uv * blocks) / blocks) : uv;

                // 2. Sample URP motion vectors
                float2 mot = SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, uvr).rg;

                // 3. Optional noise (disabled when _NoiseAmount == 0)
                float n = nrand(_Time.x, uvr.x + uvr.y * _ScreenParams.x);
                if (_NoiseAmount > 0.001)
                {
                    float loss = round(n / 1.4) * _NoiseAmount;
                    mot = max(abs(mot) - loss, 0.0) * sign(mot);
                }

                // 4. Displace previous frame UVs
                float2 mvuv = saturate(uv - mot);

                half4 currentCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 historyCol = SAMPLE_TEXTURE2D_X(_HistoryTex, sampler_LinearClamp, mvuv);

                // Optional noise when active
                float activeNoise = lerp(1.0, round(1.0 - (n / 1.4)), _NoiseAmount);

                // Multiply smoothed _Button (0.0 to 1.0) by the noise mask
                float recycleMask = saturate(_Button * activeNoise);

                return lerp(currentCol, historyCol, recycleMask);

            }
            ENDHLSL
        }
    }
}
