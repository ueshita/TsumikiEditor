// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/GuideShader"
{
	Properties
	{
		_Color1 ("Color1", Color) = (1,1,1,1)
		_Color2 ("Color2", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
		ZWrite Off
		ZTest LEqual
		Offset -1, -1
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
				float3 worldPos : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
		
			float4 _Color1;
			float4 _Color2;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
		
			float repeat(float value, float min, float max) {
				float dist = max - min;
				value -= min;
				value = fmod(value, dist);
				return value + min;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float time = _Time.y + 1000;
				float ratio = abs(repeat(time + i.worldPos.x + i.worldPos.y + i.worldPos.z, -1.0, 1.0));
				return lerp(_Color1, _Color2, ratio);
			}
			ENDCG
		}
	}
}
