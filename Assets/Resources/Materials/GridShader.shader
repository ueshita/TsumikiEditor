Shader "Custom/GridShader"
{
	Properties
	{
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
			};
			
			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(0.5, 0.5, 0.5, 1.0);
			}
			ENDCG
		}
	}
}
