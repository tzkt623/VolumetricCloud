Shader "Tezcat/CloudBox"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

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

			//----------------------------
			//
			// Base Infor
			//
			sampler2D _ScreenTex;
			sampler2D _CameraDepthTexture;

			//----------------------------
			//
			// Textures
			//
			Texture3D<float4> _ShapeTex3D;
			SamplerState sampler_ShapeTex3D;

			Texture3D<float4> _DetailTex3D;
			SamplerState sampler_DetailTex3D;

			Texture2D _WeatherTex2D;
			SamplerState sampler_WeatherTex2D;

			Texture2D _BlueNoiseTex2D;
			SamplerState sampler_BlueNoiseTex2D;


			//----------------------------
			//
			// Data
			//
			float3 _BoxMin;
			float3 _BoxMax;


			//----------------------------
			//
			// Shape
			//
			int _StepCount;
			float _CloudScale;
			float3 _CloudOffset;
			float _BlueNoiseIntensity;
			float3 _ShapeSpeedScale;
			float _ShapeDensityStrength;
			float _DensityThreshold;

			//----------------------------
			//
			// Detail
			//
			float3 _DetailSpeedScale;
			float _DetailDensityStrength;

			//----------------------------
			//
			// Light
			//

			float _DarknessThreshold;
			float _LightAbsorption;
			float3 _EnergyParams;
			float4 _CloudColor;
			float4 _CloudColorLight;
			float4 _CloudColorBlack;
			float2 _CloudColorOffset;
			float _CloudAbsorption;


			//----------------------------
			//
			// Function
			//
			float mix(float v1, float v2, float t)
			{
				return v1 * (1.0f - t) + v2 * t;
			}

			//0.5,0.6,1,0,1
			//0.0+(((0.5-0.6)/(1.0-0.6)) * (1.0-0.0)) = -0.25??
			float remap(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
			{
				return newMin + (((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
			}

			//cosAngle = dot(lightDir, viewDir)
			float hgFunc(float g, float cosAngle)
			{
				float g2 = g * g;
				float v = 1.0f + g2 - 2.0f * g * cosAngle;
				return (1.0f - g2) / ((4.0f * 3.141592653f) * pow(v, 1.5f));
			}

			//HorizonZeroDawn
			float phaseHZ(float cosAngle)
			{
				float v1 = hgFunc(_EnergyParams.x, cosAngle);
				float v2 = _EnergyParams.y * hgFunc(0.99 - _EnergyParams.z, cosAngle);

				return max(v1, v2);
			}

			//http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
			float phasePBRBook(float cosAngle)
			{
				float v1 = hgFunc(_EnergyParams.x, cosAngle);
				float v2 = hgFunc(_EnergyParams.y, cosAngle);
				return mix(v1, v2, _EnergyParams.z);
			}

			float phaseFunc(float cosAngle)
			{
				return phaseHZ(cosAngle);
			}

			float powderEffect(float value)
			{
				return 1.0f - exp(-value * 2.0f);
			}

			//HZ Funcion
			float bearPower(float value)
			{
				float bear_law = exp(-value);
				float powder_sugar_effect = 1.0f - exp(-value * 2.0f);
				return bear_law * powder_sugar_effect * 2.0f;
			}

			float bear(float value)
			{
				return exp(-value);
			}

			float2 squareUV(float2 uv)
			{
				float width = _ScreenParams.x;
				float height = _ScreenParams.y;
				//float minDim = min(width, height);
				float scale = 1000;
				float x = uv.x * width;
				float y = uv.y * height;
				return float2 (x / scale, y / scale);
			}

			float2 rayBoxDst(float3 boxMin, float3 boxMax, float3 pos, float3 rayDir)
			{
				float3 t0 = (boxMin - pos) / rayDir;
				float3 t1 = (boxMax - pos) / rayDir;

				float3 tmin = min(t0, t1);
				float3 tmax = max(t0, t1);

				//射线到box两个相交点的距离, dstA最近距离， dstB最远距离
				float dstA = max(max(tmin.x, tmin.y), tmin.z);
				float dstB = min(min(tmax.x, tmax.y), tmax.z);

				float dstToBox = max(0, dstA);
				float dstInBox = max(0, dstB - dstToBox);

				return float2(dstToBox, dstInBox);
			}

			float getCloudDatas(float height)
			{
				float stratus = max(0.0, remap(height, 0.0, 0.1, 0.0, 1.0) * remap(height, 0.2, 0.3, 1.0, 0.0));
				float stratocumulus = max(0.0, remap(height, 0.0, 0.25, 0.0, 1.0) * remap(height, 0.3, 0.65, 1.0, 0.0));
				float cumulus = max(0.0, remap(height, 0.01, 0.3, 0.0, 1.0) * remap(height, 0.6, 0.95, 1.0, 0.0));

				float a = lerp(stratus, stratocumulus, clamp(height * 2.0, 0.0, 1.0));
				float b = lerp(stratocumulus, cumulus, clamp((height - 0.5) * 2.0, 0.0, 1.0));

				return lerp(a, b, height);
			}

			float getHeightFractionForPoint(float3 inPosition, float2 inCloudMinMax)
			{
				float height_fraction = (inPosition.y - inCloudMinMax.x) / (inCloudMinMax.y - inCloudMinMax.x);
				return saturate(height_fraction);
			}

			//这里返回的是一个基础密度计数,此计数越高表明当前点的密度越大
			float calculateBaseShape(in float3 uvw)
			{
				float time = _Time.x;

				float4 shape = _ShapeTex3D.SampleLevel(sampler_ShapeTex3D, uvw + float3(time, sin(time), -time) * _ShapeSpeedScale, 0);
				float low_freq_fbm = dot(shape.bga, float3(0.625f, 0.25f, 0.125f));
				float base_shape = remap(shape.r, -(1 - low_freq_fbm), 1.0, 0.0, 1.0);

				return base_shape;
			}

			float calculateDensity(float3 target, float height)
			{
				float cloud_type = getCloudDatas(height);

				float3 uvw = (target * _CloudScale * 0.01f + _CloudOffset * 0.01f);
				float base_shape = calculateBaseShape(uvw);
				float3 base_shape_with_cloud_types = cloud_type * base_shape;

				float3 x_range = float3(_BoxMin.x, _BoxMax.x, _BoxMax.x - _BoxMin.x);
				float3 z_range = float3(_BoxMin.z, _BoxMax.z, _BoxMax.z - _BoxMin.z);
				float3 y_range = float3(_BoxMin.y, _BoxMax.y, _BoxMax.y - _BoxMin.y);
				float edge_rate = 0.5f;
				float edge_length_x = x_range.z * edge_rate;
				float edge_length_z = z_range.z * edge_rate;

				float rate_x = remap(target.x, x_range.x, x_range.x + edge_length_x, 0.0, 1.0) * remap(target.x, x_range.y - edge_length_x, x_range.y, 1.0, 0.0);
				float rate_z = remap(target.z, z_range.x, z_range.x + edge_length_z, 0.0, 1.0) * remap(target.z, z_range.y - edge_length_z, z_range.y, 1.0, 0.0);
				float rate_y = remap(target.y, y_range.x, y_range.y, 0.0, 0.5);

				rate_x = saturate(rate_x);
				rate_z = saturate(rate_z);
				rate_y = saturate(rate_y);

				float time = _Time.x;

				float4 weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uvw.xz, 0);
				float coverage = weather.r;
				//float base_shape_with_coverage = remap(base_shape_with_cloud_types, coverage, 0.0f, 1.0f, 1.0f);
				//base_shape_with_coverage *= coverage;


				//detail
				float3 detail = _DetailTex3D.SampleLevel(sampler_DetailTex3D, uvw + float3(time * .4, -time, time * 0.1) * _DetailSpeedScale, 0) * (1 / _DetailDensityStrength);
				float high_freq_fbm = dot(detail.rgb, float3(0.625f, 0.25f, 0.125f));

				//final
				float final;
				final = remap(base_shape, high_freq_fbm - 1, 1.0f, 0.0f, 1.0f);

				final = max(0, final - _DensityThreshold) * _ShapeDensityStrength * rate_x * rate_z * rate_y;
				return final;
			}

			float calulateLightEnergy(in float density, in float height, in float phaseValue, float step_size)
			{
				float absorption_scattering = max(exp(-density), exp(-density * 0.25) * 0.7);

				float depth_probability = lerp(0.05 + pow(density, remap(height, 0.3, 0.85, 0.5, 2.0)),1.0, saturate(density / step_size));
				float vertical_probability = pow(remap(height, 0.07, 0.14, 0.1, 1.0), 0.8);
				float in_scatter_probability = depth_probability * vertical_probability;

				return absorption_scattering * in_scatter_probability * phaseValue;
			}

			float calulateLightEnergy(in float density, in float phaseValue)
			{
				float temp_density = density * _LightAbsorption;

				float absorption = max(exp(-temp_density), exp(-temp_density * 0.25) * 0.7);
				float in_scattering = powderEffect(temp_density * 0.5);

				float energy = absorption;// *phaseValue * 100;// bearPower(density * _LightAbsorption);
				return _DarknessThreshold + energy * (1 - _DarknessThreshold);
			}

			float calculateLightTransmittance(in float3 pos, in float3 lightDir, in float height)
			{
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


				float2 box_info = rayBoxDst(_BoxMin, _BoxMax, pos, lightDir);
				float dst_inside_box = box_info.y;
				int step = 5;
				float step_size = dst_inside_box / (float)step;
				float total_density = 0;

				// 生成圆锥信息
				float3 light_step = step_size * lightDir;
				float cone_spread_multiplier = length(light_step);

				float coneRadius = 1.0;
				float coneStep = 1.0 / 6;


				for (int i = 0; i <= step; i++)
				{
					float3 p = pos + (lightDir * step_size * i);
					//float3 p = pos + coneRadius * (cone_spread_multiplier * noise_kernel[i] * float(i));
					float density = calculateDensity(p, height);
					total_density += max(0, density * step_size);

					coneRadius += coneStep;
				}

				return total_density;
			}

			//return xyz=LightEnergy w=Transmittance
			float4 calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				float2 box_info = rayBoxDst(_BoxMin, _BoxMax, rayOrg, rayDir);
				float dst_to_box = box_info.x;
				float dst_inside_box = box_info.y;
				if (dst_inside_box <= 0)
				{
					return float4(0.0f, 0.0f, 0.0f, 1.0f);
				}

				int step = _StepCount;
				float step_size = dst_inside_box / (float)step;
				//step_size = 11;

				float transmittance = 1;
				float3 light_total_energy = 0;

				float cos_angle = dot(rayDir, lightDir);
				float phase = phaseFunc(cos_angle);

				float random_offset = 1;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}


				for (int i = 0; i <= step; i++)
				{
					float d = dst_to_box + step_size * i * random_offset;
					if (d > depth)
					{
						break;
					}

					float3 p = rayOrg + rayDir * d;

					float height = getHeightFractionForPoint(p, float2(_BoxMin.y, _BoxMax.y));
					float step_density = calculateDensity(p, height);
					if (step_density > 0.0f)
					{
						float light_density = calculateLightTransmittance(p, lightDir, height);
						light_total_energy += step_size * step_density * transmittance * calulateLightEnergy(light_density, phase);// *phase;
						//light_total_energy += transmittance * calulateLightEnergy(light_density, height, phase, step_size);

						transmittance *= bear(step_density * step_size * _CloudAbsorption);

						if (transmittance < 0.01f)
						{
							break;
						}
					}
				}

				return float4(light_total_energy, transmittance);
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				float3 ndc_pos = float3(v.uv * 2 - 1, 0);
				float4 clip_pos = float4(ndc_pos, -1);
				float3 view_pos = mul(unity_CameraInvProjection, clip_pos).xyz;
				o.viewDir = mul(unity_CameraToWorld, float4(view_pos, 0.0f));
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 ray_o = _WorldSpaceCameraPos;
				float3 ray_dir = normalize(i.viewDir);
				//_WorldSpaceLightPos0.xyz是指向光源的方向
				float3 light_dir = normalize(_WorldSpaceLightPos0.xyz);

				float depth_in_buffer = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv).r;
				float linear_eye_depth = LinearEyeDepth(depth_in_buffer);

				float4 col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

				float forwardScattering = saturate(dot(ray_dir, light_dir));
				forwardScattering = pow(forwardScattering, 2);


				float3 col = tex2D(_ScreenTex, i.uv);

				float3 darkColor = lerp(1, _CloudColorBlack.rgb, pow(1 - col_params.xyz, _CloudColorBlack.a * 10));
				float3 cloud_col = col_params.xyz * _LightColor0;
				//cloud_col = lerp(0, 1, col_params.xyz);
				col = col * col_params.w + cloud_col;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}
