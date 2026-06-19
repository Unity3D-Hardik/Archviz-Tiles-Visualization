Shader "Aimision/Tile_URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _GapColor("Gap Color", Color) = (0,0,0,1)
        _GroutSmoothness("Grout Smoothness", Range(0.0, 1.0)) = 0.08
        _GroutOcclusion("Grout Occlusion", Range(0.0, 1.0)) = 0.85
        _TileEdgeDarkening("Tile Edge Darkening", Range(0.0, 1.0)) = 0.12
        _TileEdgeWidth("Tile Edge Width", Range(0.0, 0.2)) = 0.03

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Glossiness", Range(0.0, 1.0)) = 0.5
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}

        [Toggle(_CLEARCOAT)] _ClearCoat("Clear Coat", Float) = 0
        _ClearCoatMask("Clear Coat Mask", Range(0.0, 1.0)) = 0.0
        _ClearCoatSmoothness("Clear Coat Smoothness", Range(0.0, 1.0)) = 0.7

        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 1.0

        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(Detail)]
        _DetailMap("Detail Albedo (RGB) Smoothness (A)", 2D) = "gray" {}
        [NoScaleOffset] _DetailNormalMap("Detail Normal", 2D) = "bump" {}
        _DetailNormalScale("Detail Normal Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoStrength("Detail Albedo Strength", Range(0.0, 1.0)) = 0.3
        _DetailSmoothnessStrength("Detail Smoothness Strength", Range(0.0, 1.0)) = 0.3
        _DetailTiling("Detail Tiling X,Y", Vector) = (6,6,0,0)

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Real World Scale)]
        [Toggle] _UseRealWorldMM("Use Real World Size (mm)", Float) = 1
        [Toggle] _AutoReadSizeFromTextureName("Auto Read Size From Texture Name", Float) = 1
        _TileSizeMM("Tile Size (mm) X,Y", Vector) = (300,300,0,0)
        _GapSizeMM("Gap Size (mm) X,Y", Vector) = (2,2,0,0)

        [HideInInspector] _ImageWidth("Image Width", Float) = 1
        [HideInInspector] _ImageHeight("Image Height", Float) = 1
        [HideInInspector] _SpacingX("Spacing X", Float) = 0.2
        [HideInInspector] _SpacingY("Spacing Y", Float) = 0.2
        _Rotation("Rotation", Range(0,360)) = 0

        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            // shader_feature = build-time only. Use multi_compile_local for features that
            // change at runtime (e.g. swapping textures calls EnableKeyword at runtime).
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ _ENVIRONMENTREFLECTIONS_OFF
            #pragma multi_compile_local_fragment _ _OCCLUSIONMAP
            #pragma multi_compile_local_fragment _ _EMISSION
            #pragma multi_compile_local _ _NORMALMAP
            #pragma multi_compile_local _ _DETAILMAP
            #pragma multi_compile_local_fragment _ _CLEARCOAT

            // Baked GI / lightmap variants — MUST be present or baked scenes render black
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // _SCREEN_SPACE_OCCLUSION, _FORWARD_PLUS, _LIGHT_COOKIES, _LIGHT_LAYERS omitted — not supported / not beneficial on Quest Adreno GPU
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options nomatrices renderinglayer
            #pragma prefer_hlslcc gles
            // Exclude only pure-console targets; keep d3d11/vulkan for PC editor testing
            #pragma exclude_renderers xboxone ps4 ps5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half3 vertexLighting : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
#ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD8;
#endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);             SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);             SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap);    SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);        SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);         SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_DetailMap);           SAMPLER(sampler_DetailMap);
            TEXTURE2D(_DetailNormalMap);     SAMPLER(sampler_DetailNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _GapColor;
                float4 _EmissionColor;
                float _GroutSmoothness;
                float _GroutOcclusion;
                float _TileEdgeDarkening;
                float _TileEdgeWidth;
                float _Cutoff;
                float _Metallic;
                float _Smoothness;
                float _ClearCoatMask;
                float _ClearCoatSmoothness;
                float _BumpScale;
                float _DetailNormalScale;
                float _DetailAlbedoStrength;
                float _DetailSmoothnessStrength;
                float _OcclusionStrength;
                float4 _DetailTiling;
                float _UseRealWorldMM;
                float4 _TileSizeMM;
                float4 _GapSizeMM;
                float _ImageWidth;
                float _ImageHeight;
                float _SpacingX;
                float _SpacingY;
                float _Rotation;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                #ifndef STEREO_INSTANCING_ON
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                #endif
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;

                output.vertexLighting = VertexLighting(posInputs.positionWS, normInputs.normalWS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

#ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Required for Quest single-pass instanced stereo — must be first line
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Keep tile layout in full float precision — world-position UV math needs it
                float useMM = step(0.5, _UseRealWorldMM);
                const float mmToMeter = 0.001;

                float imageWidthMM  = max(_TileSizeMM.x * mmToMeter, 0.0001);
                float imageHeightMM = max(_TileSizeMM.y * mmToMeter, 0.0001);
                float gapXMM        = _GapSizeMM.x * mmToMeter;
                float gapYMM        = _GapSizeMM.y * mmToMeter;

                float imageWidth  = lerp(max(_ImageWidth,  0.0001), imageWidthMM,  useMM);
                float imageHeight = lerp(max(_ImageHeight, 0.0001), imageHeightMM, useMM);
                float gapX        = lerp(_SpacingX / 10.0, gapXMM, useMM);
                float gapY        = lerp(_SpacingY / 10.0, gapYMM, useMM);

                float cellWidth  = max(imageWidth  + gapX, 0.0001);
                float cellHeight = max(imageHeight + gapY, 0.0001);

                float angle = radians(_Rotation);
                float s = sin(angle);
                float c = cos(angle);

                float2 rotatedWorldPos = float2(
                    input.positionWS.x * c - input.positionWS.z * s,
                    input.positionWS.x * s + input.positionWS.z * c
                );

                float2 localPos;
                localPos.x = frac(rotatedWorldPos.x / cellWidth) * cellWidth;
                localPos.y = frac(rotatedWorldPos.y / cellHeight) * cellHeight;

                // Branchless gap detection — avoids divergent execution on Adreno tile-based GPU
                // tileMask = 1.0 on tile surface, 0.0 in grout gap
                half tileMask = step(localPos.x, (half)imageWidth) * step(localPos.y, (half)imageHeight);

                float2 imageUV = float2(localPos.x / imageWidth, localPos.y / imageHeight);
                float2 uv = TRANSFORM_TEX(imageUV, _BaseMap);

                // Subtle edge darkening inside each tile improves perceived depth.
                float edgeMin = min(min(imageUV.x, imageUV.y), min(1.0 - imageUV.x, 1.0 - imageUV.y));
                float edgeWidth = max(_TileEdgeWidth, 0.0001);
                half edgeMask = saturate(1.0h - (half)(edgeMin / edgeWidth));

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                baseSample.rgb = baseSample.rgb * (1.0h - edgeMask * (half)_TileEdgeDarkening);
                half4 albedoAlpha = lerp(_GapColor, baseSample, tileMask);

                #if defined(_ALPHATEST_ON)
                    clip(albedoAlpha.a - _Cutoff);
                #endif

                half metallic = _Metallic;
                half smoothness = _Smoothness;
                #if defined(_METALLICSPECGLOSSMAP)
                    half4 m = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
                    metallic = m.r;
                    smoothness = m.a * _Smoothness;
                #endif

                // Declare detailUV once to avoid redeclaration error when both _NORMALMAP and _DETAILMAP are active
                #if defined(_DETAILMAP)
                    float2 detailUV = uv * _DetailTiling.xy;
                    half4 detailTex = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV);
                    half3 detailAlbedo = detailTex.rgb * 2.0h - 1.0h;
                    albedoAlpha.rgb = saturate(albedoAlpha.rgb * (1.0h + detailAlbedo * (half)_DetailAlbedoStrength * tileMask));
                    smoothness = saturate(smoothness + (detailTex.a - 0.5h) * 2.0h * (half)_DetailSmoothnessStrength);
                #endif

                half3 normalWS = normalize(input.normalWS);
                #if defined(_NORMALMAP)
                    half3 n = normalize(input.normalWS);
                    half3 t = normalize(input.tangentWS.xyz);
                    half3 b = normalize(cross(n, t) * input.tangentWS.w);
                    half3x3 tbn = half3x3(t, b, n);
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                    #if defined(_DETAILMAP)
                        // detailUV already declared above — reuse it
                        half3 detailTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV), _DetailNormalScale);
                        normalTS = normalize(half3(normalTS.xy + detailTS.xy, normalTS.z * detailTS.z));
                    #endif
                    normalWS = normalize(mul(normalTS, tbn));
                #endif

                // Branchless gap surface reset — lerp to flat/non-reflective grout when in gap
                half3 flatNormalWS = normalize(input.normalWS);
                metallic   = metallic   * tileMask;
                smoothness = lerp((half)_GroutSmoothness, smoothness, tileMask);
                normalWS   = lerp(flatNormalWS, normalWS, tileMask);

                half occlusion = 1.0h;
                #if defined(_OCCLUSIONMAP)
                    half occTex = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                    occlusion = LerpWhiteTo(occTex, _OcclusionStrength);
                #endif
                occlusion = lerp((half)_GroutOcclusion, occlusion, tileMask);

                half3 emission = 0;
                #if defined(_EMISSION)
                    emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
                #endif
                emission *= tileMask;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = input.vertexLighting;
                // SAMPLE_GI handles static lightmap, dynamic lightmap, and SH correctly
#if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
#else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
#endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                // Proper shadowMask for baked + mixed lighting modes
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON)
                inputData.shadowMask = unity_ProbesOcclusion;
#else
                inputData.shadowMask = half4(1, 1, 1, 1);
#endif

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.alpha = albedoAlpha.a;
                surfaceData.metallic = metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = occlusion;
                surfaceData.emission = emission;
                // tileMask = 1 on tile, 0 in grout — zero out clearcoat in gap branchlessly
                #if defined(_CLEARCOAT)
                    surfaceData.clearCoatMask = (half)_ClearCoatMask * tileMask;
                    surfaceData.clearCoatSmoothness = (half)_ClearCoatSmoothness * tileMask;
                #else
                    surfaceData.clearCoatMask = 0.0h;
                    surfaceData.clearCoatSmoothness = 0.0h;
                #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
        UsePass "Universal Render Pipeline/Lit/Universal2D"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "TileShaderGUI"
}