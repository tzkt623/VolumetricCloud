using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace tezcat.Framework.Exp
{
    public class WorleyNoiseNew : MonoBehaviour
    {
        public int mSize = 2;
        public int mResolution = 64;

        private int[] mRandomNumberArray = new int[]
        {
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

        int mGridPixelCount;

        /*
         * 总体思路
         * 
         * 找一个一定长度的随机数列
         * 然后划分虚拟的格子
         * 在每个格子里面需要找一个点来当标点
         * 这时去随机数列里面挑一个出来
         * 
         * 挑选过程
         * 1.得到格子像素总数量
         * 2.从取出随机数列里面第一个数
         * 3.如果次数大于格子像素总数量,把他转换到格子像素数量范围内 = 随机数 % 格子数量
         * 4.如果想生成不同序列的,可以向后移动随机数列下标
         * 
         */
        void Start()
        {
//             mRandomNumberArray = new int[256];
//             for (int i = 0; i < mRandomNumberArray.Length; i++)
//             {
//                 mRandomNumberArray[i] = Random.Range(0, 256);
//             }

            int length = mResolution / mSize / mSize;
            mGridPixelCount = length * length;

            for (int x = 0; x < length * length; x++)
            {
                Debug.Log($"SpecialPoint {this.getSpecialPoint(x)}");
            }
        }

        int getSpecialPoint(int gridIndex)
        {
            int rate = mGridPixelCount / mRandomNumberArray.Length;
            //说明至少要补充到两倍大
            //但是有可能会超过
            //所以做一个模来限制范围
            if (rate > 0)
            {
                return mRandomNumberArray[gridIndex % mRandomNumberArray.Length] * (rate + 1) % mGridPixelCount;
            }

            return mRandomNumberArray[gridIndex] % mGridPixelCount;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}