Shader "Unlit/TheShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MousePos ("Mouse Pos", Vector) = (0, 0, 0, 1)
		_Color("Quad color", Color) = (.25, .5, .5, 1)
		_HoleRadius ("Hole radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MousePos;
            float4 _Color;
            float _HoleRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;

                float dist = length(i.worldPos.xy - _MousePos.xy);
                if (dist < _HoleRadius) col.a = 0;

                return col;
            }
            ENDCG
        }
    }
}
