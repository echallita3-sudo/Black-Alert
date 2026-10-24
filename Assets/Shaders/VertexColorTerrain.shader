Shader "BlackAlert/VertexColorTerrain"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _FogTex ("Faction Sight Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        struct Input
        {
            float2 uv_MainTex;
            fixed4 color : COLOR;
            float3 worldPos;
        };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.color=v.color;
        }
        sampler2D _MainTex;
        sampler2D _FogTex;
        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 fogUV=float2(saturate((IN.worldPos.x+50.0)/100.0),
                                   saturate((IN.worldPos.z+45.0)/90.0));
            half light=tex2D(_FogTex,fogUV).r;
            // Unknown locations are nearly black; explored hills are visible dimly.
            o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb * IN.color.rgb *
                lerp(0.07,1.0,saturate(light));
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
