#ifndef Function
#define Function

float remapPositive(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
{
	return newMin + (saturate((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

//0.5,0.6,1,0,1
//0.0+(((0.5-0.6)/(1.0-0.6)) * (1.0-0.0)) = -0.25??
float remap(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
{
	return newMin + ((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin);
}

float mix(float v1, float v2, float t)
{
	return v1 * (1.0f - t) + v2 * t;
}

float2 mix(float2 v1, float2 v2, float t)
{
	return v1 * (1.0f - t) + v2 * t;
}

float3 mix(float3 v1, float3 v2, float t)
{
	return v1 * (1.0f - t) + v2 * t;
}

float4 mix(float4 v1, float4 v2, float t)
{
	return v1 * (1.0f - t) + v2 * t;
}
#endif