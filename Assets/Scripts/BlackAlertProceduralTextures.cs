using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Small runtime-generated albedo texture maps for armoured vehicles and terrain.
    /// These are genuine per-pixel textures; no image-only fake gameplay.
    /// Generated once and reused to minimize APK asset size and device memory.
    /// </summary>
    public static class BlackAlertProceduralTextures
    {
        private static Texture2D azure,crimson,soil;
        private static float Hash(int x,int y,int seed)
        {
            unchecked
            {
                uint n=(uint)(x*374761393+y*668265263+seed*1442695041);
                n=(n^(n>>13))*1274126177u;
                return (n^(n>>16)) / (float)uint.MaxValue;
            }
        }
        private static Texture2D MakeTexture(string name,Color[] colors)
        {
            var texture=new Texture2D(256,256,TextureFormat.RGBA32,true);
            texture.name=name;
            texture.wrapMode=TextureWrapMode.Repeat;
            texture.filterMode=FilterMode.Trilinear;
            texture.anisoLevel=4;
            texture.SetPixels(colors);
            texture.Apply(true,true);
            return texture;
        }

        public static Texture2D Camouflage(bool hostile)
        {
            if(hostile && crimson!=null)return crimson;
            if(!hostile && azure!=null)return azure;
            const int size=256;
            Color baseColor=hostile ? new Color(.42f,.18f,.16f):new Color(.17f,.33f,.36f);
            Color shadeColor=hostile ? new Color(.24f,.20f,.18f):new Color(.17f,.23f,.22f);
            Color lightColor=hostile ? new Color(.53f,.33f,.26f):new Color(.33f,.41f,.36f);
            var colors=new Color[size*size];
            for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    // Multi-octave patch mask; pixel-level grain gives the hull real texture.
                    float shape=Mathf.PerlinNoise(x*.026f+2.1f,y*.026f+12.7f);
                    float fragment=Mathf.PerlinNoise(x*.10f+31.0f,y*.10f+11.0f);
                    float coarse=Hash(x/8,y/8,hostile?19:53);
                    float grain=(Hash(x,y,hostile?37:17)-.5f)*.075f;
                    float v=shape*.65f+fragment*.20f+coarse*.15f;
                    Color color=v<.39f?shadeColor:v>.64f?lightColor:baseColor;
                    color=Color.Lerp(color,Color.white,Mathf.Clamp(grain,-.04f,.04f)*.75f);
                    colors[y*size+x]=color;
                }
            var texture=MakeTexture(hostile?"Crimson original armored camouflage":
                "Azure original armored camouflage",colors);
            if(hostile)crimson=texture;else azure=texture;
            return texture;
        }

        public static Texture2D GroundNoise()
        {
            if(soil!=null)return soil;
            const int size=256;
            var colors=new Color[size*size];
            for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    float stone=Mathf.PerlinNoise(x*.079f+17.2f,y*.079f+9.4f);
                    float detail=Mathf.PerlinNoise(x*.31f+3.3f,y*.31f+7.2f);
                    float grain=Hash(x,y,41);
                    float value=Mathf.Clamp(.66f+stone*.28f+detail*.12f+(grain-.5f)*.09f,.40f,1f);
                    colors[y*size+x]=new Color(value,value,value,1);
                }
            soil=MakeTexture("Black Alert ground pebble and roughness detail",colors);
            return soil;
        }
    }
}
