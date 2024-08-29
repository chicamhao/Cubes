Shader "Custom/Fractal Surface GPU"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        struct Input
        {
            float3 worldPos;
        };

		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	    StructuredBuffer<float4x4> _Matrices;
		#endif
        float4 _BaseColor;

        void ConfigureProcedural()
        {
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                unity_ObjectToWorld = _Matrices[unity_InstanceID];
			#endif
		}

        void ConfigureSurface(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _BaseColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
