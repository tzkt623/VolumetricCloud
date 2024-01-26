#ifndef CloudHead
#define CloudHead

//----------------------------
//
// Base Info
//
sampler2D _ScreenTex;
sampler2D _CameraDepthTexture;
float3 _CameraUp;

//----------------------------
//
// Cloud Texture
//
Texture3D<float4> _ShapeTex3D;
SamplerState sampler_ShapeTex3D;

Texture3D<float4> _DetailTex3D;
SamplerState sampler_DetailTex3D;

Texture2D<float4> _WeatherTex2D;
SamplerState sampler_WeatherTex2D;

Texture2D<float4> _HeightTypeTex2D;
SamplerState sampler_HeightTypeTex2D;

Texture2D _BlueNoiseTex2D;
SamplerState sampler_BlueNoiseTex2D;

//----------------------------
//
// Shape
//
float _StepCount;
float _ShapeStepLength;
float _ShapeScale;
float _DetailScale;
float _ShapeDensityStrength;
float _DetailDensityStrength;
float _DensityThreshold;
float _DensityScale;
#define SHAPE_SCALE (_ShapeScale)
#define DETAIL_SCALE (_DetailScale)
#define SHAPE_DENSITY_SCALE (_ShapeDensityStrength )
#define DETAIL_DENSITY_SCALE (_DetailDensityStrength )

static float3 FBM_FACTOR = float3(0.625, 0.25, 0.125);

#define STRATUS_GRADIENT float4(0.0, 0.1, 0.2, 0.3)
#define STRATOCUMULUS_GRADIENT float4(0.02, 0.2, 0.48, 0.625)
#define CUMULUS_GRADIENT float4(0.00, 0.1625, 0.88, 0.98)

//----------------------------
//
// Weather
//
float2 _WeatherOffset;
float _CoverageRate;
float _AnvilRate;
float _WeatherScale;

//-----------------------
//
//	Motion
//
float3 _ShapeSpeedScale;
float3 _DetailSpeedScale;
float3 _CloudOffset;
float3 _CloudSpeed;
float3 _WindDirection;

//----------------------------
//
// Light
//
float3 _LightDir;
float _LightStepLength;
float4 _CloudColorLight;
float4 _CloudColorBlack;
float _CloudAbsorption;
float _LightAbsorption;
float _DarknessThreshold;
float _ForwardScatteringScale;
float4 _PhaseParams;
float3 _EnergyStrength;
int _MSOctave;

//--------------------------
//
//	Filter
//
float _BlueNoiseIntensity;


//--------------------------------
//
//	Area
//
#define AREA_BOX 0
#define AREA_PLANET 1
#define AREA_HORIZON_LINE 2

#define CAM_UNDER_GROUND -1
#define CAM_UNDER_CLOUD 0
#define CAM_IN_CLOUD 1
#define CAM_OUT_CLOUD 2

int _DrawAreaIndex;
float3 _BoxMin;
float3 _BoxMax;
//-1在地表里
//0在云层下
//1在云层中
//2在云层上
int _ViewPosition;
float4 _PlanetData;
float2 _PlanetCloudThickness;



//---------------------------------
//
// Ray Generator
//
float3 calculateViewDir(in float2 uv)
{
	float3 ndc_pos = float3(uv * 2 - 1, 0);
	float4 clip_pos = float4(ndc_pos, -1);
	float3 view_pos = mul(unity_CameraInvProjection, clip_pos).xyz;
	return mul(unity_CameraToWorld, float4(view_pos, 0.0f));
}

void calulateRayMarchDatas(in float2 uv, in float3 viewDir
	, out float3 rayO, out float3 rayDir, out float3 lightDir, out float depth)
{
	rayO = _WorldSpaceCameraPos;
	float ray_length = length(viewDir);
	rayDir = viewDir / ray_length;
	//_WorldSpaceLightPos0.xyz是指向光源的方向
	lightDir = normalize(_WorldSpaceLightPos0.xyz);

	float depth_in_buffer = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv).r;
	depth = LinearEyeDepth(depth_in_buffer) * ray_length;
}

//--------------------------------------------------------------------
//
//	Function
//
float2 squareUV(in float2 uv)
{
	float width = _ScreenParams.x;
	float height = _ScreenParams.y;
	//float minDim = min(width, height);
	float scale = 1000;
	float x = uv.x * width;
	float y = uv.y * height;
	return float2 (x / scale, y / scale);
}

float2 rayBoxDst(in float3 boxMin, in float3 boxMax, in float3 pos, in float3 rayDir)
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

float2 raySphereDst(in float3 sphereCenter, in float sphereRadius, in float3 pos, in float3 rayDir)
{
	float3 oc = pos - sphereCenter;
	float b = dot(rayDir, oc);
	float c = dot(oc, oc) - sphereRadius * sphereRadius;
	float t = b * b - c;//t > 0有两个交点, = 0 相切， < 0 不相交

	float delta = sqrt(max(t, 0));
	float dstToSphere = max(-b - delta, 0);
	float dstInSphere = max(-b + delta - dstToSphere, 0);
	return float2(dstToSphere, dstInSphere);
}

bool calculatePlanetCloudData(in int viewPosition
	, in float4 sphereData, in float2 cloudThicknessMinMax
	, in float3 pos, in float3 rayDir
	, out float2 cloudData)
{
	if (viewPosition == CAM_UNDER_GROUND)
	{
		return false;
	}

	float2 cloud_min = raySphereDst(sphereData.xyz, sphereData.w + cloudThicknessMinMax.x, pos, rayDir);
	float2 cloud_max = raySphereDst(sphereData.xyz, sphereData.w + cloudThicknessMinMax.y, pos, rayDir);

	//地表上
	if (viewPosition == CAM_UNDER_CLOUD)
	{
		//beginPos = pos + rayDir * cloud_min.y;

		cloudData.x = cloud_min.y;
		cloudData.y = cloud_max.y - cloud_min.y;

		return true;
	}
	//在云层中
	else if (viewPosition == CAM_IN_CLOUD)
	{
		//beginPos = pos;

		cloudData.x = 0;
		cloudData.y = cloud_min.y > 0 ? cloud_min.x : cloud_max.y;

		return true;
	}
	//在云层外
	else if (viewPosition == CAM_OUT_CLOUD)
	{
		if (cloud_max.y > 0)
		{
			//beginPos = pos + rayDir * cloud_max.x;

			cloudData.x = cloud_max.x;
			cloudData.y = cloud_min.x - cloud_max.x;
			return true;
		}
	}

	cloudData = -1;
	//beginPos = 0;

	return false;
}

bool isOutOfBox(in float3 pos, in float3 boxMin, in float3 boxMax)
{
	return pos.y > boxMax.y || pos.y < boxMin.y
		|| pos.x > boxMax.x || pos.x < boxMin.x
		|| pos.z > boxMax.z || pos.z < boxMin.z;
}

//---------------------------------------------------
//
// Cloud Lighting
//
float hgFunc(in float g, in float cosAngle)
{
	float g2 = g * g;
	float v = 1.0f + g2 - 2.0f * g * cosAngle;
	return (1.0f - g2) / ((4.0f * 3.14159f) * pow(v, 1.5f));
}

float phaseO(in float cosAngle, in float4 energyParams)
{
	float blend = .5;
	float hgBlend = hgFunc(energyParams.x, cosAngle) * (1 - blend) + hgFunc(-energyParams.y, cosAngle) * blend;
	return energyParams.z + hgBlend * energyParams.w;
}

//HorizonZeroDawn
float phaseHZ(in float cosAngle, in float4 energyParams)
{
	float v1 = hgFunc(energyParams.x, cosAngle);
	float v2 = energyParams.z * hgFunc(0.99 - energyParams.y, cosAngle);

	return max(v1, v2);
}

float phaseDualLobe(in float cosAngle, in float4 energyParams)
{
	float v1 = hgFunc(energyParams.x, cosAngle);
	float v2 = hgFunc(energyParams.y, cosAngle);
	return lerp(v1, v2, energyParams.z);
}

float phaseFunc(in float cosAngle, in float4 energyParams)
{
	return phaseDualLobe(cosAngle, energyParams);
}

float powderEffect(in float value)
{
	return 1.0f - exp(-value * 2.0f);
}

float powderEffect(in float value, in float cosAngle)
{
	float powder = 1.0 - exp(-value * 2.0);
	return lerp(1.0f, powder, (cosAngle * 0.5f) + 0.5f);
}

//HZ Funcion
float bearPowder(float value)
{
	float bear_law = exp(-value);
	//float bear_law = max(exp(-value), exp(-value * 0.25) * 0.7);
	float powder_sugar_effect = 1.0f - exp(-value * 2.0f);
	return bear_law * powder_sugar_effect * 2.0f;
}

float bear(float value)
{
	return exp(-value);
}

float bearNew(float value)
{
	return max(exp(-value), exp(-value * 0.25) * 0.7);
}

float3 multipleScattering(in float energy, in float cosAngle, in float density, in float stepThickness, in float sigmaE)
{
    float3 luminance = 0.0;

    // Attenuation
    float a = 1.0;
    // Contribution
    float b = 1.0;
    // Phase attenuation
    float c = 1.0;

	float phase = 0;
	
    for(float i = 0.0; i < _MSOctave; i++)
	{
		phase = phaseFunc(cosAngle, float4(_PhaseParams.x * c, _PhaseParams.y * c, _PhaseParams.z, 0.0));
		//sigmaS * bi * Li(wi) * P(wi, wo, ci*g) * exp(-ai * sigmaT * (s)ds)
		luminance += _EnergyStrength.y * b * energy * phase * exp(-a * sigmaE * stepThickness * density);
        a *= 0.5;
        b *= 0.5;
		//b *= 2.5
        c *= 0.5;
    }

    return luminance;
}

float calulateLightEnergy2(in float density, in float height, in float phaseValue, float step_size)
{
	float absorption_scattering = max(exp(-density), exp(-density * 0.25) * 0.7);

	float depth_probability = lerp(0.05 + pow(density, remap(height, 0.3, 0.85, 0.5, 2.0)), 1.0, saturate(density / step_size));
	float vertical_probability = pow(remap(height, 0.07, 0.14, 0.1, 1.0), 0.8);
	float in_scatter_probability = depth_probability * vertical_probability;

	return absorption_scattering * in_scatter_probability * phaseValue;
}

float calculateInScatter(in float heightRate, in float density)
{
	float depth_probability = 0.05 + pow(density, remap(heightRate, 0.3, 0.85, 0.5, 2.0));
	float vertical_probability = pow(remap(heightRate, 0.07, 0.14, 0.1, 1.0), 0.8);
	float in_scatter_probability = saturate(depth_probability) * saturate(vertical_probability);

	return in_scatter_probability;
}

float calulateLightEnergyOld(in float density, in float phaseValue, in float lightAbsorption, in float darknessThreshold)
{
	//float temp_density = density * _LightAbsorption;

	float absorption = bearNew(density * lightAbsorption);
	float in_scattering = powderEffect(density);

	//float energy = bear(temp_density);// *phaseValue * 100;// 
	float energy = absorption;// *1.5 * in_scattering;// *phaseValue;// ;// *phaseValue;//absorption;// *in_scattering;// *phaseValue;
	return darknessThreshold + energy * (1 - darknessThreshold);
}

void calculateLightEnergyHZ(inout float4 energy, in float cloudDensity, in float lightTotalDensity, in float phase
	, in float heightRate, in float stepThickness, in float cosAngle)
{
	float absorption = bearNew(lightTotalDensity * _LightAbsorption);
	float in_scattering = calculateInScatter(heightRate, lightTotalDensity * _LightAbsorption);

	energy.rgb += (absorption * in_scattering * phase);
	energy.a *= bearNew(cloudDensity * stepThickness * _CloudAbsorption);
}

//from 
void calculateLightEnergyMy1(inout float4 energy, in float cloudDensity, in float lightTotalDensity, in float phase
	, in float heightRate, in float stepThickness, in float cosAngle)
{
	float d_int = cloudDensity * stepThickness;
	float view_transmittance = bear(d_int);

	float light_transmittance = bearNew(lightTotalDensity);
	float shadow = _DarknessThreshold + light_transmittance * (1.0f - _DarknessThreshold);

	float3 mult_scattering = 0;
	if(_MSOctave > 1)
	{
	 	mult_scattering = multipleScattering(light_transmittance, cosAngle, cloudDensity, stepThickness, view_transmittance);
	}
	else
	{
		mult_scattering = _EnergyStrength.y * light_transmittance * phase;
	}

 	float3 sun_light = _LightColor0.rgb * mult_scattering* _LightAbsorption
		* powderEffect(lightTotalDensity, cosAngle) * 2
		* d_int
		;

 	float3 sky_light = unity_AmbientSky.rgb * clamp(heightRate, 0.5, 1.0)
		* d_int
		* _EnergyStrength.x;
	float3 lum = sun_light + sky_light;

	energy.rgb += lum * energy.a * _EnergyStrength.z;
	energy.a *= view_transmittance;
}

//from s2016-pbs-frostbite-sky-clouds-new.pdf
void calculateLightEnergyFrostbite(inout float4 energy, in float cloudDensity, in float lightTotalDensity, in float phase
	, in float heightRate, in float stepThickness, in float cosAngle)
{
	//总消光系数σe,包含了吸收和外散射, σe = σa + σs_out
	//吸收会让光子直接消失,外散射会将当前光路上的光子散射到其他光路上去,导致当前光路光子数量减少
	//内散射系数σs_in,内散射会将其他光路上的光子散射到当前光路上来,导致光子数量增加
	//因为内散射的本质是来源于外散射,所以外散射强度跟内散射有一个f(x)的关系
	//云的反照率albedo = σs / (σa + σs)

	//float sigmaA = _CloudAbsorption;
	const float sigmaA = 0;
	float sigmaS = cloudDensity;
	float sigmaE = max(sigmaA + sigmaS, 0.0000001);

	float d_int = cloudDensity * stepThickness;
	float transmittance = bear(sigmaE * stepThickness);

	float absorption = bear(lightTotalDensity);
	float shadow = _DarknessThreshold + absorption * (1.0f - _DarknessThreshold);

	float3 mult_scattering = multipleScattering(absorption, cosAngle, cloudDensity, stepThickness, sigmaE);

 	float3 sun_light = _LightColor0.rgb * mult_scattering 
		//* powderEffect(lightTotalDensity, cosAngle) * 2
		//* d_int
		;

 	float3 sky_light = (unity_AmbientSky.rgb * heightRate)
		//* d_int
		* _EnergyStrength.x;

	float3 lum = sun_light + sky_light;
	lum = (lum - lum * transmittance) / sigmaE;

	energy.rgb += lum * energy.a * _EnergyStrength.z;
	energy.a *= transmittance;
}

void calculateLightEnergy(inout float4 energy, in float cloudDensity, in float lightTotalDensity, in float phase
	, in float heightRate, in float stepThickness, in float cosAngle)
{
	calculateLightEnergyMy1(energy, cloudDensity, lightTotalDensity, phase, heightRate, stepThickness, cosAngle);
	//calculateLightEnergyFrostbite(energy, cloudDensity, lightTotalDensity, phase, heightRate, stepThickness, cosAngle);
	//calculateLightEnergyHZ(energy, cloudDensity, lightTotalDensity, phase, heightRate, stepThickness);
}

//----------------------------------------------------
//
// Cloud Shape
//
float2 calculateHeightType(in float type, in float heightRate)
{
	return _HeightTypeTex2D.SampleLevel(sampler_HeightTypeTex2D, float2(type, heightRate), 0).rg;
}

float getHeightFractionForPoint(float3 inPosition, float2 inCloudMinMax)
{
	float height_fraction = (inPosition.y - inCloudMinMax.x) / (inCloudMinMax.y - inCloudMinMax.x);
	return saturate(height_fraction);
}

float getHeightFractionForPoint(float length, float2 inCloudMinMax)
{
	float height_fraction = (length - inCloudMinMax.x) / (inCloudMinMax.y - inCloudMinMax.x);
	return saturate(height_fraction);
}

float calculateHeightRateForSphereArea(in float3 pos, in float4 planetData, in float2 cloudThickness)
{
	float height = length(pos - planetData.xyz) - planetData.w - cloudThickness.x;
	return height / (cloudThickness.y - cloudThickness.x);
}

float calculateHeightRateForBoxArea(in float3 pos, in float3 boxMin, in float3 boxMax)
{
	return (pos.y - boxMin.y) / (boxMax.y - boxMin.y);
}

float calculateEdgeForBox(in float3 pos, in float3 boxMin, in float3 boxMax)
{
	float2 length_xz = min(pos.xz - boxMin.xz, boxMax.xz - pos.xz);
	length_xz = length_xz / (boxMax.xz - boxMin.xz);
	length_xz.x = remapPositive(length_xz.x, 0.0, 0.2, 0.0, 0.5);
	length_xz.y = remapPositive(length_xz.y, 0.0, 0.2, 0.0, 0.5);

	float length_y = (pos.y - boxMin.y) / (boxMax.y - boxMin.y);
	length_y = remapPositive(length_y, 0.5, 1.0, 0.2, 0.0);
	return length_xz.x * length_y * length_xz.y;
}

float calculateEdgeForSphereArea(in float heightRate)
{
	return remapPositive(heightRate, 0.8, 1.0, 1.0, 0.0);
}

float getCloudDatas(in float heightRate, in float cloudType)
{
	float stratus = max(0.0, remap(heightRate, 0.0, 0.1, 0.0, 1.0) * remap(heightRate, 0.2, 0.3, 1.0, 0.0));
	float stratocumulus = max(0.0, remap(heightRate, 0.0, 0.25, 0.0, 1.0) * remap(heightRate, 0.3, 0.65, 1.0, 0.0));
	float cumulus = max(0.0, remap(heightRate, 0.01, 0.3, 0.0, 1.0) * remap(heightRate, 0.6, 0.95, 1.0, 0.0));

	float a = lerp(stratus, stratocumulus, clamp(cloudType * 2.0, 0.0, 1.0));
	float b = lerp(stratocumulus, cumulus, clamp((cloudType - 0.5) * 2.0, 0.0, 1.0));

	return lerp(a, b, cloudType);
}

float calculateHeightRate(in float3 pos)
{
	float result;
	if (_DrawAreaIndex == AREA_BOX)
	{
		result = calculateHeightRateForBoxArea(pos, _BoxMin, _BoxMax);
	}
	else
	{
		result = calculateHeightRateForSphereArea(pos, _PlanetData, _PlanetCloudThickness);
	}

	return result;
}

bool calculateCloudThickness(in float3 rayOrg, in float3 rayDir, out float dst_to_begin_pos, out float cloud_thickness)
{
	float2 cloud_area_data;

	if (_DrawAreaIndex == AREA_BOX)
	{
		cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, rayOrg, rayDir);
		dst_to_begin_pos = cloud_area_data.x;
		cloud_thickness = cloud_area_data.y;
	}
	else
	{
		if (_DrawAreaIndex == AREA_HORIZON_LINE)
		{
			if (_ViewPosition == CAM_UNDER_CLOUD)
			{
				if (dot(float3(0.0, 1.0, 0.0), rayDir) < 0.0f)
				{
					return false;
				}
			}
		}

		if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _PlanetCloudThickness, rayOrg, rayDir, cloud_area_data))
		{
			return false;
		}

		dst_to_begin_pos = cloud_area_data.x;
		cloud_thickness = cloud_area_data.y;
	}

	if (cloud_thickness <= 0)
	{
		return false;
	}

	return true;
}

void calculateWeatherAndEdge(in float3 pos, in float heightRate, out float2 weather, out float edge)
{
	if (_DrawAreaIndex == AREA_BOX)
	{
		edge = calculateEdgeForBox(pos, _BoxMin, _BoxMax);
		weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, (pos.xz - _BoxMin.xz) / (_BoxMax.xz - _BoxMin.xz) * _WeatherScale, 0).rg;
	}
	else
	{
		edge = calculateEdgeForSphereArea(heightRate);
		float2 uv;
		if (_DrawAreaIndex == AREA_HORIZON_LINE)
		{
			uv = (pos.xz * _WeatherScale / (_PlanetData.w + _PlanetCloudThickness.y) * 0.5 + 0.5) + _WeatherOffset;
			weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv , 0).rg;
		}
		else
		{
			float3 pos_dir = normalize(pos * _WeatherScale - _PlanetData.xyz);
			float3 bottom = float3(0, -1, 0);
			float3 back = float3(-1, 0, 0);
			uv = float2(dot(pos_dir, bottom) * 0.5 + 0.5, dot(pos_dir, back) * 0.5 + 0.5) + _WeatherOffset;

			weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv, 0).rg;
		}
	}
}

//----------------------------------------------------
//
// Debug
//
float3 debugTransmittanceCount(int loopCount)
{
	if (loopCount == 1)
	{
		return float3(1.0, 0.0, 0.0);
	}
	else if (loopCount == 2)
	{
		return float3(0.0, 1.0, 0.0);
	}
	else if (loopCount == 3)
	{
		return float3(0.0, 0.0, 1.0);
	}
	else if (loopCount == 4)
	{
		return float3(1.0, 1.0, 0.0);
	}
	else if (loopCount == 5)
	{
		return float3(1.0, 0.0, 1.0);
	}
	else if (loopCount == 6)
	{
		return float3(0.0, 1.0, 1.0);
	}
	else if (loopCount == 7)
	{
		return float3(1.0, 1.0, 1.0);
	}

	return float3(0.0, 0.0, 0.0);
}
#endif