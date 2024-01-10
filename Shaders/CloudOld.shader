Shader "Tezcat/CloudOld"
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
			#include "Head/Function.cginc"
			#include "Head/CloudHead.cginc"

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

			float calculateBaseShape(in float3 uvw, float lod)
			{
				float time = _Time.x;

				float4 shape = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, uvw + float3(time, sin(time), -time) * _ShapeSpeedScale, lod);
				float low_freq_fbm = dot(shape.gba, float3(0.625f, 0.25f, 0.125f));
				float base_shape = remapPositive(shape.r, low_freq_fbm - 1, 1.0, 0.0, 1.0);

				return base_shape;
			}

			float calculateDensity(float3 target, float height, float lod)
			{
				if (height < 0.0f || height > 1.0f)
				{
					return 0.0f;
				}

				float3 uvw = (target * _ShapeScale * 0.01f + _CloudOffset * 0.01f);
				float base_shape = calculateBaseShape(uvw, lod);

				float time = _Time.x;
				float cloud_type = getCloudDatas(height, height);

				float edge_x_rate = 1;
				float edge_z_rate = 1;
				float edge_y_rate = 1;
				if (_EdgeLength > 0.0f)
				{
					float edge_x_length = min(target.x - _BoxMin.x, _BoxMax.x - target.x);
					float edge_z_length = min(target.z - _BoxMin.z, _BoxMax.z - target.z);
					float edge_y_length = min(target.y - _BoxMin.y, _BoxMax.y - target.y);

					edge_x_rate = min(_EdgeLength, edge_x_length) / _EdgeLength;
					edge_z_rate = min(_EdgeLength, edge_z_length) / _EdgeLength;
					edge_y_rate = min(_EdgeLength, edge_y_length) / _EdgeLength;
				}

				float3 base_shape_with_cloud_types = base_shape * edge_x_rate * edge_z_rate * edge_y_rate;// *cloud_type;
				base_shape_with_cloud_types *= remap(base_shape_with_cloud_types, _CoverageRate, 1.0, 0.0, 1.0);

				//float3 weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uvw.xz, 0);
				//float coverage = weather.r;
				//float cloud_coverage = pow(coverage, remap(height, 0.7f, 0.8f, 1.0f, lerp(1.0, 0.5, 0.1)));
				//float base_shape_with_coverage = remapPositive(base_shape_with_cloud_types, coverage, 1.0, 0.0, 1.0);
				//base_shape_with_coverage = base_shape_with_cloud_types * coverage;


				float final = base_shape;// *remapPositive(height, 0.7, 0.8, 1.0, 0.0)* coverage;
				if (final > 0.0f && _DetailDensityStrength > 0.0f)
				{
					//detail
					float3 detail = _DetailTex3D.SampleLevel(sampler_DetailTex3D, uvw + float3(time * .4, -time, time * 0.1) * _DetailSpeedScale, lod) * _DetailDensityStrength;
					float high_freq_fbm = dot(detail.rgb, float3(0.625f, 0.25f, 0.125f));
					float high_freq_modifier = mix(high_freq_fbm, 1.0 - high_freq_fbm, clamp(height * 10, 0.0, 1.0));
					final = remapPositive(final, high_freq_modifier * 0.2, 1.0, 0.0, 1.0);

					final = max(0.0, final - _DensityThreshold) * _ShapeDensityStrength;
					return final;
				}

				return max(0.0, final - _DensityThreshold) * _ShapeDensityStrength;
			}

			float calculateLightData(in float3 pos, in float3 lightDir)
			{
				float2 cloud_area_data;
				float dst_to_begin_pos;
				float cloud_thickness;

				if (_DrawAreaIndex == AREA_BOX)
				{
					cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, pos, lightDir);
					cloud_thickness = cloud_area_data.y;
					if (cloud_thickness <= 0)
					{
						return 0;
					}
				}
				else
				{
					if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _PlanetCloudThickness, pos, lightDir, cloud_area_data))
					{
						return 0.0;
					}

					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
				}


				int step = 6;
				float step_size = cloud_thickness / step;
				float total_density = 0;

				//step_size = 10;
				//step = dst_inside_box / step_size;

				/*
				// 计算椎体的偏移，在-(1,1,1)和+(1,1,1)之间使用了六个噪声结果作为Kernel
				static float3 noise_kernel[6] =
				{
					float3(0.38051305,  0.92453449, -0.02111345),
					float3(-0.50625799, -0.03590792, -0.86163418),
					float3(-0.32509218, -0.94557439,  0.01428793),
					float3(0.09026238, -0.27376545,  0.95755165),
					float3(0.28128598,  0.42443639, -0.86065785),
					float3(-0.16852403,  0.14748697,  0.97460106)
				};

				// 生成圆锥信息
				float3 light_step = step_size * lightDir;
				float cone_spread_multiplier = length(light_step);
				float coneRadius = 1.0;
				float coneStep = 1.0 / 6;
				*/

				int zero_count = 0;

				float3 step_dir_length = lightDir * step_size;
				float3 begin_pos = pos;// +step_dir_length;
				for (int i = 0; i < step; i++)
				{
					//if (isOutOfBox(begin_pos, _BoxMin, _BoxMax))
					//{
					//	break;
					//}

					//float height = getHeightFractionForPoint(begin_pos, float2(_BoxMin.y, _BoxMax.y));
					float height = getHeightFractionForPoint(length(begin_pos - _PlanetData.xyz) - _PlanetData.w, _PlanetCloudThickness);
					float density = calculateDensity(begin_pos, height, 0);
					if (density > 0.0)
					{
						total_density += density;
						zero_count = 0;
					}
					else
					{
						zero_count++;
					}
					//begin_pos += coneRadius * (cone_spread_multiplier * noise_kernel[i] * float(i));

					begin_pos += step_dir_length;

					//if (total_density > 12.f)
					//{
					//	break;
					//}

					//coneRadius += coneStep;
				}

				return total_density;
			}

			//return xyz=LightEnergy w=Transmittance
			float4 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				//x距离起点的距离,y内部长度
				float2 cloud_area_data;
				float dst_to_begin_pos;
				float cloud_thickness;

				if (_DrawAreaIndex == AREA_BOX)
				{
					cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, rayOrg, rayDir);
					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
					if (cloud_thickness <= 0)
					{
						return float4(0.0f, 0.0f, 0.0f, 1.0f);
					}

					float max_length = length(_BoxMax - _BoxMin);
				}
				else
				{
					if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _PlanetCloudThickness, rayOrg, rayDir, cloud_area_data))
					{
						return float4(0.0f, 0.0f, 0.0f, 1.0f);
					}

					dst_to_begin_pos = cloud_area_data.x;
					cloud_thickness = cloud_area_data.y;
				}

				float transmittance = 1;
				float3 light_total_energy = 0;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _EnergyParams);

				float random_offset = 0;
				if (_BlueNoiseIntensity > 1)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}


				float step_thickness = cloud_thickness / _StepCount;
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

					//float height = getHeightFractionForPoint(begin_pos, float2(_BoxMin.y, _BoxMax.y));
					float height = getHeightFractionForPoint(length(begin_pos - _PlanetData.xyz) - _PlanetData.w, _PlanetCloudThickness);

					if (test_density > 0.0)
					{
						float step_density = calculateDensity(begin_pos, height, 0);

						if (step_density <= 0.0 && pre_density <= 0.0)
						{
							zero_count++;
						}

						if (step_density > 0.0 && zero_count < 11)
						{

							float light_data = calculateLightData(begin_pos, lightDir);
							light_total_energy += transmittance
								* calulateLightEnergy(light_data, phase, _LightAbsorption, _DarknessThreshold)
								* _CloudColorBlack.rgb;
							transmittance *= bearNew(step_thickness * step_density * _CloudAbsorption);
							begin_pos += step_dir_length;
							total_thickness += step_thickness;

							//light_total_energy = float3(1.0, 0.0, 0.0);


							if (transmittance < 0.01f)
							{
								break;
							}
						}
						else
						{
							zero_count = 0;
							test_density = 0;
						}

						pre_density = step_density;
					}
					else
					{
						test_density = calculateDensity(begin_pos, height, 0);
						if (test_density <= 0.0)
						{
							total_thickness += 2 * step_thickness;
							begin_pos += 2 * step_dir_length;
						}
						else
						{
							total_thickness -= step_thickness;
							begin_pos -= step_dir_length;
						}
					}
				}

				//light_total_energy.z = total_density;
				//light_total_energy = debugTransmittanceCount(loop_count);

				//return float4(total_density, total_density, total_density, transmittance);
				return float4(light_total_energy, transmittance);
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

				float4 col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

				float forwardScattering = saturate(dot(ray_dir, light_dir));//abs!!
				//forwardScattering = pow(forwardScattering, 0.5);

				float3 col = tex2D(_ScreenTex, i.uv);
				float3 cloud_col = _LightColor0 * col_params.xyz
					+ col_params.xyz * UNITY_LIGHTMODEL_AMBIENT.xyz
					+ col_params.xyz * forwardScattering * _ForwardScatteringScale;
				//+ col_params.xyz * col;

				//if (col_params.z > 0.99)
				//{
				//	cloud_col.z = 1;
				//}

				//float3 col_l = lerp(_CloudColorLight, _LightColor0, col_params.x);
				//float3 col_d = lerp(_CloudColorBlack, _CloudColorLight, col_params.x);
				//cloud_col = lerp(col_d, col_l, col_params.x) * (1 - col_params.w);

				col = col * col_params.w + cloud_col;
				//col = cloud_col;

				return float4(col, 1);
			}
			ENDCG
		}	
	}
}
