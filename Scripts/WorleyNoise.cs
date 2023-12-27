using UnityEngine;

namespace tezcat.Framework.Exp
{
    public abstract class WorleyNoise : MonoBehaviour
    {
        public enum Dimension
        {
            TowD = 2,
            ThreeD = 3
        }

        public enum Channel
        {
            C0 = 0,
            C1,
            C2,
            C3,
            C123
        }
        public bool mUpdate = false;
        public Channel mChannel = Channel.C0;

        [Header("Worley Noise", order = 0)]
        public Dimension mDimension = Dimension.TowD;
        public int mResolution = 64;
        public int[] mGridCountArray;
        protected int[] mGridLengthArray;
        protected float[] mGridRateArray;
        protected Vector2Int[] mMarkPointArray2D;
        protected Vector3Int[][] mMarkPointArray3D;
        public bool mFlipWorleyNoise = false;

        [Header("Perlin Noise")]
        [Range(1, 10)]
        public int mOctave = 4;
        [Min(1.0f)]
        public float mFrequency = 1.0f;
        [Range(2.0f, 4.0f)]
        public float mLacunarity = 2.0f;
        [Range(0.0f, 1.0f)]
        public float mPersistence = 0.5f;
        public Vector3 mOffset;
        public Vector3 mMoveSpeed;

        [Header("Viewer")]
        public Renderer[] mRenderers;


        private void Start()
        {
            this.init();
        }

        private void OnDestroy()
        {
            this.close();
        }

        private void Update()
        {
            if (mUpdate)
            {
                mOffset += Time.deltaTime * mMoveSpeed;
                this.updateData();
            }
            else
            {
                this.updateOther();
            }
        }

        protected virtual void updateOther()
        {

        }

        protected virtual void updateData()
        {

        }

        protected virtual void close()
        {

        }

        protected virtual void init()
        {
            mMarkPointArray3D = new Vector3Int[4][];
            mGridLengthArray = new int[4];
            mGridRateArray = new float[4];

            this.calculateMarkPointArray();
        }

        protected void calculateMarkPointArray()
        {
            switch (mDimension)
            {
                case Dimension.TowD:
                    {
                        var grid_length = mResolution / mGridCountArray[0];
                        var grid_count = mGridCountArray[0];

                        mMarkPointArray2D = new Vector2Int[mGridCountArray[0] * mGridCountArray[0]];
                        for (int y = 0; y < grid_count; y++)
                        {
                            for (int x = 0; x < grid_count; x++)
                            {
                                int begin_x = x * grid_length;
                                int begin_y = y * grid_length;

                                var pos_x = Random.Range(begin_x, begin_x + grid_length);
                                var pos_y = Random.Range(begin_y, begin_y + grid_length);

                                mMarkPointArray2D[x + y * grid_count] = new Vector2Int(pos_x, pos_y);
                            }
                        }

                        mGridLengthArray[0] = grid_length;
                        mGridRateArray[0] = 1.0f / mGridCountArray[0];
                    }
                    break;
                case Dimension.ThreeD:
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            this.calculateSamplePoint3D(i, mGridCountArray[i]);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        protected void calculateSamplePoint3D(int index, int gridCount)
        {
            var grid_length = mResolution / gridCount;
            var array = new Vector3Int[gridCount * gridCount * gridCount];

            for (int z = 0; z < gridCount; z++)
            {
                int begin_z = z * grid_length;
                int z_offset = z * gridCount * gridCount;

                for (int y = 0; y < gridCount; y++)
                {
                    int begin_y = y * grid_length;
                    int y_offset = y * gridCount;

                    for (int x = 0; x < gridCount; x++)
                    {
                        int begin_x = x * grid_length;

                        var pos_x = Random.Range(begin_x, begin_x + grid_length);
                        var pos_y = Random.Range(begin_y, begin_y + grid_length);
                        var pos_z = Random.Range(begin_z, begin_z + grid_length);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            mMarkPointArray3D[index] = array;
            mGridLengthArray[index] = grid_length;
            mGridRateArray[index] = 1.0f / gridCount;
        }

    }
}