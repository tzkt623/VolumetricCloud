using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class BaseData
    {
        public virtual void sendToGPU(Material material, RenderTexture source, RenderTexture destination) { }
        public virtual void sendToGPU(Material material) { }
        public virtual void sendToGPU() { }
        public virtual void update() { }
        public virtual void close() { }

        protected int[] mIDs;

        protected void generateIDs(params string[] names)
        {
            mIDs = ShaderExtension.generateIDs(names);
        }

        public abstract void initIDs();

        public virtual void init()
        {
            this.initIDs();
        }
    }

    public interface IGPUData
    {

    }


    [System.Serializable]
    public class DebugUVData : BaseData
    {
        public bool mShowUV = false;

        public override void initIDs()
        {
            this.generateIDs("_DebugData001");
        }

        public override void sendToGPU(Material material)
        {
            material.SetInteger(mIDs[0], mShowUV ? 1 : 0);
        }
    }


    public class CloudRenderData : BaseData
    {
        RenderTexture mPerlinWorleyNoiseTex3D = null;
        RenderTexture mDetailNoiseTex3D = null;
        int mFrameCount;

        public void setPerlinWorleyNoiseTex3D(RenderTexture renderTexture)
        {
            mPerlinWorleyNoiseTex3D = renderTexture;
        }

        public void setDetailNoiseTex3D(RenderTexture renderTexture)
        {
            mDetailNoiseTex3D = renderTexture;
        }

        public override void initIDs()
        {
            this.generateIDs("_PerlinWorleyNoiseTex3D", "_DetailNoiseTex3D", "_FrameCount");
        }

        public override void sendToGPU(Material material)
        {
            material.SetTexture(mIDs[0], mPerlinWorleyNoiseTex3D);
            material.SetTexture(mIDs[1], mDetailNoiseTex3D);
            material.SetInteger(mIDs[2], mFrameCount++);
        }
    }

    [System.Serializable]
    public class ExtensionData : BaseData
    {
        public float extF1;
        public float extF2;
        public float extF3;
        public float extF4;

        [Space()]
        public int extI1;
        public int extI2;
        public int extI3;
        public int extI4;

        public override void initIDs()
        {
            this.generateIDs("_Ext001", "_Ext002", "_Ext003");
        }

        public override void sendToGPU(Material material)
        {
            material.SetVector(mIDs[0], new Vector4(extF1, extF2, extF3, extF4));
            material.SetVector(mIDs[1], new Vector4(extI1, extI2, extI3, extI4));
            material.SetVector(mIDs[2], new Vector4());
        }
    }
}
