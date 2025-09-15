Shader "Custom/PlanetReveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _RevealRadius ("Reveal Radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _RevealRadius;
            float _FullReveal;
            float4 _RevealedPoints[64];
            int _PointCount;

            v2f vert (appdata v)
            {
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                float luminance = dot(col.rgb, float3(0.3, 0.59, 0.11));
                fixed4 grayCol = fixed4(luminance, luminance, luminance, col.a);

                float factor = 0;
                if (_FullReveal > 0.5)
                {
                    factor = 1;
                }
                else if (_PointCount > 0)
                {
                    float minAngle = 3.1415926535;
                    float3 norm = normalize(i.worldNormal);
                    for (int idx = 0; idx < _PointCount; idx++)
                    {
                        float cosAngle = dot(norm, _RevealedPoints[idx].xyz);
                        cosAngle = clamp(cosAngle, -1, 1);
                        float angle = acos(cosAngle);
                        minAngle = min(minAngle, angle);
                    }
                    factor = 1 - smoothstep(_RevealRadius * 0.5, _RevealRadius, minAngle);
                }

                fixed4 final = lerp(grayCol, col, factor);
                return final;
            }
            ENDCG
        }
    }
}