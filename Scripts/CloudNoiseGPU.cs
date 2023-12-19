using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class CloudNoiseGPU : WorleyNoise
    {
        public event System.Action<RenderTexture> onTextureCreated;
        public RenderTexture renderTexture => mShapeRenderTexture;

        public ComputeShader mComputeShader;
        private int mCSKernel;
        private ComputeBuffer[] mSamplePointBuffers;
        private RenderTexture mShapeRenderTexture;
        private RenderTexture mShapePerlinNoiseTexture;

        public Renderer mPerlinNosieRenderer;

        const int mCSThreadZ = 2;

        int[] shaderGridLengthArray;
        int[] shaderGridCountArray;
        float[] shaderGridRateArray;

        protected override void init()
        {
            base.init();
            this.intiData();
        }

        protected override void close()
        {
            base.close();
            foreach (var item in mSamplePointBuffers)
            {
                item?.Release();
            }
            mShapeRenderTexture?.Release();
        }

        private ComputeBuffer createBuffer(int count, int stride, string name, System.Array array)
        {
            var buffer = new ComputeBuffer(count, stride);
            buffer.SetData(array);
            mComputeShader.SetBuffer(mCSKernel, name, buffer);
            return buffer;
        }

        private T[] converToShaderPackage<T>(T[] array)
        {
            T[] shaderArray = new T[array.Length * 4];
            for (int i = 0; i < array.Length; i++)
            {
                shaderArray[i * 4] = array[i];
            }

            return shaderArray;
        }

        private void intiData()
        {
            mSamplePointBuffers = new ComputeBuffer[4];

            mComputeShader.SetInt("inDimension", (int)mDimension);

            mComputeShader.SetInt("inResolution", mResolution);

//             shaderGridLengthArray = this.converToShaderPackage(mGridLengthArray);
//             shaderGridCountArray = this.converToShaderPackage(mGridCountArray);
//             shaderGridRateArray = this.converToShaderPackage(mGridRateArray);
// 
//             mComputeShader.SetInts("inGridLengthArray", shaderGridLengthArray);
//             mComputeShader.SetInts("inGridCountArray", shaderGridCountArray);
//             mComputeShader.SetFloats("inGridRateArray", shaderGridRateArray);

//             mComputeShader.SetInts("inGridLengthArray", mGridLengthArray);
//             mComputeShader.SetInts("inGridCountArray", mGridCountArray);
//             mComputeShader.SetFloats("inGridRateArray", mGridRateArray);

            mComputeShader.SetVector("inOffset", mOffset);
            mComputeShader.SetBool("inFlip", mFlipWorleyNoise);

            mComputeShader.SetInt("inOctave", mOctave);
            mComputeShader.SetFloat("inFrequency", mFrequency);
            mComputeShader.SetFloat("inLacunarity", mLacunarity);
            mComputeShader.SetFloat("inPersistence", mPersistence);


            switch (mDimension)
            {
                case Dimension.TowD:
                    {
                        mCSKernel = mComputeShader.FindKernel("main2D");

                        mShapeRenderTexture = new RenderTexture(mResolution, mResolution, 16, GraphicsFormat.R16G16B16A16_UNorm)
                        {
                            name = "WorleyNoise",
                            wrapMode = TextureWrapMode.Repeat,
                            filterMode = FilterMode.Bilinear,
                            enableRandomWrite = true,
                        };
                        mShapeRenderTexture.Create();
                        onTextureCreated?.Invoke(mShapeRenderTexture);

                        this.createBuffer(mGridCountArray[0] * mGridCountArray[0], sizeof(int) * 2, "inMarkPos2D", mMarkPointArray2D);

                        mComputeShader.SetTexture(mCSKernel, "outTex2D", mShapeRenderTexture);

                        foreach (var item in mRenderers)
                        {
                            item.material.SetInt("_Dimension", (int)mDimension);
                            item.material.SetTexture("_MainTex2D", mShapeRenderTexture);
                        }

                        mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, 1);
                    }
                    break;
                case Dimension.ThreeD:
                    {
                        mCSKernel = mComputeShader.FindKernel("main3D");

                        mShapeRenderTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_UNorm)
                        {
                            name = "Shap",
                            volumeDepth = mResolution,
                            dimension = TextureDimension.Tex3D,
                            wrapMode = TextureWrapMode.Repeat,
                            filterMode = FilterMode.Bilinear,
                            enableRandomWrite = true,
                        };
                        mShapeRenderTexture.Create();
                        mComputeShader.SetTexture(mCSKernel, "outShapeTex3D", mShapeRenderTexture);

                        mShapePerlinNoiseTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16_UNorm)
                        {
                            name = "Shap",
                            volumeDepth = mResolution,
                            dimension = TextureDimension.Tex3D,
                            wrapMode = TextureWrapMode.Repeat,
                            filterMode = FilterMode.Bilinear,
                            enableRandomWrite = true,
                        };
                        mShapePerlinNoiseTexture.Create();
                        mComputeShader.SetTexture(mCSKernel, "outShapePerlinNoiseTex3D", mShapePerlinNoiseTexture);

                        onTextureCreated?.Invoke(mShapeRenderTexture);

                        for (int i = 0; i < 4; i++)
                        {
                            int count = mGridCountArray[i];
                            mSamplePointBuffers[i] = this.createBuffer(count * count * count, sizeof(int) * 3, $"inSamplePoint3D{i}", mMarkPointArray3D[i]);
                        }

                        foreach (var item in mRenderers)
                        {
                            item.material.SetInt("_Dimension", (int)mDimension);
                            item.material.SetTexture("_MainTex3D", mShapeRenderTexture);
                        }

                        mPerlinNosieRenderer.material.SetTexture("_MainTex3D", mShapePerlinNoiseTexture);

                        mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / mCSThreadZ);
                    }
                    break;
                default:
                    break;
            }
        }

        private void updateColor()
        {
            mComputeShader.SetInt("inOctave", mOctave);
            mComputeShader.SetFloat("inFrequency", mFrequency);
            mComputeShader.SetVector("inOffset", mOffset);
            mComputeShader.SetBool("inFlip", mFlipWorleyNoise);
            //mComputeShader.SetFloats("inGridRateArray", mGridRateArray);
            mComputeShader.SetInt("inDimension", (int)mDimension);
            mComputeShader.SetFloat("inLacunarity", mLacunarity);
            mComputeShader.SetFloat("inPersistence", mPersistence);

            switch (mDimension)
            {
                case Dimension.TowD:
                    mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, 1);
                    break;
                case Dimension.ThreeD:
                    mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / mCSThreadZ);
                    break;
                default:
                    break;
            }
        }

        protected override void updateData()
        {
            this.updateColor();
        }

        protected override void updateOther()
        {
            foreach (var item in mRenderers)
            {
                item.material.SetInt("_Channel", (int)mChannel);
            }
        }
    }
}