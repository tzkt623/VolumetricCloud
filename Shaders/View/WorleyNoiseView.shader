﻿Shader "Tezcat/WorleyNoiseView"
{
	Properties
	{
		_Mix("Mix", Range(0, 1)) = 0
		_ShapeThreshold("ShapeThreshold", Float) = 1
		_NoiseType("NoiseType", Int) = 0
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
			#include "../Head/Function.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float3 uv3 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler3D _ShapeTex3D;
			sampler3D _DetailTex3D;

			int _Dimension;
			int _Channel;
			float _Mix;
			float _ShapeThreshold;
			int _NoiseType;

			float4 _MainTex2D_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv3 = v.vertex.xyz + 0.5f;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 col = 1;
				//mipmap有bug
				float4 noise = tex3Dlod(_ShapeTex3D, float4(i.uv3, 0));

				switch (_NoiseType)
				{
				case 0:
					col.rgb = noise.rrr;
					break;
				case 1:
					col.rgb = noise.ggg;
					break;
				case 2:
					col.rgb = noise.bbb;
					break;
				case 3:
					col.rgb = noise.aaa;
					break;
				case 4:
					col.rgb = noise.gba;
					break;
				case 5:
					col.rgb = dot(noise.gba, float3(0.625, 0.25, 0.125));
					break;
				case 6:
				{
					float worley_fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
					col.rgb = max(0.0, remap(noise.r, worley_fbm - 1, 1.0, 0.0, 1.0));
					break;
				}
				case 7:
				{
					float worley_fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
					float shape = remap(noise.r, worley_fbm - 1, 1.0, 0.0, 1.0);
					//shape = max(0.0, shape);

					float4 detail = tex3Dlod(_DetailTex3D, float4(i.uv3, 0));
					float detail_fbm = dot(detail.rgb, float3(0.625, 0.25, 0.125));
					float modifier = lerp(detail_fbm, 1 - detail_fbm, saturate(i.uv3.y * 5));
					col.rgb = saturate(remap(shape, modifier * _Mix, 1.0, 0.0, 1.0));
					//col.rgb = 1 - detail_fbm;
					break;
				}
				default:
					col.rgb = float3(1, 0, 1);
					break;
				}

				return col;
			}
			ENDCG
		}
	}
}
