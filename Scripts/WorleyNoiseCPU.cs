using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class WorleyNoiseCPU : WorleyNoise
    {
        Texture2D mTex2D;
        private Texture3D mTex3D;
        private Color[] mColor3D;

        protected override void init()
        {
            base.init();
            this.initData();
        }

        private void initData()
        {
            switch (mDimension)
            {
                case Dimension.TowD:
                    {
                        mTex2D = new Texture2D(mResolution, mResolution)
                        {
                            name = "WorleyNoise2D",
                            wrapMode = TextureWrapMode.Repeat,
                        };

                        this.updateColor();

                        foreach (var item in mRenderers)
                        {
                            item.material.SetInt("_Dimension", (int)mDimension);
                            item.material.SetTexture("_MainTex2D", mTex2D);
                        }
                    }
                    break;
                case Dimension.ThreeD:
                    {
                        mColor3D = new Color[mResolution * mResolution * mResolution];
                        mTex3D = new Texture3D(mResolution, mResolution, mResolution, TextureFormat.RGBA32, false)
                        {
                            name = "WorleyNoise3D",
                            wrapMode = TextureWrapMode.Repeat,
                        };

                        this.updateColor();

                        foreach (var item in mRenderers)
                        {
                            item.material.SetInt("_Dimension", (int)mDimension);
                            item.material.SetTexture("_MainTex3D", mTex3D);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void updateData()
        {
            this.updateColor();
        }

        private void updateColor()
        {
            switch (mDimension)
            {
                case Dimension.TowD:
                    {
                        this.updateColor2D();
                    }
                    break;
                case Dimension.ThreeD:
                    {
                        this.updateColor3D();
                    }
                    break;
                default:
                    break;
            }

        }

        private void updateColor3D()
        {
            for (int z = 0; z < mResolution; z++)
            {
                float pos_z = z / (float)mResolution - 0.5f;
                int z_offset = z * mResolution * mResolution;

                for (int y = 0; y < mResolution; y++)
                {
                    float pos_y = y / (float)mResolution - 0.5f;
                    int y_offset = y * mResolution;

                    for (int x = 0; x < mResolution; x++)
                    {
                        float freq = mFrequency;
                        float strength = 1.0f;
                        float sum_strength = strength;

                        float pos_x = x / (float)mResolution - 0.5f;

                        float color = this.getNoiseF
                            (pos_x * freq + mOffset.x
                            , pos_y * freq + mOffset.y
                            , pos_z * freq + mOffset.z);

                        for (int i = 1; i < mOctave; i++)
                        {
                            freq *= mLacunarity;
                            strength *= mPersistence;
                            sum_strength += strength;
                            color += this.getNoiseF
                                (pos_x * freq + mOffset.x
                                , pos_y * freq + mOffset.y
                                , pos_z * freq + mOffset.z) * strength;
                        }

                        color /= sum_strength;

                        if (mFlipWorleyNoise)
                        {
                            color = 1.0f - color;
                        }

                        mColor3D[x + y_offset + z_offset] = new Color(color, color, color, 1.0f);
                    }
                }
            }

            mTex3D.SetPixels(mColor3D);
            mTex3D.Apply();
        }

        private void updateColor2D()
        {
            for (int h = 0; h < mResolution; h++)
            {
                for (int w = 0; w < mResolution; w++)
                {
                    float freq = mFrequency;
                    float strength = 1.0f;
                    float sum_strength = strength;

                    float pos_x = w / (float)mResolution - 0.5f;
                    float pos_y = h / (float)mResolution - 0.5f;

                    float color = this.getNoiseF(pos_x * freq + mOffset.x, pos_y * freq + mOffset.y);
                    for (int i = 1; i < mOctave; i++)
                    {
                        freq *= mLacunarity;
                        strength *= mPersistence;
                        sum_strength += strength;

                        color += this.getNoiseF(pos_x * freq + mOffset.x, pos_y * freq + mOffset.y) * strength;
                    }

                    color /= sum_strength;

                    if (mFlipWorleyNoise)
                    {
                        color = 1.0f - color;
                    }

                    mTex2D.SetPixel(w, h, new Color(color, color, color, 1.0f));
                }
            }

            foreach (var item in mMarkPointArray2D)
            {
                mTex2D.SetPixel(item.x, item.y, Color.red);
            }

            mTex2D.Apply();
        }

        private float getNoise(int w, int h)
        {
            var grid = this.getGrid(w, h);
            var n9 = this.getNeighbour9(grid);
            var close = this.getCloseDistance(n9, new Vector2Int(w, h));
            return close / mGridLengthArray[0];
        }

        private float getNoiseF(float x, float y, float z)
        {
            x -= (int)x;
            y -= (int)y;
            z -= (int)z;

            if (x < 0.0f)
            {
                x = 1f + x;
            }

            if (y < 0.0f)
            {
                y = 1f + y;
            }

            if (z < 0.0f)
            {
                z = 1f + z;
            }

            var grid = this.getGridF(x, y, z);
            var n27 = this.getNeighbour27(grid);
            var close = this.getCloseDistance(n27, new Vector3Int((int)(x * mResolution), (int)(y * mResolution), (int)(z * mResolution)));

            return close / mGridLengthArray[0];
        }

        private float getNoiseF(float w, float h)
        {
            var x = w - (int)w;
            var y = h - (int)h;

            if (x < 0.0f)
            {
                x = 1f + x;
            }

            if (y < 0.0f)
            {
                y = 1f + y;
            }

            var grid = this.getGridF(x, y);
            var n9 = this.getNeighbour9(grid);
            var close = this.getCloseDistance(n9, new Vector2Int((int)(x * mResolution), (int)(y * mResolution)));
            return close / mGridLengthArray[0];
        }

        float getCloseDistance(Vector2Int[] n9, Vector2Int pos)
        {
            float result = float.MaxValue;

            foreach (var item in n9)
            {
                var distance = Vector2Int.Distance(item, pos);

                if (distance < result)
                {
                    result = distance;
                }
            }

            return result;
        }

        float getCloseDistance(Vector3Int[] n9, Vector3Int pos)
        {
            float result = float.MaxValue;

            foreach (var item in n9)
            {
                var distance = Vector3Int.Distance(item, pos);

                if (distance < result)
                {
                    result = distance;
                }
            }

            return result;
        }

        Vector2Int getGridF(float x, float y)
        {
            var rate = 1.0f / mGridCountArray[0];
            return new Vector2Int((int)(x / rate), (int)(y / rate));
        }

        Vector3Int getGridF(float x, float y, float z)
        {
            var rate = 1.0f / mGridCountArray[0];
            return new Vector3Int((int)(x / rate), (int)(y / rate), (int)(z / rate));
        }

        Vector2Int getGrid(int w, int h)
        {
            return new Vector2Int(w / mGridLengthArray[0], h / mGridLengthArray[0]);
        }

        Vector2Int[] getNeighbour9(Vector2Int currentGrid)
        {
            Vector2Int[] result = new Vector2Int[9];
            int index = 0;
            Vector2Int offset = new Vector2Int();
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int pos_x = currentGrid.x + x;
                    int pos_y = currentGrid.y + y;
                    offset.x = 0;
                    offset.y = 0;

                    if (pos_x < 0)
                    {
                        pos_x = mGridCountArray[0] - 1;
                        offset.x = -mResolution;
                    }

                    if (pos_x >= mGridCountArray[0])
                    {
                        pos_x = 0;
                        offset.x = mResolution;
                    }

                    if (pos_y < 0)
                    {
                        pos_y = mGridCountArray[0] - 1;
                        offset.y = -mResolution;
                    }

                    if (pos_y >= mGridCountArray[0])
                    {
                        pos_y = 0;
                        offset.y = mResolution;
                    }

                    result[index] = mMarkPointArray2D[pos_x + pos_y * mGridCountArray[0]] + offset;
                    index++;
                }
            }

            return result;
        }

        Vector3Int[] getNeighbour27(Vector3Int currentGrid)
        {
            Vector3Int[] result = new Vector3Int[27];
            Vector3Int offset = new Vector3Int();

            int index = 0;
            int z_rate = mGridCountArray[0] * mGridCountArray[0];
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        int pos_x = currentGrid.x + x;
                        int pos_y = currentGrid.y + y;
                        int pos_z = currentGrid.z + z;
                        offset.x = 0;
                        offset.y = 0;
                        offset.z = 0;

                        if (pos_x < 0)
                        {
                            pos_x = mGridCountArray[0] - 1;
                            offset.x = -mResolution;
                        }

                        if (pos_x >= mGridCountArray[0])
                        {
                            pos_x = 0;
                            offset.x = mResolution;
                        }

                        if (pos_y < 0)
                        {
                            pos_y = mGridCountArray[0] - 1;
                            offset.y = -mResolution;
                        }

                        if (pos_y >= mGridCountArray[0])
                        {
                            pos_y = 0;
                            offset.y = mResolution;
                        }

                        if (pos_z < 0)
                        {
                            pos_z = mGridCountArray[0] - 1;
                            offset.z = -mResolution;
                        }

                        if (pos_z >= mGridCountArray[0])
                        {
                            pos_z = 0;
                            offset.z = mResolution;
                        }

                        result[index] = mMarkPointArray3D[0][pos_x + pos_y * mGridCountArray[0] + pos_z * z_rate] + offset;
                        index++;
                    }
                }

            }

            return result;
        }
    }
}