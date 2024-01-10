Shader "Tezcat/PerlinNoiseView"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 uvw : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler3D _ShapeTex3D;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvw = v.vertex.xyz + 0.5f;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = float4(tex3D(_ShapeTex3D, i.uvw).rrr, 1.0f);
                return col;
            }
            ENDCG
        }
    }
}
