using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class FilterData : BaseData
    {
        public Texture2D mBlueNoise;
        [Min(0.0f)]
        public float mBlueNoiseIntensity;

        public override void initIDs()
        {
            this.generateIDs("_BlueNoiseTex2D", "_BlueNoiseIntensity");
        }

        public override void sendToGPU(Material material)
        {
            material.SetTexture(mIDs[0], mBlueNoise);
            material.SetFloat(mIDs[1], mBlueNoiseIntensity);
        }

        public override void update()
        {

        }
    }
}
