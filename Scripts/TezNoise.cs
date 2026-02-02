using UnityEngine;

namespace tezcat.Framework.Utility
{
    public class TezNoise
    {
        static System.Func<Vector3, int, float>[] mFunctions = new System.Func<Vector3, int, float>[]
        {
            value1D,
            value2D,
            value3D,
            perlin1D,
            perlin2D,
            perlin3D,
            perlin2DRepeat,
            perlin3DRepeat,
        };

        public enum Function
        {
            Error = -1,
            Value1D,
            Value2D,
            Value3D,
            Perlin1D,
            Perlin2D,
            Perlin3D,
            Perlin2DRepeat,
            Perlin3DRepeat,
        }

        #region 工具
        public static int[] hashArray => mHashArray;

        /// <summary>
        /// 双倍(复制的内容)数组长度
        /// 用于优化 GradientsX[m_HashArray[h0 + iy0] 算法
        /// 因为h0+iy0的最大值是255*2
        /// 没有双倍长度需要在这里&mask才行
        /// </summary>
        private static int[] mHashArray = {
            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
             57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
             74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
             60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
             65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
             52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
             81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180,

            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
             57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
             74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
             60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
             65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
             52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
             81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
        };

        private const int mHashMask = 255;

        private static float smooth(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float dot(Vector2 g, float x, float y)
        {
            return g.x * x + g.y * y;
        }

        private static float dot(Vector3 g, float x, float y, float z)
        {
            return g.x * x + g.y * y + g.z * z;
        }

        private static float select(float a, float b, bool flag)
        {
            return flag ? b : a;
        }

        private static int select(int a, int b, bool flag)
        {
            return flag ? b : a;
        }
        #endregion

        #region 值噪音(随机数)
        public static float value1D(float point, int frequency)
        {
            ///振动!!!!
            point *= frequency;

            ///获得当前点左边参考值的Index
            int i0 = Mathf.FloorToInt(point);

            ///计算当前点到左边参考值Index的距离
            ///用于作为插值的粗略系数
            var t = point - i0;

            ///将获得的参考值的Index归一化到HashArray的长度中
            i0 &= mHashMask;

            ///获得右边参考值的Index
            int i1 = i0 + 1;

            ///取得左边点和右边点的参考值
            int h0 = mHashArray[i0];
            int h1 = mHashArray[i1];

            ///获得平滑的插值系数
            t = smooth(t);

            ///用当前点左右两边的参考值
            ///配合平滑插值系数进行插值
            ///取得当前点真正的参考值(Mathf.Lerp(h0, h1, t))
            ///并且转换到0-1之内(除以(1f / m_HashMask))
            ///便获得了平滑的一维随机数
            return Mathf.Lerp(h0, h1, t) * (1f / mHashMask);
        }

        public static float value1D(Vector3 point, int frequency)
        {
            return value1D(point.x, frequency);
        }

        public static float value2D(Vector2 point, int frequency)
        {
            point *= frequency;

            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);

            var tx = point.x - ix0;
            var ty = point.y - iy0;

            ix0 &= mHashMask;
            iy0 &= mHashMask;

            int ix1 = ix0 + 1;
            int iy1 = iy0 + 1;

            ///取得线上的值
            ///为了下一步计算面上的值
            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            ///计算面上的值
            ///用于插值计算
            int h00 = mHashArray[h0 + iy0];
            int h10 = mHashArray[h1 + iy0];
            int h01 = mHashArray[h0 + iy1];
            int h11 = mHashArray[h1 + iy1];

            tx = smooth(tx);
            ty = smooth(ty);

            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx)
                , Mathf.Lerp(h01, h11, tx)
                , ty) * (1f / mHashMask);
        }

        public static float value2D(Vector3 point, int frequency)
        {
            return value2D(new Vector2(point.x, point.y), frequency);
        }

        public static float value3D(Vector3 point, int frequency)
        {
            return value3D(point, frequency, Vector3.zero);
        }

        public static float value3D(Vector3 point, int frequency, Vector3 offset)
        {
            point *= frequency;
            point += offset;
            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);
            int iz0 = Mathf.FloorToInt(point.z);

            var tx = point.x - ix0;
            var ty = point.y - iy0;
            var tz = point.z - iz0;

            ix0 &= mHashMask;
            iy0 &= mHashMask;
            iz0 &= mHashMask;

            int ix1 = ix0 + 1;
            int iy1 = iy0 + 1;
            int iz1 = iz0 + 1;

            ///计算线上的值
            ///为了下一步计算面上的值
            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            ///计算面上的值
            ///为了下一步计算体上的值
            int h00 = mHashArray[h0 + iy0];
            int h10 = mHashArray[h1 + iy0];
            int h01 = mHashArray[h0 + iy1];
            int h11 = mHashArray[h1 + iy1];

            ///计算体上的值
            ///用于插值计算
            int h000 = mHashArray[h00 + iz0];
            int h100 = mHashArray[h10 + iz0];
            int h010 = mHashArray[h01 + iz0];
            int h110 = mHashArray[h11 + iz0];
            int h001 = mHashArray[h00 + iz1];
            int h101 = mHashArray[h10 + iz1];
            int h011 = mHashArray[h01 + iz1];
            int h111 = mHashArray[h11 + iz1];

            tx = smooth(tx);
            ty = smooth(ty);
            tz = smooth(tz);

            return Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(h000, h100, tx), Mathf.Lerp(h010, h110, tx), ty)
                , Mathf.Lerp(Mathf.Lerp(h001, h101, tx), Mathf.Lerp(h011, h111, tx), ty)
                , tz) * (1f / mHashMask);
        }
        #endregion

        #region 柏林噪音(随机数)
        private static float[] mGradients1D = { 1f, -1f };
        private const int mGradientsMask1D = 1;

        public static Vector2[] gradients2D => mGradients2D;
        private static Vector2[] mGradients2D =
        {
            new Vector2( 1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2( 0f, 1f),
            new Vector2( 0f,-1f),
            new Vector2( 1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2( 1f,-1f).normalized,
            new Vector2(-1f,-1f).normalized
        };
        private const int mGradientsMask2D = 7;
        private static float Sqrt2 = Mathf.Sqrt(2f);

        public static Vector3[] gradients3D => mGradients3D;
        private static Vector3[] mGradients3D =
        {
            new Vector3( 1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3( 1f,-1f, 0f),
            new Vector3(-1f,-1f, 0f),
            new Vector3( 1f, 0f, 1f),
            new Vector3(-1f, 0f, 1f),
            new Vector3( 1f, 0f,-1f),
            new Vector3(-1f, 0f,-1f),
            new Vector3( 0f, 1f, 1f),
            new Vector3( 0f,-1f, 1f),
            new Vector3( 0f, 1f,-1f),
            new Vector3( 0f,-1f,-1f),
            
            new Vector3( 1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3( 0f,-1f, 1f),
            new Vector3( 0f,-1f,-1f)
        };
        private const int mGradientsMask3D = 15;

        public static float perlin1D(float point, int frequency)
        {
            point *= frequency;
            int i0 = Mathf.FloorToInt(point);
            var t0 = point - i0;
            var t1 = t0 - 1;
            i0 &= mHashMask;

            int i1 = i0 + 1;

            float g0 = mGradients1D[mHashArray[i0] & mGradientsMask1D];
            float g1 = mGradients1D[mHashArray[i1] & mGradientsMask1D];

            float v0 = g0 * t0;
            float v1 = g1 * t1;

            var t = smooth(t0);

            return Mathf.Lerp(v0, v1, t) * 2f;
        }

        public static float perlin1D(Vector3 point, int frequency)
        {
            return perlin1D(point.x, frequency);
        }

        public static float perlin2D(Vector2 point, int frequency)
        {
            point *= frequency;

            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);

            //获得差值,小数部分,即相对坐标位置
            var tx0 = point.x - ix0;
            var ty0 = point.y - iy0;

            //减去1,是为了得到当前点到(1,x)和(x, 1)的向量
            var tx1 = tx0 - 1f;
            var ty1 = ty0 - 1f;
            //这里是分开计算,理论算法为
            //因为ix0-ix1之间的距离是1,所以等价于-1
            //point - vec(ix0, iy0) = (tx0, ty0) - vec(0 ,0) = (tx0, ty0)
            //point - vec(ix1, iy0) = (tx0, ty0) - vec(1, 0) = (tx1, ty0)
            //point - vec(ix0, iy1) = (tx0, ty0) - vec(0, 1) = (tx0, ty1)
            //point - vec(ix1, iy1) = (tx0, ty0) - vec(1, 1) = (tx1, ty1)


            ix0 &= mHashMask;
            iy0 &= mHashMask;

            int ix1 = (ix0 + 1) & mHashMask;
            int iy1 = (iy0 + 1) & mHashMask;

            //获得x轴随机数
            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            //获得晶格点上的随机向量
            var g00 = mGradients2D[/*获得Y轴随机数*/mHashArray[h0 + iy0] & mGradientsMask2D];
            var g10 = mGradients2D[/*获得Y轴随机数*/mHashArray[h1 + iy0] & mGradientsMask2D];
            var g01 = mGradients2D[/*获得Y轴随机数*/mHashArray[h0 + iy1] & mGradientsMask2D];
            var g11 = mGradients2D[/*获得Y轴随机数*/mHashArray[h1 + iy1] & mGradientsMask2D];

            //计算随机向量贡献值
            float v00 = dot(g00, tx0, ty0);
            float v10 = dot(g10, tx1, ty0);
            float v01 = dot(g01, tx0, ty1);
            float v11 = dot(g11, tx1, ty1);

            //将线性比例平滑之后计算成插值系数
            float tx = smooth(tx0);
            float ty = smooth(ty0);

            return Mathf.Lerp(
                Mathf.Lerp(v00, v10, tx), //LeftBottom插值到RightBottom
                Mathf.Lerp(v01, v11, tx), //LeftTop插值到RightTop
                ty); // Bottom插值到Top
        }

        public static float perlin2DRepeat(Vector3 point, int frequency)
        {
            point *= frequency;

            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);

            //获得差值,小数部分,即相对坐标位置
            var tx0 = point.x - ix0;
            var ty0 = point.y - iy0;

            //减去1,是为了得到当前点到(1,x)和(x, 1)的向量
            var tx1 = tx0 - 1f;
            var ty1 = ty0 - 1f;

            ix0 %= frequency;
            iy0 %= frequency;

            ix0 &= mHashMask;
            iy0 &= mHashMask;

            int ix1 = (ix0 + 1) % frequency;
            int iy1 = (iy0 + 1) % frequency;

            ix1 &= mHashMask;
            iy1 &= mHashMask;

            //获得x轴随机数
            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            //获得晶格点上的随机向量
            var g00 = mGradients2D[/*获得Y轴随机数*/mHashArray[h0 + iy0] & mGradientsMask2D];
            var g10 = mGradients2D[/*获得Y轴随机数*/mHashArray[h1 + iy0] & mGradientsMask2D];
            var g01 = mGradients2D[/*获得Y轴随机数*/mHashArray[h0 + iy1] & mGradientsMask2D];
            var g11 = mGradients2D[/*获得Y轴随机数*/mHashArray[h1 + iy1] & mGradientsMask2D];

            //计算随机向量贡献值
            float v00 = dot(g00, tx0, ty0);
            float v10 = dot(g10, tx1, ty0);
            float v01 = dot(g01, tx0, ty1);
            float v11 = dot(g11, tx1, ty1);

            //将线性比例平滑之后计算成插值系数
            float tx = smooth(tx0);
            float ty = smooth(ty0);

            return Mathf.Lerp(
                Mathf.Lerp(v00, v10, tx), //LeftBottom插值到RightBottom
                Mathf.Lerp(v01, v11, tx), //LeftTop插值到RightTop
                ty); // Bottom插值到Top
        }

        public static float perlin2D(Vector3 point, int frequency)
        {
            return perlin2D(new Vector2(point.x, point.y), frequency);
        }

        public static float perlin3D(Vector3 point, int frequency)
        {
            point *= frequency;

            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);
            int iz0 = Mathf.FloorToInt(point.z);

            var tx0 = point.x - ix0;
            var ty0 = point.y - iy0;
            var tz0 = point.z - iz0;
            var tx1 = tx0 - 1f;
            var ty1 = ty0 - 1f;
            var tz1 = tz0 - 1f;

            ix0 &= mHashMask;
            iy0 &= mHashMask;
            iz0 &= mHashMask;

            int ix1 = ix0 + 1;
            int iy1 = iy0 + 1;
            int iz1 = iz0 + 1;

            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            int h00 = mHashArray[h0 + iy0];
            int h10 = mHashArray[h1 + iy0];
            int h01 = mHashArray[h0 + iy1];
            int h11 = mHashArray[h1 + iy1];

            var g000 = mGradients3D[mHashArray[h00 + iz0] & mGradientsMask3D];
            var g100 = mGradients3D[mHashArray[h10 + iz0] & mGradientsMask3D];
            var g010 = mGradients3D[mHashArray[h01 + iz0] & mGradientsMask3D];
            var g110 = mGradients3D[mHashArray[h11 + iz0] & mGradientsMask3D];
            var g001 = mGradients3D[mHashArray[h00 + iz1] & mGradientsMask3D];
            var g101 = mGradients3D[mHashArray[h10 + iz1] & mGradientsMask3D];
            var g011 = mGradients3D[mHashArray[h01 + iz1] & mGradientsMask3D];
            var g111 = mGradients3D[mHashArray[h11 + iz1] & mGradientsMask3D];

            float v000 = dot(g000, tx0, ty0, tz0);
            float v100 = dot(g100, tx1, ty0, tz0);
            float v010 = dot(g010, tx0, ty1, tz0);
            float v110 = dot(g110, tx1, ty1, tz0);
            float v001 = dot(g001, tx0, ty0, tz1);
            float v101 = dot(g101, tx1, ty0, tz1);
            float v011 = dot(g011, tx0, ty1, tz1);
            float v111 = dot(g111, tx1, ty1, tz1);

            float tx = smooth(tx0);
            float ty = smooth(ty0);
            float tz = smooth(tz0);

            return Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(v000, v100, tx), Mathf.Lerp(v010, v110, tx), ty),
                Mathf.Lerp(Mathf.Lerp(v001, v101, tx), Mathf.Lerp(v011, v111, tx), ty),
                tz);
        }

        public static float perlin3DRepeat(Vector3 point, int frequency)
        {
            point *= frequency;

            int ix0 = Mathf.FloorToInt(point.x);
            int iy0 = Mathf.FloorToInt(point.y);
            int iz0 = Mathf.FloorToInt(point.z);

            var tx0 = point.x - ix0;
            var ty0 = point.y - iy0;
            var tz0 = point.z - iz0;
            var tx1 = tx0 - 1f;
            var ty1 = ty0 - 1f;
            var tz1 = tz0 - 1f;

            ix0 %= frequency;
            ix0 = select(ix0, ix0 + frequency, ix0 < 0);
            iy0 %= frequency;
            iy0 = select(iy0, iy0 + frequency, iy0 < 0);
            iz0 %= frequency;
            iz0 = select(iz0, iz0 + frequency, iz0 < 0);

            ix0 &= mHashMask;
            iy0 &= mHashMask;
            iz0 &= mHashMask;

            int ix1 = (ix0 + 1) % frequency;
            int iy1 = (iy0 + 1) % frequency;
            int iz1 = (iz0 + 1) % frequency;

            ix1 &= mHashMask;
            iy1 &= mHashMask;
            iz1 &= mHashMask;

            int h0 = mHashArray[ix0];
            int h1 = mHashArray[ix1];

            int h00 = mHashArray[h0 + iy0];
            int h10 = mHashArray[h1 + iy0];
            int h01 = mHashArray[h0 + iy1];
            int h11 = mHashArray[h1 + iy1];

            var g000 = mGradients3D[mHashArray[h00 + iz0] & mGradientsMask3D];
            var g100 = mGradients3D[mHashArray[h10 + iz0] & mGradientsMask3D];
            var g010 = mGradients3D[mHashArray[h01 + iz0] & mGradientsMask3D];
            var g110 = mGradients3D[mHashArray[h11 + iz0] & mGradientsMask3D];
            var g001 = mGradients3D[mHashArray[h00 + iz1] & mGradientsMask3D];
            var g101 = mGradients3D[mHashArray[h10 + iz1] & mGradientsMask3D];
            var g011 = mGradients3D[mHashArray[h01 + iz1] & mGradientsMask3D];
            var g111 = mGradients3D[mHashArray[h11 + iz1] & mGradientsMask3D];

            float v000 = dot(g000, tx0, ty0, tz0);
            float v100 = dot(g100, tx1, ty0, tz0);
            float v010 = dot(g010, tx0, ty1, tz0);
            float v110 = dot(g110, tx1, ty1, tz0);
            float v001 = dot(g001, tx0, ty0, tz1);
            float v101 = dot(g101, tx1, ty0, tz1);
            float v011 = dot(g011, tx0, ty1, tz1);
            float v111 = dot(g111, tx1, ty1, tz1);

            float tx = smooth(tx0);
            float ty = smooth(ty0);
            float tz = smooth(tz0);

            return Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(v000, v100, tx), Mathf.Lerp(v010, v110, tx), ty),
                Mathf.Lerp(Mathf.Lerp(v001, v101, tx), Mathf.Lerp(v011, v111, tx), ty),
                tz);
        }
        #endregion

        #region 分形噪音
        /// <summary>
        /// 范围-1到1
        /// </summary>
        /// <param name="method"></param>
        /// <param name="point"></param>
        /// <param name="frequency"></param>
        /// <param name="octaves"></param>
        /// <param name="lacunarity"></param>
        /// <param name="persistence"></param>
        /// <returns></returns>
        public static float sum(Function method, Vector3 point, int frequency, int octaves, int lacunarity, float persistence)
        {
            var function = mFunctions[(int)method];

            float sum = 0;
            float amplitude = 1f;
            float sum_amplitude = 1f;

            for (int o = 0; o < octaves; o++)
            {
                sum += function(point, frequency) * amplitude;

                sum_amplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }
            return sum / sum_amplitude;
        }

        public static float sumWithUnityPerlinNoise(Vector3 point, int frequency, int octaves, int lacunarity, float persistence)
        {
            float sum = 0;
            float amplitude = 1f;
            float sum_amplitude = 1f;
            Vector3 p;

            for (int o = 0; o < octaves; o++)
            {
                p = point * frequency;
                sum += Mathf.PerlinNoise(p.x, p.y);

                sum_amplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return sum / sum_amplitude;
        }
        #endregion
    }
}