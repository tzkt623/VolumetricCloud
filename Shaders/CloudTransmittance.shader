Shader "Tezcat/CloudTransmittance"
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

			float calculateDetailDensity(in float3 pos, in float heightRate, in float shapeDensity)
			{
				float time = _Time.x;
				//detail
				float3 detail = _DetailTex3D.SampleLevel(sampler_DetailTex3D, pos * DETAIL_SCALE, 0);
				float high_freq_fbm = dot(detail.rgb, FBM_FACTOR);
				float high_freq_fbm_rate = mix(high_freq_fbm, 1 - high_freq_fbm, clamp(heightRate, 0.0, 1.0));
				//float high_freq_fbm_rate =  high_freq_fbm;

				heightRate = remap(heightRate, 0.0, 1.0, 0.2, 1.0);
				//heightRate = clamp(heightRate, )

				float final = remap(shapeDensity, high_freq_fbm_rate * heightRate, 1.0, 0.0, 1.0);
				return max(0.0, final) * DETAIL_DENSITY_SCALE;
			}

			float calculateShapeDensity(in float3 pos, in float heightRate, bool sampleDetail)
			{
				float edge = 1;
				float weather = 1;

				if (_DrawAreaIndex == AREA_BOX)
				{
					edge = calculateEdgeForBox(pos, _BoxMin, _BoxMax);
					weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, (pos.xz - _BoxMin.xz) / (_BoxMax.xz - _BoxMin.xz), 0).r * _CoverageRate;
				}
				else
				{
					edge = calculateEdgeForSphereArea(heightRate);

					if (_DrawAreaIndex == AREA_HORIZON_LINE)
					{
						weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, (pos.xz * 100 / (_PlanetData.w + _PlanetCloudThickness.x) + 0.5), 0).r;
					}
					else
					{
						float3 pos_dir = normalize(pos - _PlanetData.xyz);
						float3 bottom = float3(0, -1, 0);
						float3 back = float3(-1, 0, 0);
						float2 uv = float2(dot(pos_dir, bottom) * 0.5 + 0.5, dot(pos_dir, back) * 0.5 + 0.5);

						weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv, 0).r;
					}
				}

				float4 noise = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, float3(pos.x * SHAPE_SCALE, heightRate, pos.z * SHAPE_SCALE) + _CloudOffset, 0);
				float fbm = dot(noise.gba, FBM_FACTOR);
				float shape = remap(noise.r, fbm - 1, 1.0, 0.0, 1.0);// *weather;
				//shape *= remap(heightRate, 0.5, 1.0, 0.5, 0.0);
				//shape *= getCloudDatas(heightRate, 1.0) / heightRate;

				float cloud_coverage = weather * _CoverageRate;
				float shape_with_coverage = remapPositive(shape, cloud_coverage, 1.0, 0.0, 1.0);
				shape_with_coverage *= cloud_coverage;

				float final = clamp(shape - _DensityThreshold, 0.0, 1.0) * SHAPE_DENSITY_SCALE;// *cloud_coverage;

				if (sampleDetail && final > 0.0f && _DetailDensityStrength > 0.0f)
				{
					return calculateDetailDensity(pos, heightRate, final);
				}

				return max(0.0, final);
			}

			float calculateLightDensity(float3 beginPos, in float3 lightDir)
			{
				float total_density = 0;
				for (int i = 0; i < 6; i++)
				{
					beginPos += lightDir * _LightStepLength;
					float height_rate = calculateHeightRate(beginPos);
					float density = calculateShapeDensity(beginPos, height_rate, true);
					total_density += max(0.0, density * _LightStepLength);
				}

				return total_density;
			}

			float2 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				float dst_to_begin_pos;
				float cloud_thickness;

				if (!calculateCloudThickness(rayOrg, rayDir, dst_to_begin_pos, cloud_thickness))
				{
					return float2(1.0, 1.0);
				}

				float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 4), 0) * _BlueNoiseIntensity;
				}

				cloud_thickness = min(cloud_thickness, _ProjectionParams.z);

				//float t = abs(dot(rayDir, float3(0, 1, 0)));
				//int stepCount = lerp(128.0f, 64.0f, t);

				float step_thickness = _ShapeStepLength;
				float3 step_dir_length = rayDir * step_thickness;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _EnergyParams);

				float transmittance = 1.0;
				float final_light = 0;
				int zero_count = 0;
				float total_density = 0;
				bool impact_cloud = false;

				float total_thickness = step_thickness + random_offset;
				float3 begin_pos = rayOrg + rayDir * (dst_to_begin_pos + random_offset);
				//begin_pos += step_dir_length;

				while (total_thickness < cloud_thickness)
					//for (int i = 0; i < stepCount; i++)
					{
						if (total_thickness + dst_to_begin_pos > depth)
						{
							break;
						}

						float height_rate = calculateHeightRate(begin_pos);
						if (impact_cloud)
						{
							float density = calculateShapeDensity(begin_pos, height_rate, true);
							//没有碰到云,大步前进
							//并转到低采样模式
							if (density <= 0.0f)
							{
								zero_count++;
								impact_cloud = false;

								total_thickness += 2 * step_thickness;
								begin_pos += 2 * step_dir_length;
							}
							else
							{
								float density_intgral = density * step_thickness;
								total_density += density_intgral;

								float light_total_density = calculateLightDensity(begin_pos, lightDir);
								float light_transmission = bearNew(light_total_density * _LightAbsorption);

								transmittance *= bear(density_intgral * _CloudAbsorption);

								//提前退出
								if (transmittance <= 0.01f)
								{
									break;
								}

								zero_count = 0;
								total_thickness += step_thickness;
								begin_pos += step_dir_length;
							}


							//pre_density = density;
						}
						else
						{
							float test_density = calculateShapeDensity(begin_pos, height_rate, false);

							if (test_density > 0.0f)
							{
								impact_cloud = true;

								//如果zero_count=0
								//说明是第一次跳进来,不用后退
								//如果>0,说明是云层采样中突然遇到空点跳转过来的
								//需要后退一步以防错过
								if (zero_count > 0)
								{
									zero_count = 0;
									total_thickness -= step_thickness;
									begin_pos -= step_dir_length;
								}
							}
							else
							{
								zero_count++;
								impact_cloud = false;

								total_thickness += 2 * step_thickness;
								begin_pos += 2 * step_dir_length;
							}
						}
					}

					float trnsmission = bear(total_density * _CloudAbsorption);
					return float2(trnsmission, transmittance);
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

					float2 col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

					float3 col = tex2D(_ScreenTex, i.uv);
					float3 cloud_col = (1 - col_params.x)
						* _Brightness 
						* _LightColor0
						;

					col = col * col_params.y
						//+ cloud_col
						;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}