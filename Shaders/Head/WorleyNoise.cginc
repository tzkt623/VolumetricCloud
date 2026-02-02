#ifndef Worley_Noise
#define Worley_Noise

#include "../Head/Function.cginc"

#define FloatMax 3.402823466e+38f

//-----------------------------------------------------
//
//	2D Worley noise
//
int2 getGridF(in float2 pos, in float rate)
{
	return int2(pos.x / rate, pos.y / rate);
}

void getNeighbour9(in StructuredBuffer<int2> samplePoints, in int resolution, in int2 gridPos, out int2 result[9], in int gridCount)
{
	int index = 0;
	float2 offset = 0;

	for (int y = -1; y <= 1; y++)
	{
		for (int x = -1; x <= 1; x++)
		{
			int pos_y = gridPos.y + y;
			int pos_x = gridPos.x + x;
			offset.x = 0;
			offset.y = 0;

			if (pos_x < 0)
			{
				pos_x = gridCount - 1;
				offset.x = -resolution;
			}

			if (pos_x >= gridCount)
			{
				pos_x = 0;
				offset.x = resolution;
			}

			if (pos_y < 0)
			{
				pos_y = gridCount - 1;
				offset.y = -resolution;
			}

			if (pos_y >= gridCount)
			{
				pos_y = 0;
				offset.y = resolution;
			}

			result[index] = samplePoints[pos_x + pos_y * gridCount] + offset;
			index++;
		}
	}
}

float getCloseDistance(in int2 n9[9], in int2 pos)
{
	float result = FloatMax;

	for (int i = 0; i < 9; i++)
	{
		float dis = distance(n9[i], pos);
		if (dis < result)
		{
			result = dis;
		}
	}

	return result;
}

float getNoiseF(in StructuredBuffer<int2> samplePoints, in int resolution, float2 pos
	, in int gridCount, in int gridLength, in float gridRate)
{
	pos.x = pos.x - (int)pos.x;
	pos.y = pos.y - (int)pos.y;

	if (pos.x < 0.0f)
	{
		pos.x = 1.0f + pos.x;
	}

	if (pos.y < 0.0f)
	{
		pos.y = 1.0f + pos.y;
	}

	int2 grid_pos = getGridF(pos, gridRate);
	int2 n9[9];
	getNeighbour9(samplePoints, resolution, grid_pos, n9, gridCount);

	int2 cp = int2(pos.x * resolution, pos.y * resolution);
	return getCloseDistance(n9, cp) / gridLength;
}



//-----------------------------------------------------
//
//	3D
//
int3 getGridF(in float3 pos, in float rate)
{
	return int3(pos.x / rate, pos.y / rate, pos.z / rate);
}

void getNeighbour27(in StructuredBuffer<int3> samplePoints, in int resolution, in int3 gridPos, in int gridCount, out int3 result[27])
{
	int index = 0;
	int offset_z = gridCount * gridCount;
	float3 offset = 0;

	for (int z = -1; z <= 1; z++)
	{
		for (int y = -1; y <= 1; y++)
		{
			for (int x = -1; x <= 1; x++)
			{
				int pos_z = gridPos.z + z;
				int pos_y = gridPos.y + y;
				int pos_x = gridPos.x + x;
				offset.x = 0;
				offset.y = 0;
				offset.z = 0;

				if (pos_x < 0)
				{
					pos_x = gridCount - 1;
					offset.x = -resolution;
				}

				if (pos_x >= gridCount)
				{
					pos_x = 0;
					offset.x = resolution;
				}

				if (pos_y < 0)
				{
					pos_y = gridCount - 1;
					offset.y = -resolution;
				}

				if (pos_y >= gridCount)
				{
					pos_y = 0;
					offset.y = resolution;
				}

				if (pos_z < 0)
				{
					pos_z = gridCount - 1;
					offset.z = -resolution;
				}

				if (pos_z >= gridCount)
				{
					pos_z = 0;
					offset.z = resolution;
				}

				result[index] = samplePoints[pos_x + pos_y * gridCount + pos_z * offset_z] + offset;
				index++;
			}
		}
	}
}

float getCloseDistance(in int3 n27[27], in int3 pos)
{
	float result = FloatMax;

	for (int i = 0; i < 27; i++)
	{
		float dis = distance(n27[i], pos);
		if (dis < result)
		{
			result = dis;
		}
	}

	return result;
}

float getNoiseF(in StructuredBuffer<int3> samplePoints, in int resolution, float3 pos
	, in int gridCount, in int gridLength, in float gridRate)
{
	pos.x = pos.x - (int)pos.x;
	pos.y = pos.y - (int)pos.y;
	pos.z = pos.z - (int)pos.z;

	if (pos.x < 0.0f)
	{
		pos.x = 1.0f + pos.x;
	}

	if (pos.y < 0.0f)
	{
		pos.y = 1.0f + pos.y;
	}

	if (pos.z < 0.0f)
	{
		pos.z = 1.0f + pos.z;
	}


	int3 grid_pos = getGridF(pos, gridRate);
	int3 n27[27];
	getNeighbour27(samplePoints, resolution, grid_pos, gridCount, n27);

	int3 cp = int3(pos.x * resolution, pos.y * resolution, pos.z * resolution);

	return getCloseDistance(n27, cp) / gridLength;
}

struct FBMConfig
{
    int octaves;
    float lacunarity;
    float frequency;
    float amplitude;
    float gain;
};

float worlyNoise33(uint3 id, int resolution, int cellCount, float3 posOffset)
{
    float edge_length = resolution / cellCount;
    int3 cell_index = int3(id.x / edge_length, id.y / edge_length, id.z / edge_length);
    float3 current_pos = float3(id.x, id.y, id.z);
    float min_distance = FloatMax;

    for (int z = -1; z <= 1; z++)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                float3 offset = float3(0.0f, 0.0f, 0.0f);
                float3 neighbor = float3(cell_index.x + x, cell_index.y + y, cell_index.z + z);
                
                if (neighbor.x < 0)
                {
                    neighbor.x = cellCount - 1;
                    offset.x = -resolution;
                }
                else if (neighbor.x >= cellCount)
                {
                    neighbor.x = 0;
                    offset.x = resolution;
                }
                
                if (neighbor.y < 0)
                {
                    neighbor.y = cellCount - 1;
                    offset.y = -resolution;
                }
                else if (neighbor.y >= cellCount)
                {
                    neighbor.y = 0;
                    offset.y = resolution;
                }
                
                if (neighbor.z < 0)
                {
                    neighbor.z = cellCount - 1;
                    offset.z = -resolution;
                }
                else if (neighbor.z >= cellCount)
                {
                    neighbor.z = 0;
                    offset.z = resolution;
                }
                
                float3 mark_pos = (neighbor + hash33(neighbor + posOffset)) * edge_length;
                mark_pos += offset;
                min_distance = min(min_distance, distance(mark_pos, current_pos));
            }
        }
    }

    //return dis;
    return (min_distance / edge_length);
}

float fbmWorley(uint3 id, int resolution, int cellCount, float3 posOffset, float amplitudeFactor, int octaves)
{
    float result = 0.0f;
    float amplitude = 1.0f;
    int frequency = 1;
    float sum = 0.0f;
    for (int i = 0; i < octaves; i++)
    {
        float noise = 1 - worlyNoise33(id, resolution, cellCount * frequency, posOffset);
        //noise = pow(noise, 2.0f);
        result += noise * amplitude;
        frequency *= 2;
        sum += amplitude;
        amplitude *= amplitudeFactor;
    }
    
    return result / sum;
}

float fbmWorleyGPUPro(uint3 id, int resolution, int cellCount, float3 posOffset, int3 powFactor)
{
    float noise = pow((1 - worlyNoise33(id, resolution, cellCount, posOffset)), 2) * 0.625f
    + pow((1 - worlyNoise33(id, resolution, cellCount * 2, posOffset)), 2) * 0.25f
    + pow((1 - worlyNoise33(id, resolution, cellCount * 4, posOffset)), 2) * 0.125f;
    return noise;
}

#endif