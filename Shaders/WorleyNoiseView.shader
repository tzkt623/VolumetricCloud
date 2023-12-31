Shader "Tezcat/WorleyNoiseView"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		ZWrite On
		ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 uv3 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 uv3 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex2D;
			sampler3D _MainTex3D;
			int _Dimension;
			int _Channel;

			float4 _MainTex2D_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex2D);
				o.uv3 = v.vertex.xyz + 0.5f;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float4 col = 1;
				if (_Dimension == 2)
				{
					col = tex2D(_MainTex2D, i.uv);
				}
				else if (_Dimension == 3)
				{
					//mipmap有bug
					float4 colors = tex3Dlod(_MainTex3D, float4(i.uv3, 0));

					switch (_Channel)
					{
					case 0:
						col.rgb = colors.rrr;
						break;
					case 1:
						col.rgb = colors.ggg;
						break;
					case 2:
						col.rgb = colors.bbb;
						break;
					case 3:
						col.rgb = colors.aaa;
						break;
					case 4:
						col.rgb = colors.gba;
						break;
					case 5:
						col.rgb = dot(colors.gba, float3(0.625, 0.25, 0.125));
						break;
					default:
						col.rgb = float3(1, 0, 1);
						break;
					}
				}

				return col;
			}
			ENDCG
		}
	}
}
