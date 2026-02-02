using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace tezcat.Framework.Exp
{
    public class NewCloudNoise : MonoBehaviour
    {
        [Header("Shape")]
        public PerlinNoise3D mPerlinNoise;
        public WorleyNoise2 mWorleyNoise;
        public WorleyNoise2 mDetailNoise;

        [Header("Weather")]
        public WeatherMap mWeatherNoise;

        [Header("Cloud")]
        public CloudNoise mCloudNoise;

        [Header("Viewer")]
        public MeshRenderer mPerlinWorleyNoiseViewer;
        public MeshRenderer mPerlinNoiseViewer;
        public MeshRenderer[] mWorleyNoiseViewer;
        public MeshRenderer[] mDetailNoiseViewer;
        public MeshRenderer mWeatherNoiseViewer;

        bool mRefresh = false;

        private float clamp01(float v)
        {
            if (v < 0.0f) return 0.0f;
            if (v > 1.0f) return 1.0f;
            return v;
        }

        private float remap(float v, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ((v - oldMin) / (oldMax - oldMin)) * (newMax - newMin) + newMin;
        }

        public void initGPU()
        {
            PerlineNoiseSharedData.initSharedData();

            mPerlinNoise.init();
            mWorleyNoise.init();
            mDetailNoise.init();
            mCloudNoise.init(mPerlinNoise, mWorleyNoise, mDetailNoise, mWeatherNoise);

            for (int i = 0; i < mWorleyNoiseViewer.Length; i++)
            {
                mWorleyNoiseViewer[i].material.SetTexture("_WorleyNoiseTex3D", mWorleyNoise.texture);
                mWorleyNoiseViewer[i].material.SetInt("_Channel", i);
            }

            for (int i = 0; i < mDetailNoiseViewer.Length; i++)
            {
                mDetailNoiseViewer[i].material.SetTexture("_WorleyNoiseTex3D", mDetailNoise.texture);
                mDetailNoiseViewer[i].material.SetInt("_Channel", i);
            }

            mPerlinNoiseViewer.material.SetTexture("_PerlinNoiseTex3D", mPerlinNoise.texture);
            mPerlinWorleyNoiseViewer.material.SetTexture("_PerlinWorleyNoiseTex3D", mCloudNoise.perlinWorleyTex3D);

            mCloudNoise.makeCloudNoise();

            mWeatherNoise.init();
            mWeatherNoiseViewer.material.SetTexture("_PerlinNoiseTex2D", mWeatherNoise.texture);
            mWeatherNoise.sendToGPU();

            Debug.Log($"Remap {clamp01(remap(1.0f, 0.0f, 0.1f, 0.0f, 1.0f)) * remap(1.0f, 0.2f, 0.3f, 1.0f, 0.0f)}");
        }

        void Start()
        {
            //this.initGPU();
            //this.initCPU();
        }

        // Update is called once per frame
        void Update()
        {
            //             mWorleyNoise.sendToGPU();
            //             mDetailNoise.sendToGPU();
            //             mPerlinNoise.sendToGPU();
            //mCShader.SetVector("inMixactor", mixFactor);
            //mCShader.Dispatch(mKernel resolution / 8, resolution / 8, resolution / 2);

            if (mRefresh)
            {
                mRefresh = false;
                mCloudNoise.makeCloudNoise();
                mWeatherNoise.sendToGPU();
            }
        }

        /*
        private void initCPU()
        {
            mValues.Capacity = cycleCount;

            //             float[] values_1 = sampleNoise(resolution, 4);
            //             float[] values_2 = sampleNoise(resolution, 8);
            //             float[] values_3 = sampleNoise(resolution, 16);

            Color[] colors_cloud = new Color[resolution * resolution];
            Color[] perlin_cloud = new Color[resolution * resolution];
            Color[] worly_cloud = new Color[resolution * resolution];


            var c = cellCount;
            for (int i = 0; i < cycleCount; i++)
            {
                mValues.Add(this.sampleNoise(resolution, c));
                c *= 2;
            }

            float perlin_scale = 2.0f;
            float[] perlin = new float[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var v = TezNoise.sum(TezNoise.Function.Perlin2DRepeat
                        , new Vector3(x / (float)resolution * perlin_scale, y / (float)resolution * perlin_scale, 0)
                        , 8
                        , 4
                        , 2
                        , 1.0f);
                    v = (v + 1.0f) * 0.5f;
                    perlin_cloud[x + y * resolution] = new Color(v, v, v);
                    perlin[x + y * resolution] = v;
                }
            }

            for (int i = 0; i < colors_cloud.Length; i++)
            {
                float v = 0.0f;
                float factor_sum = 0.0f;
                for (int j = 0; j < mValues.Count; j++)
                {
                    v += mValues[j][i] * factors[j];
                    factor_sum += factors[j];
                    //worly_cloud[i] = new Color(v, v, v);

                    //v *= 2.0f;
                    //v = Mathf.Clamp01(v);
                }

                v /= factor_sum;
                v = perlin[i];
                colors_cloud[i] = new Color(v, v, v);
            }

            mTex2DCloud = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
            };
            mTex2DCloud.SetPixels(colors_cloud);
            mTex2DCloud.Apply();

            //cloudRenderer.material.mainTexture = mTex2DCloud;

            mTex2DWorly = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
            };
            mTex2DWorly.SetPixels(colors_cloud);
            mTex2DWorly.Apply();

            //worlyRenderer.material.mainTexture = mTex2DWorly;


            mTex2DPerlin = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
            };
            mTex2DPerlin.SetPixels(colors_cloud);
            mTex2DPerlin.Apply();

            //perlinRenderer.material.mainTexture = mTex2DPerlin;
        }

        private float[] sampleNoise(int resolution, int cellCount)
        {
            float[] values = new float[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float v = calculateNeighbor9(new Vector2Int(x, y), resolution, cellCount);
                    //Debug.Log(v);
                    values[x + y * resolution] = v;
                }
            }

            return values;
        }

        private float calculateNeighbor9(Vector2Int pos, int resolution, int cellCount)
        {
            float min_distance = float.MaxValue;
            var edge_length = resolution / cellCount;

            Vector2Int cell_index = new Vector2Int(pos.x / edge_length, pos.y / edge_length);
            Vector2Int neighbor = new Vector2Int(0, 0);

            Vector2Int mark_index = new Vector2Int(0, 0);
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    mark_index.x = 0;
                    mark_index.y = 0;

                    neighbor.x = cell_index.x + x;
                    neighbor.y = cell_index.y + y;

                    if (neighbor.x < 0)
                    {
                        neighbor.x = cellCount - 1;
                        //记录点位偏移量
                        mark_index.x = -resolution;
                    }
                    else if (neighbor.x >= cellCount)
                    {
                        neighbor.x = 0;
                        mark_index.x = resolution;
                    }

                    if (neighbor.y < 0)
                    {
                        neighbor.y = cellCount - 1;
                        mark_index.y = -resolution;
                    }
                    else if (neighbor.y >= cellCount)
                    {
                        neighbor.y = 0;
                        mark_index.y = resolution;
                    }

                    Vector2 temp_mark_pos = Hash.hash33(new Vector3(neighbor.x, neighbor.y, 0));
                    temp_mark_pos *= edge_length;

                    //把超出范围的框拉回到原本的虚拟位置贴贴
                    Vector2Int mark_pos = new Vector2Int(
                        (int)temp_mark_pos.x + neighbor.x * edge_length + mark_index.x
                        , (int)temp_mark_pos.y + neighbor.y * edge_length + mark_index.y
                        );

                    min_distance = Mathf.Min(min_distance, Vector2Int.Distance(mark_pos, pos));
                }
            }

            return 1.0f - (min_distance / edge_length);
        }
        */

        private void OnValidate()
        {
            mRefresh = true;
        }
    }
}