Shader "BlackAlert/RiverWater"
{
    Properties
    {
        _ShallowColor ("Shallow Water", Color) = (0.09,0.31,0.36,1)
        _DeepColor ("Deep Water", Color) = (0.02,0.13,0.20,1)
        _WaveSpeed ("Wave Speed", Range(0.1,4)) = 1.2
        _Opacity ("Surface Transparency", Range(0.5,1)) = 0.88
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Cull Back
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard alpha:fade
        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        half _WaveSpeed;
        half _Opacity;

        struct Input
        {
            float3 worldPos;
        };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Per-pixel moving interference ripples; no stationary photograph.
            float2 p=IN.worldPos.xz;
            float w1=sin(p.x*2.1+p.y*.37+_Time.y*_WaveSpeed);
            float w2=sin(p.y*3.2-p.x*.42-_Time.y*_WaveSpeed*.73);
            float w3=sin(p.x*5.1+p.y*1.75+_Time.y*_WaveSpeed*.61);
            float wave=(w1+w2*.48+w3*.24)/1.72;
            float deep=saturate(.55+wave*.18);
            fixed3 water=lerp(_ShallowColor.rgb,_DeepColor.rgb,deep);
            float highlight=pow(saturate(.55+.42*wave),8)*.27;
            o.Albedo=water+highlight;
            o.Metallic=.08;
            o.Smoothness=.76+wave*.07;
            o.Normal=normalize(float3(wave*.09,w2*.09,1));
            o.Emission=water*.09+highlight*.25;
            o.Alpha=_Opacity;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
