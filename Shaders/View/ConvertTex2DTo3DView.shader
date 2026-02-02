Shader "Tezcat/ConvertTex2DTo3DView"
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
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float3 uvw : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler3D _Tex3D;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvw = v.vertex.xyz + 0.5;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float col = tex3D(_Tex3D, i.uvw).r;

				return float4(col,col,col,1);
			}
			ENDCG
		}
	}
}
