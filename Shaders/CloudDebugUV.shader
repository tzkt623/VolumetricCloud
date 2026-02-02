Shader "Tezcat/CloudDebugUV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Tags
        {
            "LightMode" = "ForwardBase"
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "Head/Function.cginc"
			#include "Head/CloudHead.cginc"

            //sampler3D _Test3D;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
            };

            float4 calculateUVW(float3 rayO, float3 rayDir)
            {
	            float length_to_begin_pos = 0;
	            float cloud_thickness = 0;
	            if (!calculateCloudThickness(rayO, rayDir, length_to_begin_pos, cloud_thickness))
	            {
		            return float4(0.0f, 0.0f, 0.0f, 1.0f);
	            }
    
	            float3 pos = rayO;
	            //pos /= float3(10, 2, 10);

	            return float4(frac((pos + rayDir * length_to_begin_pos) * _ShapeScale), 0.0f);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.viewDir = calculateViewDir3(v.uv);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 ray_o, ray_dir, light_dir;
				float depth;
				calulateRayMarchDatas(i.uv, i.viewDir, ray_o, ray_dir, light_dir, depth);

	            float4 col = tex2D(_MainTex, i.uv);    
	            float4 uvw = calculateUVW(ray_o, ray_dir);
	            col.rgb = col.rgb * uvw.a + uvw.rgb * (1 - uvw.a);
	            return float4(col.rgb, 1.0);
            }
            ENDCG
        }
    }
}
