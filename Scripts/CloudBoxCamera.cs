﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [ImageEffectAllowedInSceneView]
    public class CloudBoxCamera : MonoBehaviour
    {
        public CloudBox mCloudBox;

        void Start()
        {

        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            mCloudBox.renderCloudBox(source, destination);
        }
    }
}