Shader "BlackAlert/VertexColorTerrain"
{
    Properties { _MainTex ("Albedo", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        struct Input { float2 uv_MainTex; fixed4 color : COLOR; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.color=v.color;
        }
        sampler2D _MainTex;
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*IN.color.rgb;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
