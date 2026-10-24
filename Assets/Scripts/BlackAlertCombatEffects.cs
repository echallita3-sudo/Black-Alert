using System.Collections.Generic;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Actual world-space particle emissions (muzzle sparks, dust, smoke, blast debris).
    /// Bounded reusable particle systems are intentionally cheap enough for midrange Android.
    /// Procedural placeholder VFX; not film-quality pyro / texture flipbooks.
    /// </summary>
    public static class BlackAlertCombatEffects
    {
        private enum EffectKind { Muzzle, Sparks, Smoke, NuclearFire, NuclearDust }
        private sealed class Entry
        {
            public EffectKind kind;
            public ParticleSystem system;
            public float availableAt;
        }
        private static readonly List<Entry> pool=new List<Entry>();
        private static Material fireMaterial;
        private const int PoolLimit=36;

        private static Material ParticleMaterial()
        {
            if(fireMaterial!=null)return fireMaterial;
            var shader=Shader.Find("Particles/Standard Unlit");
            if(shader==null)shader=Shader.Find("Legacy Shaders/Particles/Additive");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            fireMaterial=new Material(shader);
            fireMaterial.name="Black Alert runtime battlefield particles";
            return fireMaterial;
        }

        private static Entry Create(EffectKind kind)
        {
            var go=new GameObject("Black Alert Pooled "+kind);
            var system=go.AddComponent<ParticleSystem>();
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;
            main.loop=false;
            main.playOnAwake=false;
            main.duration=1.5f;
            main.startLifetime=kind==EffectKind.Muzzle?.11f:
                kind==EffectKind.Smoke?1.4f:
                kind==EffectKind.NuclearDust?3.3f:
                kind==EffectKind.NuclearFire?2.2f:.45f;
            main.startSpeed=kind==EffectKind.Muzzle?1.4f:
                kind==EffectKind.Smoke?1.8f:
                kind==EffectKind.NuclearDust?8f:
                kind==EffectKind.NuclearFire?11f:5.5f;
            main.startSize=kind==EffectKind.Muzzle?.45f:
                kind==EffectKind.Smoke?.65f:
                kind==EffectKind.NuclearDust?2.5f:
                kind==EffectKind.NuclearFire?3.8f:.13f;
            main.maxParticles=kind==EffectKind.NuclearDust?110:80;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=kind==EffectKind.Smoke ?
                new Color(.16f,.17f,.18f,.48f):
                kind==EffectKind.NuclearDust ?
                new Color(.32f,.28f,.25f,.68f):
                kind==EffectKind.Sparks ?
                new Color(1f,.55f,.12f,1f):
                new Color(1f,.69f,.18f,1f);
            var emission=system.emission;
            emission.enabled=false;
            var shape=system.shape;
            shape.enabled=true;
            shape.shapeType=ParticleSystemShapeType.Sphere;
            shape.radius=kind==EffectKind.NuclearDust?7.0f:
                kind==EffectKind.NuclearFire?3.0f:
                kind==EffectKind.Muzzle?.05f:.18f;

            var lifetimeColor=system.colorOverLifetime;
            lifetimeColor.enabled=true;
            var fade=new Gradient();
            var start=main.startColor.color;
            fade.SetKeys(new[]{
                new GradientColorKey(start,.0f),
                new GradientColorKey(start,.48f),
                new GradientColorKey(new Color(.21f,.20f,.20f),1f)
            },new[]{
                new GradientAlphaKey(kind==EffectKind.Smoke?.52f:1f,0f),
                new GradientAlphaKey(.55f,.5f),
                new GradientAlphaKey(0f,1f)
            });
            lifetimeColor.color=new ParticleSystem.MinMaxGradient(fade);
            var shapeOverTime=system.sizeOverLifetime;
            shapeOverTime.enabled=true;
            var curve=new AnimationCurve(
                new Keyframe(0f,.45f),
                new Keyframe(.35f,kind==EffectKind.Smoke||kind==EffectKind.NuclearDust?1.85f:1.2f),
                new Keyframe(1f,.1f));
            shapeOverTime.size=new ParticleSystem.MinMaxCurve(1f,curve);
            var renderer=go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial=ParticleMaterial();
            renderer.sortingFudge=0.1f;
            return new Entry{kind=kind,system=system,availableAt=-1f};
        }

        private static void Emit(EffectKind kind,Vector3 world,int count)
        {
            Entry chosen=null;
            foreach(var entry in pool)
                if(entry.kind==kind && entry.availableAt<=Time.time)
                {chosen=entry;break;}
            if(chosen==null && pool.Count<PoolLimit)
            {chosen=Create(kind);pool.Add(chosen);}
            if(chosen==null)
            {
                foreach(var entry in pool)
                    if(entry.kind==kind && (chosen==null||entry.availableAt<chosen.availableAt))
                        chosen=entry;
            }
            if(chosen==null)return;
            var system=chosen.system;
            if(system==null)return;
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            system.transform.position=world;
            system.Play(true);
            system.Emit(count);
            chosen.availableAt=Time.time+
                (kind==EffectKind.NuclearDust?3.5f:
                 kind==EffectKind.Smoke?1.6f:
                 kind==EffectKind.NuclearFire?2.5f:.6f);
        }

        public static void MuzzleFlash(Vector3 position) { Emit(EffectKind.Muzzle,position,8); }

        public static void Hit(Vector3 position)
        {
            Emit(EffectKind.Sparks,position,13);
            Emit(EffectKind.Smoke,position,7);
        }

        public static void NuclearDetonation(Vector3 position)
        {
            Emit(EffectKind.NuclearFire,position+Vector3.up*3f,68);
            Emit(EffectKind.NuclearDust,position+Vector3.up,78);
        }

        public static void Clear()
        {
            foreach(var entry in pool)
                if(entry.system!=null)
                    Object.Destroy(entry.system.gameObject);
            pool.Clear();
        }
    }
}
