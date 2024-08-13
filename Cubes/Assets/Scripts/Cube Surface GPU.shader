Shader "Custom/Cube Surface GPU"
{
    Properties
    {
    }
    SubShader
    {
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma target 4.5

        struct Input
        {
            float3 worldPos;
        };

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	    StructuredBuffer<float3> _Positions;
		#endif

        float _Step;

        void ConfigureProcedural()
        {
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
				float3 position = _Positions[unity_InstanceID];
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
				unity_ObjectToWorld._m00_m11_m22 = _Step;
			#endif
		}

        void ConfigureSurface(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = saturate(IN.worldPos * 0.5 + 0.5);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
