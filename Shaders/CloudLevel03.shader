Shader "Tezcat/CloudLevel03"
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

			float calculateDensity(in float3 pos)
			{
				float edge;
				if (_DrawPlanetArea)
				{
					edge = calculateEdgeForPlanet(pos, _PlanetCloudThickness.y, _PlanetData.xyz, _PlanetData.w);
				}
				else
				{
					edge = calculateEdgeForBox(pos, _BoxMin, _BoxMax);
				}


				float4 noise = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, pos * _ShapeScale * 0.01f + _CloudOffset, 0);
				float fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
				float shape = remap(noise.r, fbm - 1, 1.0, 0.0, 1.0);

				return clamp(shape - _DensityThreshold, 0.0, 1.0) * _ShapeDensityStrength * edge;
			}

			//return xyz=LightEnergy w=Transmittance
			float3 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				//x距离起点的距离,y内部长度
				float2 cloud_area_data;
				float dst_to_begin_pos;
				float cloud_thickness;

				if (_DrawPlanetArea)
				{
					if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _PlanetCloudThickness, rayOrg, rayDir, cloud_area_data))
					{
						return float3(0.0f, 1.0f, 1.0f);
					}

					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
				}
				else
				{
					cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, rayOrg, rayDir);
					cloud_thickness = cloud_area_data.y;

					if (cloud_thickness <= 0)
					{
						return float3(0.0f, 1.0f, 1.0f);
					}

					dst_to_begin_pos = cloud_area_data.x;
				}

				float transmittance = 0.68;
				float final_light = 0;

				float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}

				float step_thickness = cloud_thickness / _StepThickness;

				float3 step_dir_length = rayDir * step_thickness;
				float total_thickness = random_offset + step_thickness;
				float total_density = 0;

				float3 begin_pos = rayOrg + rayDir * (dst_to_begin_pos + random_offset);
				begin_pos += step_dir_length;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _EnergyParams);

				while (total_thickness <= cloud_thickness)
				{
					if (total_thickness + dst_to_begin_pos > depth)
					{
						break;
					}

					float density = calculateDensity(begin_pos);
					if (density > 0.0f)
					{
						total_density += density * step_thickness;

						float light_total_density = 0;
						float3 light_pos = begin_pos;
						for (int i = 0; i < 6; i++)
						{
							light_pos += lightDir * step_thickness;
							float light_density = calculateDensity(light_pos);
							light_total_density += max(0.0, light_density * step_thickness);
						}

						float light_transmission = bear(light_total_density * _LightAbsorption);
						float shadow = _DarknessThreshold + light_transmission * (1.0 - _DarknessThreshold);
						final_light += density * step_thickness * transmittance * shadow;
						transmittance *= bear(density * step_thickness * _CloudAbsorption);
					}

					if (transmittance <= 0.1)
					{
						break;
					}

					total_thickness += step_thickness;
					begin_pos += step_dir_length;
				}

				float trnsmission = bear(total_density * _CloudAbsorption);
				return float3(final_light, trnsmission, transmittance);
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

				float3 col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

				float forwardScattering = saturate(dot(ray_dir, light_dir));//abs!!

				float3 col = tex2D(_ScreenTex, i.uv);
				float3 cloud_col = lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, col_params.x * _Brightness);

				col = col * col_params.y
					+ (1 - col_params.y) * cloud_col * _LightColor0
					+ (1 - col_params.y) * forwardScattering * _ForwardScatteringScale;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}