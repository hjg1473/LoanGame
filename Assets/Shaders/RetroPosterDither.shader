Shader "Hidden/LoadGame/Retro Poster Dither"
{
    Properties
    {
        _ColorLevels("Color Levels", Float) = 32
        _DitherStrength("Dither Strength", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Retro Poster Dither"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _ColorLevels;
                float _DitherStrength;
            CBUFFER_END

            float Bayer4x4(uint2 pixel)
            {
                uint x = pixel.x & 3u;
                uint y = pixel.y & 3u;
                uint index = x + y * 4u;

                if (index == 0u) return 0.0;
                if (index == 1u) return 8.0;
                if (index == 2u) return 2.0;
                if (index == 3u) return 10.0;
                if (index == 4u) return 12.0;
                if (index == 5u) return 4.0;
                if (index == 6u) return 14.0;
                if (index == 7u) return 6.0;
                if (index == 8u) return 3.0;
                if (index == 9u) return 11.0;
                if (index == 10u) return 1.0;
                if (index == 11u) return 9.0;
                if (index == 12u) return 15.0;
                if (index == 13u) return 7.0;
                if (index == 14u) return 13.0;
                return 5.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float levels = max(2.0, floor(_ColorLevels + 0.5));
                float quantizationSteps = levels - 1.0;

                // Quantizing in display space keeps the perceived brightness
                // more stable than quantizing the linear render target.
                float3 displayColor = LinearToSRGB(max(source.rgb, 0.0h));
                float threshold = Bayer4x4((uint2)input.positionCS.xy) / 16.0 - 0.46875;
                displayColor += threshold * saturate(_DitherStrength) / quantizationSteps;
                displayColor = floor(saturate(displayColor) * quantizationSteps + 0.5) / quantizationSteps;

                return half4(SRGBToLinear(displayColor), source.a);
            }
            ENDHLSL
        }
    }
}
