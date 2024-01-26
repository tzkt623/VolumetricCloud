Shader "Tezcat/CloudFinal"
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

			float3 _VFog;

			float heightFunc(in float heightRate)
			{
				float result = saturate(remap(heightRate, 0.0, 0.07, 0.0, 1.0));//round bottom;
				result *= saturate(remap(heightRate, 0.1, 1.0, 1.0, 0.0));//round top

				//make anvil
				result = pow(result, saturate(remap(heightRate, 0.65, 0.95, 1, 1 - _AnvilRate * _CoverageRate)));

				return result;
			}

			float densityFunc(in float heightRate, in float density)
			{
				float result = heightRate * saturate(remap(heightRate, 0.0f, 0.2f, 0.0f, 1.0f));
				result *= saturate(remap(heightRate, 0.9, 1.0, 1.0, 0.0));

				result *= lerp(1, saturate(remap(pow(heightRate, 0.5), 0.4, 0.95, 1.0, 0.2)), _AnvilRate);

				return result * density * 2;
			}

			float calculateDetailDensity(in float3 pos, in float heightRate, in float shapeDensity, in float erosion)
			{
				float time = _Time.x;
				//detail
				float3 detail = _DetailTex3D.SampleLevel(sampler_DetailTex3D, pos * DETAIL_SCALE, 0);
				float high_freq_fbm = dot(detail.rgb, FBM_FACTOR);
				float high_freq_fbm_rate = lerp(high_freq_fbm, 1 - high_freq_fbm, saturate(heightRate * 5));

				//because detail.rgb = [0, 1], so dot(detail.rgb, FBM_FACTOR) = [0, 1]
				//because remap range is [0, 1] and high_freq_fbm_rate range is [0, 1]
				//So DETAIL_DENSITY_SCALE range must in [0, 1]
				float final = remap(shapeDensity, high_freq_fbm_rate * DETAIL_DENSITY_SCALE * (1 - erosion), 1.0, 0.0, 1.0);
				return final;
			}

			float calculateShapeDensity(in float3 pos, in float heightRate, bool sampleDetail)
			{
				float edge = 1;
				float2 weather = 1;

				calculateWeatherAndEdge(pos, heightRate, weather, edge);
				if (weather.r <= 0)
				{
					return 0;
				}

				pos = pos + _WindDirection * heightRate;

				float2 height_type = calculateHeightType(weather.g + 0.4, heightRate);

				float4 noise = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, float3(pos.x * SHAPE_SCALE, pos.y * SHAPE_SCALE/*heightRate*/, pos.z * SHAPE_SCALE) + _CloudOffset, 0)
					* height_type.r
					;
				if(noise.r <= 0)
				{
					return 0;
				}
				

				float fbm = dot(noise.gba, FBM_FACTOR);//range=[0, 1]
				float shape = saturate(remap(noise.r, fbm - 1, 1.0, 0.0, 1.0));
				//shape *= height_type.r;
				//shape *= heightFunc(heightRate);

				//shape *= heightFunc(heightRate);
				//shape *= densityFunc(heightRate);
				//shape *= getCloudDatas(heightRate, weather.g);

				float cloud_coverage = weather.r * _CoverageRate;
				float shape_with_coverage = saturate(remap(shape, 1 - cloud_coverage, 1.0, 0.0, 1.0));
				shape_with_coverage *= cloud_coverage;
				//shape_with_coverage *= densityFunc(heightRate, shape);

				//final must in range [0, 1]
				//So SHAPE_DENSITY_SCALE = [0, 1]
				float final = clamp(shape_with_coverage - _DensityThreshold, 0.0, 1.0) * SHAPE_DENSITY_SCALE;

				if (sampleDetail && final > 0.0f && _DetailDensityStrength > 0.0f)
				{
					return calculateDetailDensity(pos, heightRate, final, height_type.g) * _DensityScale;
				}

				return final * _DensityScale;
			}

			float calculateLightDensity(float3 beginPos, in float3 lightDir)
			{
				float total_density = 0;
				float density;
				for (int i = 0; i < 4; i++)
				{
					beginPos += lightDir * _LightStepLength * (i + 1);
					float height_rate = calculateHeightRate(beginPos);
					if(height_rate <= 1)
					{
						density = calculateShapeDensity(beginPos, height_rate, false);
						total_density += max(0.0, density);
					}
				}

				return saturate(total_density) * _LightStepLength;
			}

			float4 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				float dst_to_begin_pos;
				float cloud_thickness;

				if (!calculateCloudThickness(rayOrg, rayDir, dst_to_begin_pos, cloud_thickness))
				{
					return float4(0.0, 0.0, 0.0, 1.0);
				}

				float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv), 0);
					//random_offset = frac(random_offset + float(_Time.x % 32) * 1.61803398875);
					random_offset *= _BlueNoiseIntensity;
				}

				float step_thickness = _ShapeStepLength;
				//float step_thickness = cloud_thickness / _StepCount;
				float3 step_dir_length = rayDir * step_thickness;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _PhaseParams);
				//phase = max(phase, _PhaseParams.w);

				float fog_phase = hgFunc(cos_angle, _PhaseParams.y);
				int fog_step_count = 0;

				float4 final_light = float4(0.0, 0.0, 0.0, 1.0);
				int zero_count = 0;
				bool impact_cloud = false;

				float total_thickness = step_thickness * random_offset;
				float3 begin_pos = rayOrg + rayDir * dst_to_begin_pos;
				begin_pos += step_thickness * random_offset;

				//float light_step_length = cloud_thickness * 0.5f / 6;


				while (total_thickness < cloud_thickness)
				//for (int i = 0; i < _StepCount; i++)
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

							_VFog += fog_phase;
						}
						else
						{
							float light_total_density = calculateLightDensity(begin_pos, lightDir);
							calculateLightEnergy(final_light
								, density
								, light_total_density
								, phase
								, height_rate
								, step_thickness
								, cos_angle);

							//transmittance
							if (final_light.a <= 0.01f)
							{
								final_light.a = 0;
								break;
							}

							zero_count = 0;
							total_thickness += step_thickness;
							begin_pos += step_dir_length;

							_VFog += fog_phase * density;
						}

						//fog_step_count++;
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

				return float4(final_light.rgb, final_light.a);
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

				float3 col = tex2D(_ScreenTex, i.uv);
				float3 cloud_col = col_params.rgb * _LightColor0
					//+ (col_params.rgb * unity_AmbientSky.rgb)
					;

				//cloud_col = lerp(_VFog, cloud_col,  col_params.rgb);
				//cloud_col = mix(cloud_col, col, pow(_VFog, .7));

				col = col * col_params.a
					+ cloud_col
					//+ _VFog * col_params.a
					;
				//+ (1 - col_params.y) * forwardScattering * _ForwardScatteringScale;

				return float4(col, 1);
			}
		ENDCG
		}
	}
}