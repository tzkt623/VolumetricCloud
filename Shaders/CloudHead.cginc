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

Texture2D _WeatherTex2D;
SamplerState sampler_WeatherTex2D;

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
float _EdgeLength;
float _CoverageRate;
#define SHAPE_SCALE (_ShapeScale * 0.01f)
#define DETAIL_SCALE (_DetailScale * 0.01f)
static float3 FBM_FACTOR = float3(0.625, 0.25, 0.125);

#define STRATUS_GRADIENT float4(0.0, 0.1, 0.2, 0.3)
#define STRATOCUMULUS_GRADIENT float4(0.02, 0.2, 0.48, 0.625)
#define CUMULUS_GRADIENT float4(0.00, 0.1625, 0.88, 0.98)

//-----------------------
//
//	Motion
//
float3 _ShapeSpeedScale;
float3 _DetailSpeedScale;
float3 _CloudOffset;
float3 _CloudSpeed;

//----------------------------
//
// Light
//
float _LightStepLength;
float4 _CloudColorLight;
float4 _CloudColorBlack;
float _CloudAbsorption;
float _LightAbsorption;
float _DarknessThreshold;
float _ForwardScatteringScale;
float4 _EnergyParams;
float _Brightness;

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

#define CAM_UNDER_HORIZON_LINE -1
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

float2 raySphereDst(float3 sphereCenter, float sphereRadius, float3 pos, float3 rayDir)
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
	if (viewPosition == CAM_UNDER_HORIZON_LINE)
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
	float v2 = energyParams.z * hgFunc(-energyParams.y, cosAngle);

	return max(v1, v2);
}

//http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
float phasePBRBook(in float cosAngle, in float4 energyParams)
{
	float v1 = hgFunc(energyParams.x, cosAngle);
	float v2 = hgFunc(energyParams.y, cosAngle);
	return mix(v1, v2, clamp(cosAngle * 0.5 + 0.5, 0.0, 1.0));
}

float phaseFunc(in float cosAngle, in float4 energyParams)
{
	return phaseO(cosAngle, energyParams);
}

float powderEffect(in float value)
{
	return 1.0f - exp(-value * 2.0f);
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

float calulateLightEnergy2(in float density, in float height, in float phaseValue, float step_size)
{
	float absorption_scattering = max(exp(-density), exp(-density * 0.25) * 0.7);

	float depth_probability = lerp(0.05 + pow(density, remap(height, 0.3, 0.85, 0.5, 2.0)), 1.0, saturate(density / step_size));
	float vertical_probability = pow(remap(height, 0.07, 0.14, 0.1, 1.0), 0.8);
	float in_scatter_probability = depth_probability * vertical_probability;

	return absorption_scattering * in_scatter_probability * phaseValue;
}

float calulateLightEnergy(in float density, in float phaseValue, in float lightAbsorption, in float darknessThreshold)
{
	//float temp_density = density * _LightAbsorption;

	float absorption = bearNew(density * lightAbsorption);
	float in_scattering = powderEffect(density);

	//float energy = bear(temp_density);// *phaseValue * 100;// 
	float energy = absorption;// *1.5 * in_scattering;// *phaseValue;// ;// *phaseValue;//absorption;// *in_scattering;// *phaseValue;
	return darknessThreshold + energy * (1 - darknessThreshold);
}

float calculateInScatter(in float heightRate, in float density)
{
	float depth_probability = 0.05 + pow(density, remapPositive(heightRate, 0.3, 0.85, 0.5, 2.0));
	float vertical_probability = pow(remapPositive(heightRate, 0.07, 0.14, 0.1, 1.0), 0.8);
	float in_scatter_probability = depth_probability * vertical_probability;

	return in_scatter_probability;

}

//----------------------------------------------------
//
// Cloud Shape
//
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