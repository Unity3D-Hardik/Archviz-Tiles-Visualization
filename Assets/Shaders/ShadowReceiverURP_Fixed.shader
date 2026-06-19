Shader "Aimision/ShadowReceiverURP"
{
    Properties
    {
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        // Ensure the object participates in shadowmap rendering
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        // Main forward pass: unlit transparent overlay that draws only shadows
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float _ShadowStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 shadowCoord : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings v;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);

                float4 worldPos4 = mul(unity_ObjectToWorld, IN.positionOS);
                float3 worldPos = worldPos4.xyz;

                v.positionCS = TransformObjectToHClip(IN.positionOS);
                v.positionWS = worldPos;
                v.shadowCoord = TransformWorldToShadowCoord(worldPos);

                return v;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Get main light
                Light mainLight = GetMainLight(IN.shadowCoord);

                // Compute shadow attenuation (1 = lit, 0 = fully shadowed)
                float shadowAtten = mainLight.shadowAttenuation;

                // darkness factor where shadows fall
                float darkness = saturate(1.0 - shadowAtten) * _ShadowStrength;

                // output a transparent color that darkens underlying pixels where shadow exists
                return half4(0.0, 0.0, 0.0, darkness);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
