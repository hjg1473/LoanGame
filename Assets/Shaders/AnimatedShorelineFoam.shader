Shader "LoadGame/Animated Shoreline Foam"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _FoamColor("Foam Color", Color) = (1, 1, 1, 0.85)
        _FoamWidth("Foam Width", Range(0.005, 0.2)) = 0.055
        _FoamSoftness("Foam Softness", Range(0.001, 0.1)) = 0.018
        _NoiseScale("Noise Scale", Range(0.1, 20)) = 5
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.45
        _FlowSpeed("Flow Speed", Range(0, 5)) = 0.65
        _PulseSpeed("Pulse Speed", Range(0, 5)) = 1.1

        [Header(Surface Color Noise)]
        [Toggle] _EnableSurfaceNoise("Enable Surface Noise", Float) = 1
        _SurfaceBaseColor("Base Blue", Color) = (0.03, 0.48, 0.95, 1)
        _SurfaceShallowColor("Light Cyan", Color) = (0.08, 0.62, 0.92, 1)
        _SurfaceDeepColor("Deep Blue", Color) = (0.02, 0.36, 0.78, 1)
        _SurfaceNoiseScale("Surface Noise Scale", Range(0.005, 1)) = 0.045
        _SurfaceNoiseStrength("Surface Noise Strength", Range(0, 1)) = 0.12
        _SurfaceOverlayOpacity("Surface Overlay Opacity", Range(0, 1)) = 0.3

        [Header(Animated Ripples)]
        [Toggle] _EnableRipples("Enable Ripples", Float) = 1
        _RippleColor("Ripple Color", Color) = (0.45, 0.9, 1, 1)
        _RippleDensity("Ripple Density", Range(0, 1)) = 0.045
        _RippleSpeed("Ripple Speed", Range(0, 3)) = 0.18
        _RippleOpacity("Ripple Opacity", Range(0, 1)) = 0.2
        _RippleWorldScale("Ripple World Scale", Range(0.1, 3)) = 0.42

        [Header(Sunlight Glints)]
        [Toggle] _EnableGlints("Enable Sunlight Glints", Float) = 0
        _GlintColor("Glint Color", Color) = (1, 1, 0.82, 1)
        _GlintDensity("Glint Density", Range(0, 1)) = 0.015
        _GlintSpeed("Glint Speed", Range(0, 3)) = 0.35
        _GlintOpacity("Glint Opacity", Range(0, 1)) = 0.22
        [HideInInspector] _SpriteUVRect("Sprite UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // Surface noise, sparse ripples and glints exceed the temporary
            // register/instruction limits of the old SM2 fragment profile.
            // SM3 remains compatible with the project's GLES3/WebGL2 targets.
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FoamColor;
                half4 _Color;
                float _FoamWidth;
                float _FoamSoftness;
                float _NoiseScale;
                float _NoiseStrength;
                float _FlowSpeed;
                float _PulseSpeed;
                float _EnableSurfaceNoise;
                half4 _SurfaceBaseColor;
                half4 _SurfaceShallowColor;
                half4 _SurfaceDeepColor;
                float _SurfaceNoiseScale;
                float _SurfaceNoiseStrength;
                float _SurfaceOverlayOpacity;
                float _EnableRipples;
                half4 _RippleColor;
                float _RippleDensity;
                float _RippleSpeed;
                float _RippleOpacity;
                float _RippleWorldScale;
                float _EnableGlints;
                half4 _GlintColor;
                float _GlintDensity;
                float _GlintSpeed;
                float _GlintOpacity;
                float4 _SpriteUVRect;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x),
                    lerp(Hash21(cell + float2(0, 1)), Hash21(cell + 1), local.x),
                    local.y);
            }

            float LifePulse(float phase)
            {
                return smoothstep(0.0, 0.18, phase) * (1.0 - smoothstep(0.62, 1.0, phase));
            }

            float RippleStroke(float2 local, float verticalOffset, float rippleLength, float thickness, float slope, float curve)
            {
                float curvedY = verticalOffset + local.x * slope + local.x * local.x * curve;
                float lineDistance = abs(local.y - curvedY);
                float body = 1.0 - smoothstep(thickness, thickness + 0.018, lineDistance);
                float ends = 1.0 - smoothstep(rippleLength, rippleLength + 0.04, abs(local.x));
                return body * ends;
            }

            float AnimatedRipple(float2 positionWS, float time)
            {
                float2 scaled = positionWS * max(0.01, _RippleWorldScale);
                float2 cell = floor(scaled);
                float random = Hash21(cell + 19.17);
                float exists = step(1.0 - _RippleDensity, random);
                float phase = frac(time * _RippleSpeed * 0.18 + Hash21(cell + 43.71));
                float2 local = frac(scaled) - 0.5;
                local.x -= (phase - 0.5) * 0.055;

                float variationA = Hash21(cell + 7.13);
                float variationB = Hash21(cell + 27.61);
                float direction = lerp(-0.16, 0.16, variationA);
                float curve = lerp(-0.65, 0.65, variationB);

                float strokeA = RippleStroke(
                    local,
                    -0.075,
                    lerp(0.105, 0.22, variationA),
                    lerp(0.012, 0.021, variationB),
                    direction,
                    curve);
                float strokeB = RippleStroke(
                    local - float2(0.055, 0.0),
                    0.005,
                    lerp(0.075, 0.18, variationB),
                    lerp(0.011, 0.018, variationA),
                    -direction * 0.55,
                    -curve * 0.72);
                float thirdStrokeEnabled = step(0.52, Hash21(cell + 51.83));
                float strokeC = RippleStroke(
                    local + float2(0.04, 0.0),
                    0.078,
                    lerp(0.065, 0.145, variationA),
                    0.012,
                    direction * 0.35,
                    curve * 0.48) * thirdStrokeEnabled;

                float cluster = max(strokeA, max(strokeB, strokeC));
                return cluster * LifePulse(phase) * exists;
            }

            float AnimatedGlint(float2 positionWS, float time)
            {
                float2 scaled = positionWS * 1.35;
                float2 cell = floor(scaled);
                float random = Hash21(cell + 91.37);
                float exists = step(1.0 - _GlintDensity, random);
                float phase = frac(time * _GlintSpeed * 0.24 + Hash21(cell + 12.53));
                float2 center = float2(
                    lerp(0.22, 0.78, Hash21(cell + 3.41)),
                    lerp(0.22, 0.78, Hash21(cell + 6.79)));
                float2 local = abs(frac(scaled) - center);
                float horizontal = (1.0 - smoothstep(0.07, 0.12, local.x))
                    * (1.0 - smoothstep(0.012, 0.028, local.y));
                return horizontal * LifePulse(phase) * exists;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS.xy;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half sourceAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(sourceAlpha - 0.001h);

                float2 uv = (input.uv - _SpriteUVRect.xy) / max(_SpriteUVRect.zw, float2(0.0001, 0.0001));
                float northDistance = abs(uv.y - uv.x - 0.5) * 0.70710678;
                float eastDistance = abs(uv.x + uv.y - 1.5) * 0.70710678;
                float southDistance = abs(uv.y - uv.x + 0.5) * 0.70710678;
                float westDistance = abs(uv.x + uv.y - 0.5) * 0.70710678;

                float northRegion = step(uv.x, 0.5) * step(0.5, uv.y);
                float eastRegion = step(0.5, uv.x) * step(0.5, uv.y);
                float southRegion = step(0.5, uv.x) * step(uv.y, 0.5);
                float westRegion = step(uv.x, 0.5) * step(uv.y, 0.5);

                float distanceToShore = 10.0;
                distanceToShore = lerp(distanceToShore, min(distanceToShore, northDistance), step(0.5, input.color.r) * northRegion);
                distanceToShore = lerp(distanceToShore, min(distanceToShore, eastDistance), step(0.5, input.color.g) * eastRegion);
                distanceToShore = lerp(distanceToShore, min(distanceToShore, southDistance), step(0.5, input.color.b) * southRegion);
                distanceToShore = lerp(distanceToShore, min(distanceToShore, westDistance), step(0.5, input.color.a) * westRegion);

                float time = _Time.y;

                half3 surfaceColor = _SurfaceBaseColor.rgb;
                half surfaceAlpha = 0.0h;
                [branch] if (_EnableSurfaceNoise > 0.5)
                {
                    float surfaceNoiseLarge = ValueNoise(input.positionWS * _SurfaceNoiseScale + float2(31.7, 18.9));
                    float surfaceNoiseWide = ValueNoise(input.positionWS * (_SurfaceNoiseScale * 0.37) + float2(73.1, 9.4));
                    float surfaceNoise = lerp(surfaceNoiseWide, surfaceNoiseLarge, 0.32);
                    half3 lowSurfaceColor = lerp(_SurfaceDeepColor.rgb, _SurfaceBaseColor.rgb, smoothstep(0.0, 0.5, surfaceNoise));
                    half3 highSurfaceColor = lerp(_SurfaceBaseColor.rgb, _SurfaceShallowColor.rgb, smoothstep(0.5, 1.0, surfaceNoise));
                    surfaceColor = lerp(lowSurfaceColor, highSurfaceColor, step(0.5, surfaceNoise));
                    surfaceColor = lerp(_SurfaceBaseColor.rgb, surfaceColor, _SurfaceNoiseStrength);
                    surfaceAlpha = sourceAlpha * _SurfaceOverlayOpacity;
                }

                float ripple = 0.0;
                [branch] if (_EnableRipples > 0.5)
                {
                    ripple = AnimatedRipple(input.positionWS, time);
                }
                half rippleAlpha = sourceAlpha * ripple * _RippleOpacity * _RippleColor.a;

                float glint = 0.0;
                [branch] if (_EnableGlints > 0.5)
                {
                    glint = AnimatedGlint(input.positionWS, time);
                }
                half glintAlpha = sourceAlpha * glint * _GlintOpacity * _GlintColor.a;

                float2 noisePosition = input.positionWS * _NoiseScale;
                noisePosition += float2(time * _FlowSpeed, -time * _FlowSpeed * 0.37);
                float noise = ValueNoise(noisePosition);
                float animatedWidth = _FoamWidth * (1.0 + sin(time * _PulseSpeed + noise * 6.28318) * 0.18);
                float edge = 1.0 - smoothstep(animatedWidth, animatedWidth + _FoamSoftness, distanceToShore);
                float breakup = lerp(1.0, smoothstep(0.18, 0.72, noise), _NoiseStrength);

                float innerBandCenter = animatedWidth * 1.75;
                float innerBand = 1.0 - smoothstep(
                    _FoamSoftness,
                    _FoamSoftness * 2.5,
                    abs(distanceToShore - innerBandCenter));
                innerBand *= 0.28 * smoothstep(0.35, 0.8, 1.0 - noise);

                half foamAlpha = saturate((edge * breakup + innerBand) * sourceAlpha * _FoamColor.a);

                half3 finalColor = surfaceColor;
                finalColor = lerp(finalColor, _RippleColor.rgb, rippleAlpha);
                finalColor = lerp(finalColor, _GlintColor.rgb, glintAlpha);
                finalColor = lerp(finalColor, _FoamColor.rgb, foamAlpha);
                half finalAlpha = saturate(surfaceAlpha + rippleAlpha + glintAlpha + foamAlpha);
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
