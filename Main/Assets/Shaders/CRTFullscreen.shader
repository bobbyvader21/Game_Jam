Shader "Custom/URP_CRTFullscreen"
{
    Properties
    {
        _Curvature ("Screen Curvature", Range(0.0, 0.5)) = 0.15
        _VignetteStrength ("Corner Vignette", Range(0.0, 2.0)) = 0.4
        _ScanlineDensity ("Scanline Density", Float) = 240.0
        _ScanlineOpacity ("Scanline Opacity", Range(0.0, 1.0)) = 0.35
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 2.0
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 0.02)) = 0.003
        _PhosphorStrength ("RGB Phosphor Mask", Range(0.0, 1.0)) = 0.25
        _ScreenResolutionX ("Virtual Width (Phosphors)", Float) = 320.0
        _Blend ("Effect Blend", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "CRTPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Curvature;
            float _VignetteStrength;
            float _ScanlineDensity;
            float _ScanlineOpacity;
            float _ScanlineSpeed;
            float _ChromaticAberration;
            float _PhosphorStrength;
            float _ScreenResolutionX;
            float _Blend;

            // 1. Barrel Distortion Function
            float2 CurveUV(float2 uv, float curvature)
            {
                // Remap [0, 1] to [-1, 1]
                uv = uv * 2.0 - 1.0;
                
                // Calculate radial offset proportional to squared distance
                float2 offset = abs(uv.yx) * curvature;
                uv = uv + uv * (offset * offset);
                
                // Remap back to [0, 1]
                uv = uv * 0.5 + 0.5;
                return uv;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 rawUV = input.texcoord;

                // Early exit optimization when completely off
                if (_Blend <= 0.001)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, rawUV);
                }

                // Sample unedited screen color for blending
                half3 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, rawUV).rgb;

                // Scale distortion and aberration by _Blend for a smooth physical transition
                float2 uv = CurveUV(rawUV, _Curvature * _Blend);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(lerp(baseColor, half3(0.0, 0.0, 0.0), _Blend), 1.0);
                }

                float2 caOffset = float2(_ChromaticAberration * _Blend, 0.0);
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + caOffset).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - caOffset).b;
                half3 crtColor = half3(r, g, b);

                // Scanlines
                float scanlineWave = sin((uv.y * _ScanlineDensity * PI * 2.0) + (_Time.y * _ScanlineSpeed));
                float scanlineMask = lerp(1.0, scanlineWave * 0.5 + 0.5, _ScanlineOpacity);
                crtColor *= scanlineMask;

                // RGB Aperture Grille
                float subpixelIndex = frac(uv.x * _ScreenResolutionX) * 3.0;
                half3 phosphorMask = half3(1., 1.0, 1.0);
                if (subpixelIndex < 1.0)
                    phosphorMask = half3(1.0, 0.2, 0.2);
                else if (subpixelIndex < 2.0)
                    phosphorMask = half3(0.2, 1.0, 0.2);
                else
                    phosphorMask = half3(0.2, 0.2, 1.0);

                crtColor *= lerp(half3(1.0, 1.0, 1.0), phosphorMask, _PhosphorStrength);

                // Edge Vignette
                float2 vignetteUV = uv * (1.0 - uv.yx);
                float vignette = pow(saturate(vignetteUV.x * vignetteUV.y * 15.0), _VignetteStrength);
                crtColor *= vignette;

                // Final blend between clean image and CRT image
                half3 finalColor = lerp(baseColor, crtColor, _Blend);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
