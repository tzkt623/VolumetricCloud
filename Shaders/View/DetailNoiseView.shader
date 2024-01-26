Shader "Tezcat/DetailNoiseView"
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

            sampler3D _DetailTex3D;
            int _DetailLevel;

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
                float4 col = float4(1, 0, 0, 1);

                
                switch (_DetailLevel)
                {
                case 0:
                    col.rgb = tex3D(_DetailTex3D, i.uvw).rrr;
                    //col.rgb = float4(1, 0, 0, 0);
                    break;
                case 1:
                    col.rgb = tex3D(_DetailTex3D, i.uvw).ggg;
                    //col.rgb = float4(0, 1, 0, 0);
                    break;
                case 2:
                    col.rgb = tex3D(_DetailTex3D, i.uvw).bbb;
                    //col.rgb = float4(0, 0, 1, 0);
                    break;
                case 3:
                    col.rgb = tex3D(_DetailTex3D, i.uvw).rgb;
                    break;
                case 4:
                    col.rgb = tex3D(_DetailTex3D, i.uvw).aaa;
                    break;
                }
                

                return col;
            }
            ENDCG
        }
    }
}
