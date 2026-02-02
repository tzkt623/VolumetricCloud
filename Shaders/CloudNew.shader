Shader "Tezcat/CloudNew"
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

            Texture3D<float4> _PerlinWorleyNoiseTex3D;
            SamplerState sampler_PerlinWorleyNoiseTex3D;

            Texture3D<float4> _DetailNoiseTex3D;
            SamplerState sampler_DetailNoiseTex3D;

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

            bool impactBox(float3 pos)
            {
                if(pos.x < _BoxMin.x || pos.y < _BoxMin.y || pos.z < _BoxMin.z
                || pos.x > _BoxMax.x || pos.y > _BoxMax.y || pos.z > _BoxMax.z)
                {
                    return false;
                }

                return true;
            }

float applyErosion(float baseDensity, float detailNoise, float strength)
{
    // 关键：用细节噪声创建一个局部阈值，低于此阈值的基础密度被“挖空”
	float erodedDensity = remap(baseDensity, detailNoise * strength, 1.0, 0.0, 1.0);
    // 将负值（被完全侵蚀的部分）钳制为0
	erodedDensity = max(0.0, erodedDensity);
	return erodedDensity;
}

float applyErosion(float baseDensity, float detailNoise, float strength, float heightRate)
{
    // 关键：用细节噪声创建一个局部阈值，低于此阈值的基础密度被“挖空”
	float erodedDensity = remap(baseDensity, detailNoise * strength * lerp(0.3f, 1.0f, heightRate), 1.0, 0.0, 1.0);
    // 将负值（被完全侵蚀的部分）钳制为0
	erodedDensity = max(0.0, erodedDensity);
	return erodedDensity;
}

            float sampleDetail(in float3 pos, in float hightRate)
            {
	            float s = lerp(1.0, 2.0, hightRate);
	            float4 detail = _DetailNoiseTex3D.SampleLevel(sampler_DetailNoiseTex3D, pos * _DetailScale, 0);
				float high_freq_fbm = dot(detail.rgb, FBM_FACTOR);
	            high_freq_fbm = lerp(high_freq_fbm, 1 - high_freq_fbm, saturate(hightRate * 10));
	            return high_freq_fbm;
            }

            // 《地平线：零之曙光》风格的高度密度函数
            float HorizonZDHeightDensity(float height, float3 worldPos)
            {
                // 三个主要区域：底部、核心、顶部
	            //float h = saturate((height - _CloudBase) / _CloudThickness);
	            float h = height;
    
                // 底部梯度（快速上升）
	            float bottom = 1.0 - exp(-h * 12.0);
    
                // 核心区域（平坦高密度）
	            float core = smoothstep(0.1, 0.3, h) * (1.0 - smoothstep(0.6, 0.9, h));
    
                // 顶部侵蚀（风切变效果）
	            float windEffect = sin(worldPos.x * 0.001 + _Time.y * 0.1) * 0.5 + 0.5;
	            float topErosion = 1.0 - smoothstep(0.7, 1.0, h + windEffect * 0.1);
    
                // 组合
	            return bottom * core * topErosion;
            }

            float heightGradient(float height)
            {
                // 云层高度分布
	            float bottom = smoothstep(0.05, 0.2, height);
	            float top = 1.0 - smoothstep(0.7, 1.0, height);
	            return bottom * top;
            }

float heightGradient2(float heightRate, float weather)
{
	float stratus = calcCloudType(heightRate, 0.0, 0.1, 0.2, 0.3);
	float cumulus_stratus = calcCloudType(heightRate, 0.0, 0.25, 0.3, 0.65);
	float cumulus = calcCloudType(heightRate, 0.1, 0.3, 0.6, 0.95);
    
    if (weather <= 0.5f)
    {
		return lerp(stratus, cumulus_stratus, weather * 2.0f);
	}
    else
	{
		return lerp(cumulus_stratus, cumulus, (weather - 0.5f) * 2.0f);
	}
}

float3 CalculateCloudColor(float density, float transmittance, float height01, float3 lightColor, float3 ambientColor)
{
    // 1. 计算粉糖效应 (让边缘和暗部更有层次)
    // 当密度增加时，原本的比尔定律会让颜色迅速变黑
    // 粉糖效应 (1.0 - exp(-density * 2.0)) 会在暗部增加一点“增益”
	float powderTerm = 1.0 - exp(-density * 2.0);
	float finalSunIntensiy = transmittance * powderTerm;

    // 2. 计算高度驱动的环境光遮蔽 (Ambient Occlusion)
    // 底部通常比顶部暗，我们用一个简单的线性或曲线插值
	float ambientOcclusion = lerp(0.2, 1.0, height01);
    
    // 3. 颜色合成
    // 太阳光部分：受比尔定律和粉糖效应影响
	float3 sunPart = finalSunIntensiy * lightColor;
    
    // 环境光部分：受高度遮蔽影响，并随密度增加
	float3 ambientPart = ambientOcclusion * ambientColor * density;
    
    // 4. 最终着色
    // 我们还可以加入一个基于高度的微调：底部偏蓝（散射），顶部偏白（直射）
	float3 cloudBaseColor = lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, height01);
    
	return (sunPart + ambientPart) * cloudBaseColor;
}


            float getShapeDensity(float3 pos)
            {
	            float weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, pos.xz * _WeatherScale * 0.001f, 0).r;
	            return weather;
            }

            float getWeatherData(float3 pos)
            {
	            float weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, pos.xz * _WeatherScale * 0.001f, 0).r;
	            return weather;
            }

            float sampleDensity(float3 pos, bool needSampleDetail)
            {
	            float weather = getWeatherData(pos);
	            //weather = remap(weather, _Coverage, _Coverage + 0.5f, 0.0f, 1.0f);
	            //if (weather <= 0.001f)
	            //{
		        //    return 0.0f;
	            //}
	            float normalized_height = calculateHeightRate(pos);
                float4 shape_noise = _PerlinWorleyNoiseTex3D.SampleLevel(sampler_PerlinWorleyNoiseTex3D
                                                        , pos * _ShapeScale + _CloudOffset
                                                        , 0) * _ShapeDensityStrength;
    
    
	            float transitionWidth = weather; // 过渡区宽度，可调
	            float softCoverage = smoothstep(0.0, transitionWidth, _Coverage);
	            float edgeSoftness = 0.15; // 可调节参数
	            float cloudBase = smoothstep(0.5 - edgeSoftness, 0.5 + edgeSoftness, weather);
    
	            float shape_base = shape_noise.r;
	            shape_base = max(0, remap(shape_base, 1 - _Coverage, 1.0, 0.0, 1.0));
	            //shape_base *= HorizonZDHeightDensity(normalized_height, pos * _ShapeScale);
	            shape_base *= heightGradient(normalized_height);
                if(shape_base == 0.0f)
                {
                    return 0.0f;
	            }
    
	            float shape_fbm = dot(shape_noise.gba, _ShapeMixFactor.rgb) / (_ShapeMixFactor.r + _ShapeMixFactor.g + _ShapeMixFactor.b);
	shape_base = max(0, remap(shape_base, shape_fbm - 1, 1.0, 0.0, 1.0));
	            //shape_base = max(0, remap(shape_base, (1 - weather * _Coverage), 1.0, 0.0, 1.0));
    
	            if (needSampleDetail && shape_base > 0.0f)
                {
			        float shape_detail = sampleDetail(pos, normalized_height);
			        shape_base = applyErosion(shape_base, shape_detail, _DetailDensityStrength, normalized_height);
	            }
    
	            return shape_base * _DensityScale;
            }

            float3 hardCodeColor(float transmittance, float hightRate)
            {
                //一种硬编码颜色的方法
                //transmittance越低,颜色越暗,越接近黑色,越高颜色越亮,越接近白色
                //这里只是寻找一个最简单的曲线函数来调整颜色
                //float2 rate2 = float2(saturate(pow(_CloudColorOffsetLight * transmittance, 4)), saturate(pow(_CloudColorOffsetBlack * transmittance, 4)));
                hightRate *= transmittance;

                float3 cloud_color = lerp(_CloudColorLight.rgb, _LightColor0.rgb, hightRate);
                cloud_color = lerp(_CloudColorBlack.rgb, cloud_color, hightRate);

                float3 sun_set_color = lerp(_CloudColorBlack2.rgb, _LightColor0.rgb, hightRate);
                sun_set_color = lerp(_CloudColorBlack2.rgb, sun_set_color, hightRate);

                cloud_color = lerp(cloud_color, sun_set_color, saturate(pow(_SunAngleRate, 8)));

                return cloud_color * transmittance;
            }

            float3 sampleLightEnergy(float3 pos, float3 lightDir)
            {
                //float2 data = rayBoxDst(_BoxMin, _BoxMax, pos, 1.0f / lightDir);
                float length_to_begin_pos = 0;
                float cloud_thickness = 0;
                if(!calculateCloudThickness(pos, lightDir, length_to_begin_pos, cloud_thickness))
                {
                    return 1.0f;
                }

                float total_density = 0;
	            float step_length = cloud_thickness / 4.0f;
	            //float step_length = _LightStepLength;
                
                if(cloud_thickness > 0.0f)
                {
                    for(int i = 0; i < 6; i++)
                    {
                        pos += lightDir * step_length;
			            total_density += max(0, sampleDensity(pos, true));
		            }
	            }

	            float transmittance = bear(total_density * step_length * _LightAbsorption);
                return transmittance;
            }

            float4 calculateCloud(float3 rayO, float3 rayDir, float3 lightDir, float depth)
            {
                //x : to cloud length
                //y : in cloud length
                //float2 cloud_data = rayBoxDst(_BoxMin, _BoxMax, rayO, 1.0f / rayDir);
                float length_to_begin_pos = 0;
                float cloud_thickness = 0;
                if(!calculateCloudThickness(rayO, rayDir, length_to_begin_pos, cloud_thickness))
                {
                    return float4(0.0f, 0.0f, 0.0f, 1.0f);
                }

                float3 cloud_color = float3(0.0f,0.0f,0.0f);
                float transmittance = 1.0f;
                float step_length = cloud_thickness / _StepCount;

                float phase_value = phaseFunc(dot(lightDir, -rayDir), _PhaseParams);
                float travel_thickness = length_to_begin_pos;
                //step_length *= hash33(current_pos);

                float3 current_pos = rayO + rayDir * length_to_begin_pos;

                float random_offset = 0;
				if (_BlueNoiseIntensity > 0)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, current_pos.xy, 0);
					//random_offset = frac(random_offset + float(_FrameCount % 32) * 1.61803398875) * _BlueNoiseIntensity;
					//random_offset *= _BlueNoiseIntensity;
		            current_pos += _BlueNoiseIntensity;
                    //step_length *= (1 + random_offset);
				}

                for(int j = 0; j < _StepCount; j++)
                {
                    if(travel_thickness >= depth)
                    {
                        break;
                    }

                    float density = sampleDensity(current_pos, true);
                    if(density > 0.0f)
                    {
                        float step_transmittance = exp(-density * step_length * _CloudAbsorption);
                        transmittance *= step_transmittance;
                        if(transmittance < 0.01f)
                        {
                            break;
                        }

                        float3 scattering = sampleLightEnergy(current_pos, lightDir);
                        //scattering = hardCodeColor(scattering, hight_rate);
			            cloud_color += scattering * phase_value * step_transmittance * step_length * density;
		            }

                    current_pos += rayDir * step_length;
                    travel_thickness += step_length;
                }

                return float4(cloud_color, transmittance);
            }

            float4 calculateCloud2(float3 rayO, float3 rayDir, float3 lightDir, float depth, float2 screenUV)
            {
                float length_to_begin_pos = 0;
                float cloud_thickness = 0;
                if (!calculateCloudThickness(rayO, rayDir, length_to_begin_pos, cloud_thickness))
                {
                    return float4(0.0f, 0.0f, 0.0f, 1.0f);
                }

                float3 cloud_color = float3(0.0f, 0.0f, 0.0f);
                float transmittance = 1.0f;
	            float step_length = cloud_thickness / _StepCount;
	            float travel_thickness = length_to_begin_pos;
	            float3 current_pos = rayO + rayDir * length_to_begin_pos;
	            float phase_value = phaseFunc(dot(rayDir, -lightDir), _PhaseParams);
    
	            if (_BlueNoiseIntensity > 0)
	            {
		            float3 random_offset = 0;       
		            random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, screenUV, 0).r;
		            current_pos += rayDir * (step_length * random_offset * _BlueNoiseIntensity);
	            }
    
	            for (int i = 0; i < _StepCount; i++)
	            {
		            if (travel_thickness >= depth)
		            {
			            break;
		            }
        

			            float density = sampleDensity(current_pos, true);
			            if (density > 0.0f)
			            {
				            float step_transmittance = exp(-density * step_length * _CloudAbsorption);
				            transmittance *= step_transmittance;
				            if (transmittance < 0.01f)
				            {
					            break;
				            }
            
				            float3 scattering = sampleLightEnergy(current_pos, lightDir);
				            cloud_color += scattering * phase_value * transmittance * step_length * density;
			            }
		            

		            current_pos += rayDir * step_length;
		            travel_thickness += step_length;
	            }
    
	            return float4(cloud_color, transmittance);
}

            float4 calculateUVW(float3 rayO, float3 rayDir)
            {
	            float length_to_begin_pos = 0;
	            float cloud_thickness = 0;
	            if (!calculateCloudThickness(rayO, rayDir, length_to_begin_pos, cloud_thickness))
	            {
		            return float4(0.0f, 0.0f, 0.0f, 1.0f);
	            }
    
	            float3 pos = frac((rayO + rayDir * length_to_begin_pos) * _ShapeScale);
	            //pos /= float3(10, 2, 10);

	            return float4(pos, 0.0f);
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
	            float4 screen_col = tex2D(_MainTex, i.uv);
	            float4 cloud_color = calculateCloud2(ray_o, ray_dir, light_dir, depth, i.uv);
	            float3 combine_col = screen_col.rgb * cloud_color.a + cloud_color.rgb;

	            if (ShowDebugUV == 1)
                {
		            float4 uvw = calculateUVW(ray_o, ray_dir);
		            float3 debug_color = screen_col.rgb * uvw.a + uvw.rgb * (1 - uvw.a);
		            return float4(mix(debug_color, combine_col, 0.5f), 1.0f);
	            }
                else
	            {
		            float3 c = lerp(_CloudColorBlack2.rgb, _CloudColorLight2.rgb, cloud_color.rgb);
		            combine_col = screen_col.rgb * cloud_color.a + c * (1 - cloud_color.a) * _LightColor0;
		            return float4(combine_col, 1.0f);
	            }
    
            }
            ENDCG
        }
    }
}
