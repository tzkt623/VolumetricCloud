using tezcat.Framework.Utility;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class PerlinNoiseData
    {
        [Range(1, 10)]
        public int mOctave = 4;
        [Min(1.0f)]
        public float mFrequency = 1.0f;
        [Range(2.0f, 4.0f)]
        public float mLacunarity = 2.0f;
        [Range(0.0f, 1.0f)]
        public float mPersistence = 0.5f;
        public Vector3 mOffset;

        static ComputeBuffer mGradients2DBuffer;
        static ComputeBuffer mGradients3DBuffer;
        static ComputeBuffer mHashArrayBuffer;
        static bool mInit = false;

        protected string mMarkName;

        public static void initSharedData()
        {
            if (mInit)
            {
                return;
            }

            mInit = true;

            mGradients2DBuffer = new ComputeBuffer(TezNoise.gradients2D.Length, sizeof(int) * 2);
            mGradients2DBuffer.SetData(TezNoise.gradients2D);

            mGradients3DBuffer = new ComputeBuffer(TezNoise.gradients3D.Length, sizeof(int) * 3);
            mGradients3DBuffer.SetData(TezNoise.gradients3D);

            mHashArrayBuffer = new ComputeBuffer(TezNoise.hashArray.Length, sizeof(int));
            mHashArrayBuffer.SetData(TezNoise.hashArray);
        }

        public virtual void init(int d, int kernel, ComputeShader shader)
        {
            shader.SetBuffer(kernel, $"inPerlinHashArray", mHashArrayBuffer);
            if(d == 2)
            {
                shader.SetBuffer(kernel, $"inPerlinGradients2D", mGradients2DBuffer);
            }
            else if(d == 3)
            {
                shader.SetBuffer(kernel, $"inPerlinGradients3D", mGradients3DBuffer);
            }

            this.sendToGPU(kernel, shader);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetInt($"in{mMarkName}PLOctave", mOctave);
            shader.SetFloat($"in{mMarkName}PLFrequency", mFrequency);
            shader.SetFloat($"in{mMarkName}PLLacunarity", mLacunarity);
            shader.SetFloat($"in{mMarkName}PLPersistence", mPersistence);
            shader.SetVector($"in{mMarkName}PLOffset", mOffset);
        }
    }
}