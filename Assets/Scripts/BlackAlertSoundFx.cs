using UnityEngine;

namespace BlackAlert
{
    // Procedural demo audio for first runtime test.
    // Audio is spatialized and generated at runtime; replace with recorded/mixed effects later.
    public static class BlackAlertSoundFx
    {
        private static AudioClip warning,explosion,shot;

        private static AudioClip Make(string name,float seconds,float fundamental,int seed,bool noise)
        {
            const int sampleRate=22050;
            int count=(int)(seconds*sampleRate);
            var buffer=new float[count];
            var random=new System.Random(seed);
            for(int i=0;i<count;i++)
            {
                float t=(float)i/sampleRate;
                float env=Mathf.Clamp01(1f-t/seconds);
                float tone=Mathf.Sin(2f*Mathf.PI*fundamental*t);
                float second=Mathf.Sin(2f*Mathf.PI*(fundamental*1.51f)*t);
                float white=(float)(random.NextDouble()*2-1);
                buffer[i]=Mathf.Clamp((tone*.50f+second*.18f+(noise?white*.39f:0f))*
                    env*env,-.85f,.85f);
            }
            var clip=AudioClip.Create(name,count,1,sampleRate,false);
            clip.SetData(buffer,0);
            return clip;
        }
        public static void Shot(Vector3 pos)
        {
            if(shot==null)shot=Make("BlackAlert weapon test",.20f,172f,123,true);
            AudioSource.PlayClipAtPoint(shot,pos,.50f);
        }
        public static void Warning(Vector3 pos)
        {
            if(warning==null)warning=Make("BlackAlert air raid siren",2.5f,420f,339,false);
            AudioSource.PlayClipAtPoint(warning,pos,.85f);
        }
        public static void Explosion(Vector3 pos)
        {
            if(explosion==null)explosion=Make("BlackAlert nuclear test blast",1.9f,60f,914,true);
            AudioSource.PlayClipAtPoint(explosion,pos,1f);
        }
    }
}
