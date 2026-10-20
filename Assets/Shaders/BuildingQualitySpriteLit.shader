Shader "LoadGame/Building Quality Sprite Lit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Quality)]
        _SilverColor("Silver Color", Color) = (0.72, 0.82, 0.95, 1)
        _SilverIntensity("Silver Intensity", Range(0, 1)) = 1
        _GoldColor("Gold Color", Color) = (1, 0.72, 0.2, 1)
        _GoldIntensity("Gold Intensity", Range(0, 1)) = 1
        _ShineColor("Shine Color", Color) = (1, 1, 1, 1)
        _ShineSpeed("Shine Speed", Range(0, 2)) = 0.35
        _ShineWidth("Shine Width", Range(0.01, 0.5)) = 0.12
        _ShineIntensity("Shine Intensity", Range(0, 2)) = 0.75
        _RainbowSpeed("Rainbow Speed", Range(0, 2)) = 0.2
        _RainbowDensity("Rainbow Density", Range(0.5, 6)) = 2.2
        _RainbowSaturation("Rainbow Saturation", Range(0, 1)) = 0.9
        _RainbowIntensity("Rainbow Intensity", Range(0, 1)) = 0.85

        [HideInInspector] _QualityMode("Quality Mode", Float) = 0
        [HideInInspector] _QualityPhase("Quality Phase", Float) = 0
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _QualityMode;
            half _QualityPhase;
            half4 _SilverColor;
            half _SilverIntensity;
            half4 _GoldColor;
            half _GoldIntensity;
            half4 _ShineColor;
            half _ShineSpeed;
            half _ShineWidth;
            half _ShineIntensity;
            half _RainbowSpeed;
            half _RainbowDensity;
            half _RainbowSaturation;
            half _RainbowIntensity;
        CBUFFER_END

        half3 ApplyMetalShine(half3 source, half2 uv, half3 tintColor, half tintIntensity)
        {
            half luminance = dot(source, half3(0.299h, 0.587h, 0.114h));
            half3 metalTint = lerp(half3(1.0h, 1.0h, 1.0h), tintColor, tintIntensity * 0.5h);
            half diagonal = uv.x * 0.65h + uv.y * 0.35h;
            half shineCenter = frac(_Time.y * _ShineSpeed + _QualityPhase);
            half shineDistance = abs(frac(diagonal - shineCenter + 0.5h) - 0.5h);
            half shineCore = 1.0h - smoothstep(_ShineWidth * 0.15h, _ShineWidth * 0.55h, shineDistance);
            half shineHalo = 1.0h - smoothstep(_ShineWidth * 0.4h, _ShineWidth * 1.5h, shineDistance);
            half3 metalBase = source * metalTint;
            metalBase *= 1.0h - shineHalo * 0.18h * _ShineIntensity;
            half3 metalHighlight = lerp(_ShineColor.rgb, tintColor, 0.25h);
            half highlightVisibility = 0.55h + (1.0h - luminance) * 0.85h;
            return metalBase + metalHighlight * shineCore * _ShineIntensity * highlightVisibility;
        }

        half3 ApplyQualityColor(half3 source, half2 uv)
        {
            if (_QualityMode < 0.5h)
            {
                return source;
            }

            if (_QualityMode < 1.5h)
            {
                return ApplyMetalShine(source, uv, _SilverColor.rgb, _SilverIntensity);
            }

            if (_QualityMode < 2.5h)
            {
                return ApplyMetalShine(source, uv, _GoldColor.rgb, _GoldIntensity);
            }

            half luminance = dot(source, half3(0.299h, 0.587h, 0.114h));
            half hue = frac((uv.x * 0.7h + uv.y * 0.3h) * _RainbowDensity
                - _Time.y * _RainbowSpeed + _QualityPhase);
            half3 rainbow = 0.5h + 0.5h * cos(6.2831853h * (hue + half3(0.0h, 0.6667h, 0.3333h)));
            half3 rainbowTint = lerp(half3(1.0h, 1.0h, 1.0h), rainbow * 1.35h, _RainbowSaturation);
            half3 rainbowWithDetail = source * rainbowTint;
            half rainbowGlint = pow(saturate(0.5h + 0.5h * sin((uv.x - uv.y) * 18.0h + _Time.y * 1.4h)), 8.0h);
            rainbowWithDetail += rainbow * rainbowGlint * 0.18h * (0.35h + luminance);
            return lerp(source, rainbowWithDetail, _RainbowIntensity);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex LitVertex
            #pragma fragment QualityLitFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 QualityLitFragment(Varyings input) : SV_Target
            {
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                main.rgb = ApplyQualityColor(main.rgb, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="UniversalForward" "Queue"="Transparent" "RenderType"="Transparent" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex UnlitVertex
            #pragma fragment QualityUnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 QualityUnlitFragment(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                color.rgb = ApplyQualityColor(color.rgb, input.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
