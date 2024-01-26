using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class BaseData
    {
        public virtual void sendToGPU(Material material, RenderTexture source, RenderTexture destination) { }
        public virtual void sendToGPU(Material material) { }
        public virtual void update() { }
        public virtual void close() { }
    }
}
