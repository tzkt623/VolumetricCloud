Shader "Tezcat/CloudAmbient"
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
				float high_freq_fbm_rate = lerp(high_freq_fbm, 1 - high_freq_fbm, clamp(heightRate * 10, 0.0, 1.0));
				//float high_freq_fbm_rate =  high_freq_fbm;

				//heightRate = remap(heightRate, 0.0, 1.0, 0.2, 1.0);
				//heightRate = clamp(heightRate, )

				float final = remap(shapeDensity, high_freq_fbm_rate * heightRate, 1.0, 0.0, 1.0);
				return max(0.0, final);
			}

			float calculateShapeDensity(in float3 pos, in float heightRate, bool sampleDetail)
			{
				float edge = 1;
				float weather = 1;

				float4 noise = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, pos * SHAPE_SCALE, 0);
				float fbm = dot(noise.gba, FBM_FACTOR);
				float shape = remap(noise.r, fbm - 1, 1.0, 0.0, 1.0);// *weather;
				float final = noise.r;

				if (sampleDetail && final > 0.0f && _DetailDensityStrength > 0.0f)
				{
					final = calculateDetailDensity(pos, heightRate, final);
				}

				return max(0.0, final - _DensityThreshold) * _DensityScale;
			}

			float calculateLightEnergy(float3 beginPos, in float3 lightDir, in float cosAngle)
			{
				float total_density = 0;
				float step = _LightStepLength;
				for (int i = 0; i < 4; i++)
				{
					step = step * (i + 1);
					beginPos += lightDir * step;
					float height_rate = calculateHeightRate(beginPos);
					float density = calculateShapeDensity(beginPos, height_rate, true);
					total_density += max(0.0, density * step);
				}

				return exp(-total_density * _LightAbsorption)
					//* powderEffect(total_density, cosAngle) * 2
					;
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
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 4), 0) * _BlueNoiseIntensity;
				}

				float step_thickness = calculateStepThickness(cloud_thickness) / cloud_thickness;
				float3 step_dir_length = rayDir * step_thickness;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle, _PhaseParams);

				float transmittance = 1.0;
				float3 final_light = 0;
				float total_density = 0;

				float total_thickness = random_offset * step_thickness;
				float3 begin_pos = rayOrg + rayDir * dst_to_begin_pos;
				begin_pos += rayDir * random_offset * step_thickness;

				const float sigmaA = 0;
				const float sigmaS = 1;
				const float sigmaE = max(sigmaA + sigmaS, 0.000001);
				int select = 1;

				while (total_thickness < 1)
				//for (int i = 0; i < stepCount; i++)
				{
					if (total_thickness * cloud_thickness + dst_to_begin_pos > depth)
					{
						break;
					}
					
					float height_rate = calculateHeightRate(begin_pos);
					float density = calculateShapeDensity(begin_pos, height_rate, true);
					if(density <= 0)
					{
						total_thickness += step_thickness;
						begin_pos += step_dir_length;
						continue;
					}

					float view_transmittance = 0;
					if(select == 1)
					{
						float sampleSigmaS = sigmaS * density;
						float sampleSigmaE = sigmaE * density;
						//因为exp是一个从1开始无限趋近于0的函数
						//所以step_thickness的大小能决定透射率
						//以一个简单的范围模型来说,云的高度越高,step_thickness越大,exp的结果越小,云层透射率越低
						view_transmittance = exp(-sampleSigmaE * step_thickness * _CloudAbsorption);
						float3 sun_lum = calculateLightEnergy(begin_pos, lightDir, cos_angle);

						sun_lum	= 1//lum
							* lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, sun_lum)
							//* phase
							* SUN_STRENGTH;

						float3 sky_lum = lerp(unity_AmbientGround.rgb, unity_AmbientSky.rgb, height_rate) * AMBIENT_STRENGTH;

						float3 total_lum = (sun_lum + sky_lum) * sampleSigmaS;
						total_lum = (total_lum - total_lum * view_transmittance) / sampleSigmaE;

						final_light += transmittance * total_lum;
					}
					
					if(select == 0)
					{
						float density_intgral = density * step_thickness;
						view_transmittance = exp(-density_intgral);
						float lum = calculateLightEnergy(begin_pos, lightDir, cos_angle);

						lum = lum
							//* phase
							* lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, lum)
							* SUN_STRENGTH
							;
						
						float sky_lum = lerp(unity_AmbientGround.rgb, unity_AmbientSky.rgb, height_rate) * AMBIENT_STRENGTH;

						final_light += (lum + sky_lum) * transmittance * density_intgral;
					}

					transmittance *= view_transmittance;
					//提前退出
					if (transmittance <= 0.01f)
					{
						transmittance = 0;
						break;
					}

					total_thickness += step_thickness;
					begin_pos += step_dir_length;
				}

				return float4(final_light * BRIGHTNESS, transmittance);
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

				float3 col = tex2D(_MainTex, i.uv);
				float3 cloud_col = col_params.rgb;

				col = col * col_params.a
					+ cloud_col;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}