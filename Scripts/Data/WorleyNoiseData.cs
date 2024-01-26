using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class WorleyNoiseData
    {
        public bool mFilp;
        [Min(1.0f)]
        public float mFrequency;

        public Vector3 mOffset;

        protected int mGridLength;
        protected string mMarkName;

        public virtual void close() { }
    }

    public abstract class WorleyNoiseSingleData : WorleyNoiseData
    {
        [Min(1)]
        public int mGridCount;
    }

    [System.Serializable]
    public class WorleyNoiseData2D : WorleyNoiseSingleData
    {
        private Vector2Int[] mMarkPointArray2D;
        private ComputeBuffer mComputeBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mGridLength = resolution / mGridCount;

            mMarkPointArray2D = new Vector2Int[mGridCount * mGridCount];
            for (int y = 0; y < mGridCount; y++)
            {
                for (int x = 0; x < mGridCount; x++)
                {
                    int begin_x = x * mGridLength;
                    int begin_y = y * mGridLength;

                    var pos_x = Random.Range(begin_x, begin_x + mGridLength);
                    var pos_y = Random.Range(begin_y, begin_y + mGridLength);

                    mMarkPointArray2D[x + y * mGridCount] = new Vector2Int(pos_x, pos_y);
                }
            }

            mComputeBuffer = new ComputeBuffer(mGridCount * mGridLength, sizeof(int) * 2);
            mComputeBuffer.SetData(mMarkPointArray2D);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints", mComputeBuffer);

            shader.SetInt($"in{mMarkName}WLGridCount", mGridCount);
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public override void close()
        {
            mComputeBuffer.Release();
        }
    }

    [System.Serializable]
    public class WorleyNoiseData3D : WorleyNoiseSingleData
    {
        private ComputeBuffer mComputeBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mGridLength = resolution / mGridCount;
            var array = new Vector3Int[mGridCount * mGridCount * mGridCount];

            for (int z = 0; z < mGridCount; z++)
            {
                int begin_z = z * mGridLength;
                int z_offset = z * mGridCount * mGridCount;

                for (int y = 0; y < mGridCount; y++)
                {
                    int begin_y = y * mGridLength;
                    int y_offset = y * mGridCount;

                    for (int x = 0; x < mGridCount; x++)
                    {
                        int begin_x = x * mGridLength;

                        var pos_x = Random.Range(begin_x, begin_x + mGridLength);
                        var pos_y = Random.Range(begin_y, begin_y + mGridLength);
                        var pos_z = Random.Range(begin_z, begin_z + mGridLength);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            mComputeBuffer = new ComputeBuffer(mGridCount * mGridLength * mGridLength, sizeof(int) * 3);
            mComputeBuffer.SetData(array);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints", mComputeBuffer);

            shader.SetInt($"in{mMarkName}WLGridCount", mGridCount);

            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public override void close()
        {
            mComputeBuffer.Release();
        }
    }

    public abstract class WorleyNoiseGroupData : WorleyNoiseData
    {
        public int[] mGridCounts;
    }

    [System.Serializable]
    public class WorleyNoiseGroupData3D : WorleyNoiseGroupData
    {
        private ComputeBuffer[] mComputeBuffer;
        public ComputeBuffer mGridCountBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mComputeBuffer = new ComputeBuffer[mGridCounts.Length];

            for (int i = 0; i < mGridCounts.Length; i++)
            {
                this.calculateSamplePoint3D(i, resolution, mGridCounts[i], kernel, shader);
            }

            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);

            //Count Buffer
            mGridCountBuffer = new ComputeBuffer(mGridCounts.Length, sizeof(int));
            mGridCountBuffer.SetData(mGridCounts);

            shader.SetBuffer(kernel, $"in{mMarkName}WLGroupGridCount", mGridCountBuffer);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        protected void calculateSamplePoint3D(int index, int resolution, int gridCount, int kernel, ComputeShader shader)
        {
            var grid_length = resolution / gridCount;
            var array = new Vector3Int[gridCount * gridCount * gridCount];

            for (int z = 0; z < gridCount; z++)
            {
                int begin_z = z * grid_length;
                int z_offset = z * gridCount * gridCount;

                for (int y = 0; y < gridCount; y++)
                {
                    int begin_y = y * grid_length;
                    int y_offset = y * gridCount;

                    for (int x = 0; x < gridCount; x++)
                    {
                        int begin_x = x * grid_length;

                        var pos_x = Random.Range(begin_x, begin_x + grid_length);
                        var pos_y = Random.Range(begin_y, begin_y + grid_length);
                        var pos_z = Random.Range(begin_z, begin_z + grid_length);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            var buffer = new ComputeBuffer(gridCount * gridCount * gridCount, sizeof(int) * 3);
            buffer.SetData(array);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints3D{index}", buffer);

            mComputeBuffer[index] = buffer;
        }

        public override void close()
        {
            foreach (var item in mComputeBuffer)
            {
                item?.Release();
            }

            mGridCountBuffer?.Release();
        }
    }
}