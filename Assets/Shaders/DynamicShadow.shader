Shader "Aimision/DynamicShadow"
{
    Properties
    {
        [Enum(Circle, 0, Square, 1, Soft Circle, 2, Soft Square, 3, Rounded Square, 4)] _ShadowShape ("Shadow Shape", Float) = 0
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.7
        _ShadowSize ("Shadow Size", Range(0.1, 2)) = 1.0
        _ShadowHardness ("Shadow Hardness", Range(0.1, 5)) = 1.5
        _CornerRadius ("Corner Radius", Range(0, 1)) = 0.3
        _Opacity ("Overall Opacity", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            float _ShadowShape;
            float _ShadowIntensity;
            float _ShadowSize;
            float _ShadowHardness;
            float _CornerRadius;
            float _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Center coordinates
                float2 center = i.uv - fixed2(0.5, 0.5);
                float2 absCenter = abs(center * 2.0);

                fixed shadowAlpha = 0.0;

                // Circle shape
                if (_ShadowShape < 0.5)
                {
                    float dist = length(absCenter);
                    shadowAlpha = saturate((1.0 - dist / _ShadowSize) * _ShadowHardness);
                }
                // Square shape
                else if (_ShadowShape < 1.5)
                {
                    float maxDist = max(absCenter.x, absCenter.y);
                    shadowAlpha = saturate((1.0 - maxDist / _ShadowSize) * _ShadowHardness);
                }
                // Soft Circle (Gaussian)
                else if (_ShadowShape < 2.5)
                {
                    float dist = length(absCenter);
                    shadowAlpha = exp(-dist * dist * _ShadowHardness / _ShadowSize);
                }
                // Soft Square
                else if (_ShadowShape < 3.5)
                {
                    float maxDist = max(absCenter.x, absCenter.y);
                    shadowAlpha = exp(-maxDist * maxDist * _ShadowHardness / _ShadowSize);
                }
                // Rounded Square (with corner radius control)
                else
                {
                    float2 cornerCenter = absCenter - (_ShadowSize * (1.0 - _CornerRadius));
                    cornerCenter = max(cornerCenter, fixed2(0, 0));

                    float cornerDist = length(cornerCenter);
                    float edgeDist = max(absCenter.x, absCenter.y) - (_ShadowSize * (1.0 - _CornerRadius));

                    float dist = max(cornerDist, edgeDist);
                    shadowAlpha = saturate((1.0 - dist / (_ShadowSize * _CornerRadius)) * _ShadowHardness);
                }

                // Apply intensity and opacity
                shadowAlpha *= _ShadowIntensity * _Opacity;

                // Black shadow
                return fixed4(0, 0, 0, shadowAlpha);
            }
            ENDCG
        }
    }

    Fallback "Transparent/VertexLit"
}

