using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class RayMarching : MonoBehaviour
    {
        public Camera mCamera;
        public Material mMaterial;

        // Start is called before the first frame update
        void Start()
        {
            mCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, mMaterial);
        }

        float getDist(Vector3 p, Vector3 wPos)
        {
            float d = Vector3.Distance(p, wPos);
            return d;
        }

        float getDist(Vector3 p)
        {
            float d = Vector3.Distance(p, Vector3.zero) - 0.5f;
            return d;
        }

    }
}