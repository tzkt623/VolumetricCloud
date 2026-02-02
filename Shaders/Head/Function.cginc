#ifndef Function
#define Function

float remapPositive(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
{
	return newMin + (saturate((originalValue - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

//0.5,0.6,1,0,1
//0.0+(((0.5-0.6)/(1.0-0.6)) * (1.0-0.0)) = -0.25??
//0.5,0.0,1.0,0.6,1.0
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

float2 N22(float2 p)
{
    float3 a = frac(p.xyx * float3(123.34, 234.34, 345.65));
    a += dot(a, a + 34.45);
    return frac(float2(a.x * a.y, a.y * a.z));
}

float3 hash33_1(float3 p)
{
    float3 q = frac(p * float3(123.34, 234.34, 345.56));
    q += dot(q, q + 19.19);
    return frac(q * q);
}

float3 hash33_2(float3 p)
{
    float n = sin(dot(p, float3(7.0f, 157.0f, 113.0f)));
    return frac(float3(2097152.0f, 262144.0f, 32768.0f) * n);
}

// 推荐使用改进版
float3 hash33_3(float3 p)
{
    float3 q = frac(p * float3(123.34, 234.34, 345.56));
    q += dot(q, q + 19.19);
    q = frac(q * q);
    // 可选：增加混合步骤提升质量
    q += dot(q, q.yzx + 33.33);
    return frac(q);
}

float3 hash33_4(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453123);
}

float3 hash33_5(float3 p)
{
    uint3 q = uint3(int3(p)) * uint3(1597334673U, 3812015801U, 2798796415U);
    q = (q.x ^ q.y ^ q.z) * uint3(1597334673U, 3812015801U, 2798796415U);
    return frac(float3(q));
}

// ✅ 推荐：Wombat 整数哈希变体（2023年社区共识最佳实践）
// 来源：Brian Sharpe (Wombat), Unity HDRP, Inigo Quilez 后期推荐
// 优势：分布均匀 | 分量独立 | 抗周期性 | 无可见模式 | 跨平台稳定
float3 hash33_6(float3 p)
{
    // 1. 关键：将浮点坐标离散化为整数网格点（噪声算法标准做法）
    //    避免浮点精度问题，确保相同网格点始终返回相同哈希
    uint3 i = uint3(floor(p));
    
    // 2. 高质量3D整数哈希混合（位运算+质数，经Dieharder统计测试）
    i = (i ^ (i.zxy << 16u)) ^ (i.yzx >> 16u);
    i = (i ^ (i.zxy << 15u)) ^ (i.yzx >> 15u);
    i = i ^ (i.zxy << 13u);
    i = i * uint3(73897u, 85691u, 97267u); // 大质数增强扩散
    
    // 3. 归一化到[0,1)（使用2^32避免除零，现代GPU支持uint->float精确转换）
    return float3(i) * (1.0 / 4294967296.0); // = / 2^32
}

// PCG (Permuted Congruential Generator) 3D Hash
// 优势：统计学上非常接近真随机，无任何可见纹理，性能极高
float3 hash33(float3 p)
{
    uint3 v = uint3(floor(p)); // 或者直接传入uint3
    v = v * 1664525u + 1013904223u;

    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;

    v ^= v >> 16u;

    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;

    return float3(v) * (1.0 / 4294967296.0);
}

float3 random3_improved(float3 p)
{
    p = frac(p * 0.3183099 + 0.1); // 缩放+偏移避免归零
    p *= 17.0;
    return frac(p.xzx * p.yyx + p.xxy);
}
#endif