Shader "Tezcat/CloudBaseLightWithIntegral"
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

			float calculateDensity(in float3 pos, in float heightRate)
			{
				float edge = 1;
				float height_rate = 0;
				float weather = 1;

				if (_DrawAreaIndex == AREA_BOX)
				{
					edge = calculateEdgeForBox(pos, _BoxMin, _BoxMax);
					//weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, (pos.xz - _BoxMin.xz) / (_BoxMax.xz - _BoxMin.xz), 0).r;
				}
				else
				{
					edge = calculateEdgeForSphereArea(height_rate);


					if (_DrawAreaIndex == AREA_HORIZON_LINE)
					{
						//weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, pos.xy, 0).r;
					}
					else
					{
						float3 pos_dir = normalize(pos - _PlanetData.xyz);
						float3 bottom = float3(0, -1, 0);
						float3 back = float3(-1, 0, 0);
						float2 uv = float2(1 - dot(pos_dir, bottom) * 0.5 + 0.5, dot(pos_dir, back) * 0.5 + 0.5);

						weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv + _Time.x * 0.1, 0).r;
					}
				}


				float4 noise = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, pos * SHAPE_SCALE + _CloudOffset, 0);
				float fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
				float shape = remap(noise.r, fbm - 1, 1.0, 0.0, 1.0);

				float final = clamp(shape - _DensityThreshold, 0.0, 1.0) * _ShapeDensityStrength * edge * weather;
				if (final > 0.0f && _DetailDensityStrength > 0.0f)
				{
					float time = _Time.x;
					//detail
					float3 detail = _DetailTex3D.SampleLevel(sampler_DetailTex3D, pos + float3(time * .4, -time, time * 0.1) * _DetailSpeedScale, 0) * _DetailDensityStrength;
					float high_freq_fbm = dot(detail.rgb, float3(0.625f, 0.25f, 0.125f));
					final = remapPositive(high_freq_fbm * 0.1f, 0.0, 1.0, final, 1.0);

					final = max(0.0, final);
					return final;
				}

				return max(0.0, final);
			}

			//return xyz=LightEnergy w=Transmittance
			float3 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				//x距离起点的距离,y内部长度
				float dst_to_begin_pos;
				float cloud_thickness;

				if (!calculateCloudThickness(rayOrg, rayDir, dst_to_begin_pos, cloud_thickness))
				{
					return float3(0.0f, 1.0f, 1.0f);
				}

				float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}

				float step_thickness = cloud_thickness / (float)_StepCount;

				float3 step_dir_length = rayDir * step_thickness;
				float total_thickness = random_offset;

				float3 begin_pos = rayOrg + rayDir * (dst_to_begin_pos +random_offset);

				float total_density = 0;
				float transmittance = 1.0;
				float final_light = 0;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _PhaseParams);

				while (total_thickness < cloud_thickness)
				{
					if (total_thickness + dst_to_begin_pos > depth)
					{
						break;
					}
					
					float height_rate = calculateHeightRate(begin_pos);
					float density = calculateDensity(begin_pos, height_rate);
					if (density > 0.0f)
					{
						total_density += density * step_thickness;

						float light_total_density = 0;
						float3 light_pos = begin_pos;
						for (int i = 0; i < 6; i++)
						{
							light_pos += lightDir * step_thickness;
							height_rate = calculateHeightRate(light_pos);
							float light_density = calculateDensity(light_pos, height_rate);
							light_total_density += max(0.0, light_density * step_thickness);
						}

						float light_transmission = bearNew(light_total_density * _LightAbsorption);
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
				float3 cloud_col = lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, col_params.x * _EnergyStrength.z);

				col = col * col_params.y
					+ (1 - col_params.y) * cloud_col * _LightColor0
					+ (1 - col_params.y) * forwardScattering * _ForwardScatteringScale;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}