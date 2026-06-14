Shader "Custom/Tiller"
{
    Properties
    {
        _MainTex ("Image", 2D) = "white" {}
        _GapColor ("Gap Color", Color) = (0,0,0,1)

        _ImageWidth ("Image Width", Float) = 1
        _ImageHeight ("Image Height", Float) = 1

        _SpacingX ("Spacing X", Float) = 0.2
        _SpacingY ("Spacing Y", Float) = 0.2

        _Rotation ("Rotation", Range(0,360)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _GapColor;

            float _ImageWidth;
            float _ImageHeight;

            float _SpacingX;
            float _SpacingY;

            float _Rotation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float cellWidth  = _ImageWidth + (_SpacingX / 10);
                float cellHeight = _ImageHeight + (_SpacingY/ 10);

                float2 localPos;

                localPos.x = frac(i.worldPos.x / cellWidth) * cellWidth;
                localPos.y = frac(i.worldPos.z / cellHeight) * cellHeight;

                float angle = radians(_Rotation);
                float s = sin(angle);
                float c = cos(angle);

                // Rotate the world coordinates
                float2 rotatedWorldPos = float2(i.worldPos.x * c - i.worldPos.z * s, i.worldPos.x * s + i.worldPos.z * c);

                // Then tile using rotated coordinates
                localPos.x = frac(rotatedWorldPos.x / cellWidth) * cellWidth;
                localPos.y = frac(rotatedWorldPos.y / cellHeight) * cellHeight;

                if(localPos.x > _ImageWidth || localPos.y > _ImageHeight) return _GapColor;

                float2 imageUV;

                imageUV.x = localPos.x / _ImageWidth;
                imageUV.y = localPos.y / _ImageHeight;

                return tex2D(_MainTex, imageUV);
            }
            ENDCG
        }
    }
}