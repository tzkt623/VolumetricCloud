using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public interface IPostRenderer
    {
        void rendering(RenderTexture source, RenderTexture destination);
    }



}