#ifndef CloudHead
#define CloudHead

#include "Function.cginc"

//----------------------------
//
// Base Info
//
sampler2D _CameraDepthTexture;
sampler2D _MainTex;
float3 _CameraUp;
float4x4 _Matrix_Inv_VP;
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

int _FrameCount;

//----------------------------
//
// Shape
//
int _ShapeData01;
float4 _ShapeData02;
float4 _ShapeData03;
float4 _ShapeData04;

#define _StepCount _ShapeData01
#define _ShapeScale _ShapeData02.x
#define _ShapeDensityStrength _ShapeData02.y
#define _DensityThreshold _ShapeData02.z
#define _DensityScale _ShapeData02.w
#define _CloudOffset _ShapeData03.xyz
#define _CloudSpeed _ShapeData04.xyz


float4 _DetailData01;
#define _DetailScale _DetailData01.x
#define _DetailDensityStrength _DetailData01.y
#define _DetailDensityScale _DetailData01.z

float4 _ShapeMixFactor;
float _ShapeStepLength;
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
float4 _WeatherData001;
float4 _WeatherData002;

#define _WeatherScale _WeatherData001.x
#define _Coverage _WeatherData001.y
#define _WeatherOffset _WeatherData002.xy
#define _WeatherSpeed _WeatherData002.zw

//-----------------------
//
//	Motion
//
float3 _ShapeSpeedScale;
float3 _DetailSpeedScale;
float3 _WindDirection;

#define SHAPE_OFFSET (_ShapeSpeedScale * _Time.x)
#define DETAIL_OFFSET (_DetailSpeedScale * _Time.x)
#define WEATHER_OFFSET (_WeatherOffset * _Time.x)

//----------------------------
//
// Light
//
float4 _LightData01;
#define _LightAbsorption _LightData01.x
#define _LightStepLength _LightData01.y
#define _DarknessThreshold _LightData01.z

float4 _LightData02;
#define _CloudAbsorption _LightData02.x
#define _CloudColorOffsetLight _LightData02.y
#define _CloudColorOffsetBlack _LightData02.z
#define _SunAngleRate _LightData02.w

float3 _LightDir;
float4 _CloudColorLight;
float4 _CloudColorBlack;
float4 _CloudColorLight2;
float4 _CloudColorBlack2;
float _ForwardScatteringScale;
float4 _PhaseParams;
float3 _EnergyStrength;
int _MSOctave;

#define SCATTER_FORWRAD_PARAMS _PhaseParams.x
#define SCATTER_BACK_PARAMS _PhaseParams.x

#define AMBIENT_STRENGTH _EnergyStrength.x
#define SUN_STRENGTH _EnergyStrength.y
#define BRIGHTNESS _EnergyStrength.z


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


//----------------------------------
//
// Area Data
//
int _DrawAreaIndex;
float3 _BoxMin;
float3 _BoxMax;
//-1在地表里
//0在云层下
//1在云层中
//2在云层上
int _ViewPosition;
float4 _PlanetData;
float4 _CloudData;

#define _PlanetCenter _PlanetData.xyz
#define _PlanetRadius _PlanetData.w
#define _CloudMin _CloudData.x
#define _CloudMax _CloudData.y
#define _CloudThickness _CloudData.z

//----------------------------
//
// Ext
//
float4 _ExtData001;
int4 _ExtData002;
float4 _ExtData003;


//----------------------------
//
// Debug
//
int _DebugData001;

#define ShowDebugUV _DebugData001

//---------------------------------
//
// Ray Generator
//
float3 calculateViewDir(in float2 uv)
{
	float3 ndc_pos = float3(uv * 2 - 1, 1);
	float4 clip_pos = float4(ndc_pos, -1);
	float4 view_pos = mul(unity_CameraInvProjection, clip_pos);
	return mul(unity_CameraToWorld, float4(view_pos.xyz, 0.0f));
}

float3 calculateViewDir2(in float2 uv)
{
	float4 ndc_pos = float4(uv * 2.0f - 1.0f, 1.0f, 1.0f);
	//ndc_pos = mul(unity_CameraInvProjection, ndc_pos);
	//ndc_pos /= ndc_pos.w;
	//float4 world_pos = mul(unity_CameraToWorld, ndc_pos);

	float4 world_pos = mul(_Matrix_Inv_VP, ndc_pos);

	world_pos /= world_pos.w;
	world_pos.xyz = world_pos.xyz - _WorldSpaceCameraPos;
	
	return normalize(world_pos.xyz);
}

float3 calculateViewDir3(in float2 uv)
{
	float3 ndc_pos = float3(uv * 2.0f - 1.0f, 1.0f);
	float3 clip_pos = ndc_pos;
	float4 view_pos = mul(unity_CameraInvProjection, clip_pos.xyzz);
	view_pos /= view_pos.w;
	view_pos.z *= -1;
	float3 world_dir = mul(unity_CameraToWorld, float4(view_pos.xyz, 0.0f));
	
	//不用normalize,要在fragshader里面normalize
	//return world_dir;
	return normalize(world_dir);
}

void calulateRayMarchDatas(in float2 uv, in float3 viewDir
	, out float3 rayO, out float3 rayDir, out float3 lightDir, out float depth)
{
	rayO = _WorldSpaceCameraPos.xyz;
	rayDir = normalize(viewDir);
	//float ray_length = length(viewDir);
	//rayDir = viewDir / ray_length;
	//_WorldSpaceLightPos0.xyz是光源方向的反方向,也就是指向光源
    //lightDir = normalize(_WorldSpaceLightPos0.xyz);
    lightDir = _WorldSpaceLightPos0.xyz;

	float depth_in_buffer = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv).r;
	depth = LinearEyeDepth(depth_in_buffer);
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

float2 rayBoxDst(in float3 boxMin, in float3 boxMax, in float3 pos, in float3 invRayDir)
{
	float3 t0 = (boxMin - pos) * invRayDir;
	float3 t1 = (boxMax - pos) * invRayDir;

	float3 tmin = min(t0, t1);
	float3 tmax = max(t0, t1);

	//射线到box两个相交点的距离, dstA最近距离， dstB最远距离
	float dstA = max(max(tmin.x, tmin.y), tmin.z);
	float dstB = min(min(tmax.x, tmax.y), tmax.z);

	float dstToBox = max(0, dstA);
	float dstInBox = max(0, dstB - dstToBox);

	return float2(dstToBox, dstInBox);
}

//(射线发射点到球体的距离,射线穿过球体的距离)
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

//输出(到云层的距离,当前位置朝向下的云层厚度)
bool calculatePlanetCloudData(in int viewPosition
	, in float4 sphereData, in float4 inCloudData
	, in float3 rayPos, in float3 rayDir
	, out float2 outCloudData)
{
	if (viewPosition == CAM_UNDER_GROUND)
	{
		return false;
	}

    float2 cloud_min = raySphereDst(sphereData.xyz, sphereData.w + inCloudData.x, rayPos, rayDir);
    float2 cloud_max = raySphereDst(sphereData.xyz, sphereData.w + inCloudData.y, rayPos, rayDir);

	//地表上
	if (viewPosition == CAM_UNDER_CLOUD)
	{
		//如果在地表上
		//相机位置在小球和大球内
		//则到云层的距离就是cloud_min.y,即射线在小球内行进的距离
		//云层厚度就是射线在大球中行进的距离-小球中行进的距离,即cloud_max.y - cloud_min.y
        outCloudData.x = cloud_min.y;
        outCloudData.y = cloud_max.y - cloud_min.y;

		return true;
	}
	//在云层中
	else if (viewPosition == CAM_IN_CLOUD)
	{
		//beginPos = pos;

        outCloudData.x = 0;
		//如果在云层中
		//相机就是在小球上方和大球下方之间
		//云层的厚度就得看相机看向哪个位置
		//1.如果相机看向小球,那么云层厚度就是相机位置到小球面的距离,即cloud_min.x
		//2.如果相机看向大球,那么云层厚度就是相机位置在云层中的行进距离,即cloud_max.y
		//如何分辨相机看向哪个位置呢?
		//看cloud_min.y是否大于等于0,大于0说明射线在小球内部行进,说明相机看向小球,等于0说明相切
        outCloudData.y = cloud_min.y >= 0 ? cloud_min.x : cloud_max.y;

		return true;
	}
	//在云层外
	else if (viewPosition == CAM_OUT_CLOUD)
	{
		//如果相机在云层外
		//射线想要穿过云层,在云层中的行进距离必须大于0,即cloud_max.y必须大于0
		//那么到云层的距离就是射线到大球的距离,即cloud_max.x
		//云层的厚度分为两种情况
		//1.如果射线和小球相交,那么云层厚度就是射线抵达小球的距离-射线抵达大球的距离,即cloud_min.x - cloud_max.x
		//  此时cloud_min.y一定大于0
		//2.如果射线和小球不相交,那么云层厚度就是射线在大球中的行进距离,即cloud_max.y
		if (cloud_max.y > 0)
		{		
            outCloudData.x = cloud_max.x;
            outCloudData.y = cloud_min.y > 0.0 ? (cloud_min.x - cloud_max.x) : cloud_max.y;
			return true;
		}
	}

    outCloudData.x = 0;
    outCloudData.y = 0;
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

float HenyeyGreensteinPhaseFunction(float cosLV, float g)
{
    float g2 = g * g;
    return (1 - g2) / (4 * UNITY_PI * pow(1 + g2 - 2 * g * cosLV, 1.5));
}

float SchlickPhaseFunction(float cosLV, float k)
{
    float k2 = k * k;
    return (1 - k2) / (4 * UNITY_PI * pow(1 - k * cosLV, 2));
}

float phase1(float cosLV, in float4 energyParams)
{
    float frontLight = HenyeyGreensteinPhaseFunction(cosLV, energyParams.x);
    float backLight = HenyeyGreensteinPhaseFunction(cosLV, energyParams.y);
    float phase = lerp(frontLight, backLight, 0.5) * energyParams.z + energyParams.w;
    return phase;
}

float dualLobeHG(float cosTheta, in float4 energyParams)
{
    return lerp(HenyeyGreensteinPhaseFunction(cosTheta, energyParams.x)
	, HenyeyGreensteinPhaseFunction(cosTheta, energyParams.y)
	, energyParams.z) * energyParams.w;
}

float phaseFunc(in float cosAngle, in float4 energyParams)
{
    return phase1(cosAngle, energyParams);
}

float powderEffect(in float value)
{
	return 1.0f - exp(-value * 2.0f);
}

float powderEffect(in float value, in float cosAngle)
{
	float powder = 1.0 - exp(-value * 2.0);
	return lerp(1.0f, powder, (-cosAngle * 0.5f) + 0.5f);
}



//HZ Funcion
float bearPowder(float value)
{
	float bear_law = exp(-value);
	//float bear_law = max(exp(-value), exp(-value * 0.25) * 0.7);
	float powder_sugar_effect = 1.0f - exp(-value * 2.0f);
	return bear_law * powder_sugar_effect * 2.0f;
}

float bearPowder2(float value)
{
    float bear_law = exp(-value * _LightAbsorption);
	//float bear_law = max(exp(-value), exp(-value * 0.25) * 0.7);
    float powder_sugar_effect = 1.0f - exp(-value * _DetailScale);
    return bear_law * lerp(1.0, powder_sugar_effect, _DetailDensityStrength) * 2.0f;
}

float bear(float value)
{
	return exp(-value);
}

float bearNew2(float value)
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
		phase = phaseFunc(cosAngle, float4(0.3 * c, -0.1 * c, 0.3, 0.0));
		//sigmaS * bi * Li(wi) * P(wi, wo, ci*g) * exp(-ai * sigmaT * (s)ds)
		luminance += b * energy * phase * exp(-a * sigmaE * stepThickness);
        a *= 0.5;
        b *= 0.5;
		//b *= 2.5
        c *= 0.5;
    }

    return luminance;
}

float3 multipleScattering2(in float energy, in float cosAngle, in float lightDensity, in float sigmaE)
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
		phase = phaseFunc(cosAngle * a, float4(SCATTER_FORWRAD_PARAMS * c, SCATTER_BACK_PARAMS * c, 0.3, 0.0));
		//sigmaS * bi * Li(wi) * P(wi, wo, ci*g) * exp(-ai * sigmaT * (s)ds)
		luminance += b * energy * phase;// * exp(-a * lightDensity * sigmaE * 1000);
        a *= 0.9;
        b *= 0.25;
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
	float view_transmittance = bearNew(d_int);

	float light_transmittance = bearNew(lightTotalDensity);
	float shadow = _DarknessThreshold + light_transmittance * (1.0f - _DarknessThreshold);

	float3 scattering = 0;
	if(_MSOctave > 1)
	{
	 	scattering = _EnergyStrength.y * multipleScattering(light_transmittance, cosAngle, cloudDensity, stepThickness, view_transmittance);
	}
	else
	{
		scattering = _EnergyStrength.y * light_transmittance * phase;
	}

 	float3 sun_light = _LightColor0.rgb * scattering
		* powderEffect(lightTotalDensity, cosAngle) * 2
		* d_int;

 	float3 sky_light = lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, heightRate)
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
	const float sigmaS = 1;
	const float sigmaE = max(sigmaA + sigmaS, 0.0000001);

	float sampleSigmaS = sigmaS * cloudDensity;
	float sampleSigmaE = sigmaE * cloudDensity;

	float view_transmittance = bear(sampleSigmaE * stepThickness * _CloudAbsorption);
	float3 sun_lum = bear(lightTotalDensity * _LightAbsorption);
	//lum = _DarknessThreshold + lum * (1.0f - _DarknessThreshold);

	float3 scattering = 0;
	if(_MSOctave > 1)
	{
	 	scattering = multipleScattering2(sun_lum, cosAngle, lightTotalDensity, sampleSigmaE);
	}
	else
	{
		scattering = sun_lum * phase;
	}

 	sun_lum = scattering
		//* lerp(_CloudColorBlack.rgb, _CloudColorLight.rgb, scattering)
		//* powderEffect(lightTotalDensity, cosAngle) * 2
		* SUN_STRENGTH;

	float3 sky_lum = lerp(unity_AmbientGround.rgb, unity_AmbientSky.rgb, saturate(heightRate * 10)) * AMBIENT_STRENGTH;

	float3 total_lum = (sun_lum + sky_lum) * sampleSigmaS;
	total_lum = (total_lum - total_lum * view_transmittance) / sampleSigmaE;

	energy.rgb += energy.a * total_lum;
	energy.a *= view_transmittance;
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

float calculateHeightRateForSphereArea(in float3 pos, in float4 planetData, in float4 cloudData)
{
    float height = length(pos - planetData.xyz) - planetData.w - cloudData.x;
    return height / cloudData.z;
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
	cloudType = remap(cloudType, _CloudColorLight.w, 1.0, 0.0, 1.0);

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
		result = calculateHeightRateForSphereArea(pos, _PlanetData, _CloudData);
	}

	return result;
}

bool calculateCloudThickness(in float3 rayOrg, in float3 rayDir, out float dst_to_begin_pos, out float cloud_thickness)
{
	float2 cloud_area_data;

	if (_DrawAreaIndex == AREA_BOX)
	{
		cloud_area_data = rayBoxDst(_BoxMin, _BoxMax, rayOrg, 1.0f / rayDir);
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

		if (!calculatePlanetCloudData(_ViewPosition, _PlanetData, _CloudData, rayOrg, rayDir, cloud_area_data))
		{
			return false;
		}

		dst_to_begin_pos = cloud_area_data.x;
		cloud_thickness = cloud_area_data.y;
	}

	if (cloud_thickness <= 0.0f)
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
		float2 uv = (pos.xz - _BoxMin.xz) / (_BoxMax.xz - _BoxMin.xz) * _WeatherScale + WEATHER_OFFSET;
		weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv, 0).rg;
	}
	else
	{
		edge = calculateEdgeForSphereArea(heightRate);
		float2 uv;
		if (_DrawAreaIndex == AREA_HORIZON_LINE)
		{
			uv = (pos.xz * _WeatherScale / (_PlanetData.w + _CloudData.y) * 0.5 + 0.5) + WEATHER_OFFSET;
			weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv , 0).rg;
		}
		else
		{
			float3 pos_dir = normalize(pos * _WeatherScale - _PlanetData.xyz);
			float3 bottom = float3(0, -1, 0);
			float3 back = float3(-1, 0, 0);
			uv = float2(dot(pos_dir, bottom) * 0.5 + 0.5, dot(pos_dir, back) * 0.5 + 0.5) + WEATHER_OFFSET;

			weather = _WeatherTex2D.SampleLevel(sampler_WeatherTex2D, uv, 0).rg;
		}
	}
}


float calculateStepThickness(in float cloudThickness)
{
	return _ShapeStepLength > 0 ? _ShapeStepLength : cloudThickness / _StepCount;
}

float calcCloudType(float heightRate, float lowMin, float lowMax, float highMin, float highMax)
{
    return saturate(remap(heightRate, lowMin, lowMax, 0.0f, 1.0f)) * saturate(remap(heightRate, highMin, highMax, 1.0f, 0.0f));
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