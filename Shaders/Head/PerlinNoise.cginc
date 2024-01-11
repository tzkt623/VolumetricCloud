#ifndef Perlin_Noise
#define Perlin_Noise

#include "Function.cginc"

StructuredBuffer<int> inPerlinHashArray;
StructuredBuffer<float3> inPerlinGradients3D;

//-----------------------------------------
//
//	My Perlin nOISE
//
float perlinSmooth(float t)
{
	return t * t * t * (t * (t * 6.f - 15.f) + 10.f);
}

float perlinDot(float3 g, float x, float y, float z)
{
	return g.x * x + g.y * y + g.z * z;
}

float perlinSelect(float a, float b, bool flag)
{
	return flag ? b : a;
}

float perlin3D(in StructuredBuffer<int> hashArray, in StructuredBuffer<float3> gradients3D
	, in float3 pos, in float frequency)
{
	const int hash_mask = 255;
	const int gradient_mask = 15;

	//pos *= frequency;
	int ix0 = floor(pos.x);
	int iy0 = floor(pos.y);
	int iz0 = floor(pos.z);

	float tx0 = pos.x - ix0;
	float tx1 = tx0 - 1.f;
	ix0 %= frequency;
	ix0 = perlinSelect(ix0, ix0 + frequency, ix0 < 0);

	float ty0 = pos.y - iy0;
	float ty1 = ty0 - 1.f;
	iy0 %= frequency;
	iy0 = perlinSelect(iy0, iy0 + frequency, iy0 < 0);

	float tz0 = pos.z - iz0;
	float tz1 = tz0 - 1.f;
	iz0 %= frequency;
	iz0 = perlinSelect(iz0, iz0 + frequency, iz0 < 0);

	ix0 &= hash_mask;//m_HashMask;
	iy0 &= hash_mask;//m_HashMask;
	iz0 &= hash_mask;//m_HashMask;

	int ix1 = (ix0 + 1) % frequency;
	int iy1 = (iy0 + 1) % frequency;
	int iz1 = (iz0 + 1) % frequency;

	int h0 = hashArray[ix0];
	int h1 = hashArray[ix1];

	int h00 = hashArray[h0 + iy0];
	int h10 = hashArray[h1 + iy0];
	int h01 = hashArray[h0 + iy1];
	int h11 = hashArray[h1 + iy1];

	float3 g000 = gradients3D[hashArray[h00 + iz0] & gradient_mask];
	float3 g100 = gradients3D[hashArray[h10 + iz0] & gradient_mask];
	float3 g010 = gradients3D[hashArray[h01 + iz0] & gradient_mask];
	float3 g110 = gradients3D[hashArray[h11 + iz0] & gradient_mask];
	float3 g001 = gradients3D[hashArray[h00 + iz1] & gradient_mask];
	float3 g101 = gradients3D[hashArray[h10 + iz1] & gradient_mask];
	float3 g011 = gradients3D[hashArray[h01 + iz1] & gradient_mask];
	float3 g111 = gradients3D[hashArray[h11 + iz1] & gradient_mask];

	float v000 = perlinDot(g000, tx0, ty0, tz0);
	float v100 = perlinDot(g100, tx1, ty0, tz0);
	float v010 = perlinDot(g010, tx0, ty1, tz0);
	float v110 = perlinDot(g110, tx1, ty1, tz0);
	float v001 = perlinDot(g001, tx0, ty0, tz1);
	float v101 = perlinDot(g101, tx1, ty0, tz1);
	float v011 = perlinDot(g011, tx0, ty1, tz1);
	float v111 = perlinDot(g111, tx1, ty1, tz1);

	float tx = perlinSmooth(tx0);
	float ty = perlinSmooth(ty0);
	float tz = perlinSmooth(tz0);

	return lerp(
		lerp(lerp(v000, v100, tx), lerp(v010, v110, tx), ty),
		lerp(lerp(v001, v101, tx), lerp(v011, v111, tx), ty),
		tz);
}

float pn3D(in float3 pos, in float freq)
{
	return perlin3D(inPerlinHashArray, inPerlinGradients3D, pos, freq);
}
#endif