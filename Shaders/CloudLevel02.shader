Shader "Tezcat/CloudLevel02"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "Function.cginc"
			#include "CloudHead.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			//return xyz=LightEnergy w=Transmittance
			float calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				//x距离起点的距离,y内部长度
				float2 cloud_area_data;
				float dst_to_begin_pos;
				float cloud_thickness;

				if (_DrawPlanetArea)
				{
					if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _PlanetCloudThickness, rayOrg, rayDir, cloud_area_data))
					{
						return 0.0;
					}

					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
				}
				else
				{
					cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, rayOrg, rayDir);
					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
					if (cloud_thickness <= 0)
					{
						return 0.0;
					}
				}

				float transmittance = 1;
				float light_total_energy = 0;

				float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}

				float step_thickness = cloud_thickness / _StepThickness;
				float total_thickness = random_offset + step_thickness;

				float3 begin_pos = rayOrg + rayDir * (dst_to_begin_pos + random_offset);
				float3 step_dir_length = rayDir * step_thickness;

				int zero_count = 0;
				float pre_density = 0;
				float total_density = 0;
				float test_density = 0;

				while (total_thickness <= cloud_thickness)
				{
					if (total_thickness + dst_to_begin_pos > depth)
					{
						break;
					}

					total_density += _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, begin_pos + _CloudOffset, 0).r;
					if (total_density >= 1)
					{
						break;
					}
					
					begin_pos += step_dir_length;
					total_thickness += step_thickness;
				}

				return total_density * _ShapeDensityStrength;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.viewDir = calculateViewDir(v.uv);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 ray_o;
				float3 ray_dir;
				float3 light_dir;
				float linear_eye_depth;
				calulateRayMarchDatas(i.uv, i.viewDir
					, ray_o, ray_dir, light_dir, linear_eye_depth);

				float col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

				float3 col = tex2D(_ScreenTex, i.uv);
				float3 cloud_col = _LightColor0 * col_params;

				col = col * (1 - col_params) + cloud_col;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}