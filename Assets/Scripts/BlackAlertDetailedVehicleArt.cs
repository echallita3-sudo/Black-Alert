using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Original editable 3D mechanical meshes for pre-alpha gameplay, built as actual
    /// Unity vertices and separate transforms. Not screenshots, painted stand-ins,
    /// final scanned/PBR models, or professional skeletal animation.
    /// </summary>
    public static class BlackAlertDetailedVehicleArt
    {
        private static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();

        private static Color Faction(bool red) => red
            ? new Color(.38f,.16f,.14f) : new Color(.14f,.29f,.38f);
        private static Color FactionLight(bool red) => red
            ? new Color(.66f,.24f,.17f) : new Color(.18f,.47f,.56f);
        private static readonly Color Armor=new Color(.31f,.34f,.34f);
        private static readonly Color Dark=new Color(.075f,.085f,.095f);
        private static readonly Color Tracks=new Color(.115f,.12f,.12f);
        private static readonly Color Window=new Color(.085f,.26f,.31f);
        private static readonly Color Gold=new Color(.65f,.48f,.16f);
        private static readonly Color Steel=new Color(.35f,.39f,.40f);

        private static Material Mat(string name,Color color,float metallic=.25f,float smooth=.4f)
        {
            if(materials.TryGetValue(name,out Material cached))return cached;
            var shader=Shader.Find("Standard");
            if(shader==null)shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            var mat=new Material(shader);
            mat.name="Black Alert PBR first-pass "+name;
            mat.color=color;
            if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);
            if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",smooth);
            materials[name]=mat;
            return mat;
        }
        private static Material Painted(bool red)
        {
            var material=Mat(red?"crimson armor":"azure armor",Color.white,.48f,.36f);
            if(material.mainTexture==null)
            {
                material.mainTexture=BlackAlertProceduralTextures.Camouflage(red);
                material.mainTextureScale=new Vector2(2f,2f);
            }
            return material;
        }
        private static Material Stripe(bool red) =>
            Mat(red?"crimson warning":"azure warning",FactionLight(red),.22f,.55f);

        private static Mesh SharedMesh(string key,Func<Mesh> builder)
        {
            if(meshes.TryGetValue(key,out Mesh existing))return existing;
            Mesh created=builder();
            meshes[key]=created;
            return created;
        }
        private static Transform AddMesh(Transform parent,string name,Mesh mesh,Material mat,
            Vector3 localPosition,Quaternion rotation)
        {
            var go=new GameObject(name);
            go.transform.SetParent(parent,false);
            go.transform.localPosition=localPosition;
            go.transform.localRotation=rotation;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            return go.transform;
        }
        private static Transform Primitive(Transform parent,string name,PrimitiveType type,
            Vector3 at,Vector3 scale,Material mat,Vector3 rotation=default(Vector3))
        {
            var go=GameObject.CreatePrimitive(type);
            go.name=name;
            go.transform.SetParent(parent,false);
            go.transform.localPosition=at;
            go.transform.localScale=scale;
            go.transform.localRotation=Quaternion.Euler(rotation);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            var collider=go.GetComponent<Collider>();
            if(collider!=null)UnityEngine.Object.Destroy(collider);
            return go.transform;
        }

        public static GameObject Tank(Vector3 position,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Battle Tank Mk II":"Azure Battle Tank Mk II");
            root.transform.position=position;
            var collision=root.AddComponent<BoxCollider>();
            collision.center=new Vector3(0,.22f,0);
            collision.size=new Vector3(3.45f,2.5f,5.4f);
            var painted=Painted(hostile);
            var highlight=Stripe(hostile);
            var metal=Mat("gunmetal",Armor);
            var trackMat=Mat("rubber tracks",Tracks,.08f,.18f);
            var darkMat=Mat("recesses",Dark,.05f,.16f);

            Mesh hull=SharedMesh("modern-mbt-angular-hull",()=>BlackAlertMeshGeometry.AngularHull(
                "Modern armored monocoque",
                new BlackAlertMeshGeometry.BodySection(-2.8f,1.25f,-.36f,.40f,.25f),
                new BlackAlertMeshGeometry.BodySection(-2.15f,1.50f,-.36f,.60f,.30f),
                new BlackAlertMeshGeometry.BodySection(1.78f,1.52f,-.36f,.65f,.24f),
                new BlackAlertMeshGeometry.BodySection(2.76f,1.22f,-.32f,.21f,.18f)));
            AddMesh(root.transform,"Angled armored tank chassis",hull,painted,Vector3.zero,Quaternion.identity);

            Mesh upper=SharedMesh("mbt-upper-armor",()=>BlackAlertMeshGeometry.AngularHull(
                "Front glacis and engine deck",
                new BlackAlertMeshGeometry.BodySection(-1.93f,1.03f,.39f,.92f,.21f),
                new BlackAlertMeshGeometry.BodySection(.96f,1.09f,.39f,.96f,.22f),
                new BlackAlertMeshGeometry.BodySection(2.23f,.9f,.33f,.61f,.13f)));
            AddMesh(root.transform,"Second layered armor plate",upper,metal,Vector3.zero,Quaternion.identity);

            var wheels=new List<Transform>();
            for(int side=-1;side<=1;side+=2)
            {
                var section=new GameObject(side<0?"PORT tracked suspension":"STARBOARD tracked suspension");
                section.transform.SetParent(root.transform,false);
                section.transform.localPosition=new Vector3(side*1.45f,0,0);
                var belt=SharedMesh("tank-continuous-track",()=>BlackAlertMeshGeometry.TrackBelt(
                    "Continuous crawler track with curved idlers",.50f,2.62f,-.73f,.19f,.36f));
                AddMesh(section.transform,"One continuous moving track belt",belt,trackMat,
                    Vector3.zero,Quaternion.identity);
                for(int wheel=0;wheel<6;wheel++)
                {
                    float z=-2.07f+wheel*.82f;
                    var w=Primitive(section.transform,"Independent road wheel "+wheel,
                        PrimitiveType.Cylinder,new Vector3(side*.32f,-.48f,z),
                        new Vector3(.44f,.20f,.44f),
                        wheel%2==0?metal:darkMat,new Vector3(0,0,90));
                    wheels.Add(w);
                    Primitive(w,"Wheel hub bolt",PrimitiveType.Cylinder,Vector3.zero,
                        new Vector3(.17f,.52f,.17f),highlight);
                }
                // A side skirt is visually distinct from the actual continuous track.
                var skirt=SharedMesh("mbt-side-skirt",()=>BlackAlertMeshGeometry.AngularHull(
                    "Layered curved track protection",
                    new BlackAlertMeshGeometry.BodySection(-2.4f,.12f,.10f,.53f,.07f),
                    new BlackAlertMeshGeometry.BodySection(2.4f,.12f,.10f,.53f,.07f)));
                AddMesh(section.transform,"Segmented angled track armor",
                    skirt,painted,new Vector3(0,.05f,0),Quaternion.identity);
                for(int i=0;i<4;i++)
                    Primitive(section.transform,"Bolted ceramic panel "+i,PrimitiveType.Cube,
                        new Vector3(side*.24f,.35f,-1.65f+i*1.05f),
                        new Vector3(.08f,.36f,.90f),metal);
            }

            var pivot=new GameObject("Turret yaw / independent weapon articulation");
            pivot.transform.SetParent(root.transform,false);
            pivot.transform.localPosition=new Vector3(0,.93f,-.30f);
            Mesh turret=SharedMesh("mbt-beveled-turret",()=>BlackAlertMeshGeometry.AngularHull(
                "Chamfered rotating composite armor turret",
                new BlackAlertMeshGeometry.BodySection(-1.25f,.88f,-.13f,.52f,.24f),
                new BlackAlertMeshGeometry.BodySection(-.77f,1.17f,-.23f,.72f,.35f),
                new BlackAlertMeshGeometry.BodySection(.77f,1.20f,-.23f,.54f,.28f),
                new BlackAlertMeshGeometry.BodySection(1.20f,.63f,-.10f,.32f,.18f)));
            AddMesh(pivot.transform,"Custom meshed 360-degree turret",turret,painted,Vector3.zero,
                Quaternion.identity);

            var gun=new GameObject("Main cannon elevation and recoil");
            gun.transform.SetParent(pivot.transform,false);
            gun.transform.localPosition=new Vector3(0,.13f,.45f);
            Primitive(gun.transform,"Armored gun mantlet",PrimitiveType.Cylinder,
                new Vector3(0,0,.48f),new Vector3(.47f,.34f,.47f),metal,new Vector3(90,0,0));
            var barrel=SharedMesh("mbt-stepped-barrel",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Stepped bored steel main gun",12,
                new BlackAlertMeshGeometry.TubeSection(.0f,.22f,.22f),
                new BlackAlertMeshGeometry.TubeSection(1.0f,.22f,.22f),
                new BlackAlertMeshGeometry.TubeSection(1.25f,.18f,.18f),
                new BlackAlertMeshGeometry.TubeSection(3.65f,.16f,.16f),
                new BlackAlertMeshGeometry.TubeSection(3.85f,.26f,.26f)));
            AddMesh(gun.transform,"Actual extruded cannon barrel",barrel,metal,
                Vector3.zero,Quaternion.identity);
            Primitive(gun.transform,"Muzzle brake",PrimitiveType.Cylinder,
                new Vector3(0,0,3.85f),new Vector3(.32f,.22f,.32f),darkMat,new Vector3(90,0,0));
            var muzzle=new GameObject("Weapon muzzle flash origin");
            muzzle.transform.SetParent(gun.transform,false);
            muzzle.transform.localPosition=new Vector3(0,0,4.05f);
            Primitive(pivot.transform,"Rotating commander periscope",PrimitiveType.Cube,
                new Vector3(.70f,.90f,-.45f),new Vector3(.39f,.28f,.58f),
                Mat("smoked sight glass",Window,.15f,.77f));
            Primitive(pivot.transform,"Rear storage bustle",PrimitiveType.Cube,
                new Vector3(0,.2f,-1.47f),new Vector3(1.85f,.5f,.65f),metal);
            Primitive(pivot.transform,"Coaxial machine gun",PrimitiveType.Cylinder,
                new Vector3(-.56f,.34f,1.10f),new Vector3(.065f,.83f,.065f),darkMat,
                new Vector3(90,0,0));

            var animate=root.AddComponent<BlackAlertVehicleAnimator>();
            animate.kind=BlackAlertVehicleAnimator.VehicleKind.Tank;
            animate.turret=pivot.transform;
            animate.cannon=gun.transform;
            animate.muzzle=muzzle.transform;
            animate.wheels=wheels.ToArray();
            return root;
        }

        public static GameObject Helicopter(Vector3 position,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Attack Helicopter Mk II":"Azure Attack Helicopter Mk II");
            root.transform.position=position;
            root.AddComponent<BoxCollider>().size=new Vector3(4.8f,2.1f,9.5f);
            var paint=Painted(hostile);
            var dark=Mat("airframe carbon",Dark,.18f,.32f);
            var gunmetal=Mat("airframe structural steel",Steel);
            var glass=Mat("tinted cockpit",Window,.25f,.82f);
            var body=SharedMesh("helicopter-tapered-cockpit",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Aerodynamic fuselage",12,
                new BlackAlertMeshGeometry.TubeSection(-3f,.19f,.20f),
                new BlackAlertMeshGeometry.TubeSection(-2.1f,.70f,.60f),
                new BlackAlertMeshGeometry.TubeSection(-.8f,1.04f,.87f),
                new BlackAlertMeshGeometry.TubeSection(1.55f,.98f,.83f),
                new BlackAlertMeshGeometry.TubeSection(2.4f,.63f,.56f),
                new BlackAlertMeshGeometry.TubeSection(3.0f,.05f,.08f)));
            AddMesh(root.transform,"Tapered gunship fuselage",body,paint,Vector3.zero,Quaternion.identity);
            var canopy=SharedMesh("gunship-cockpit",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Faceted cockpit glazing",10,
                new BlackAlertMeshGeometry.TubeSection(.9f,.80f,.31f,.53f),
                new BlackAlertMeshGeometry.TubeSection(2.0f,.70f,.48f,.32f),
                new BlackAlertMeshGeometry.TubeSection(2.85f,.06f,.09f,.13f)));
            AddMesh(root.transform,"Pilot and navigator glass cockpit",canopy,glass,Vector3.zero,
                Quaternion.identity);

            var boom=SharedMesh("helicopter-tail-boom",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Tapered tail rotor mount",8,
                new BlackAlertMeshGeometry.TubeSection(-6.0f,.13f,.17f,.45f),
                new BlackAlertMeshGeometry.TubeSection(-4.65f,.24f,.30f,.37f),
                new BlackAlertMeshGeometry.TubeSection(-2.5f,.45f,.35f,.3f)));
            AddMesh(root.transform,"Aft tail boom",boom,paint,Vector3.zero,Quaternion.identity);
            Primitive(root.transform,"Tail vertical fin",PrimitiveType.Cube,new Vector3(0,.85f,-5.5f),
                new Vector3(.10f,1.45f,1.4f),gunmetal,new Vector3(-16,0,0));
            var tailRotor=new GameObject("Independent vertical tail rotor");
            tailRotor.transform.SetParent(root.transform,false);
            tailRotor.transform.localPosition=new Vector3(.18f,.73f,-5.67f);
            Primitive(tailRotor.transform,"Tail blade A",PrimitiveType.Cube,Vector3.zero,
                new Vector3(.07f,1.95f,.14f),dark);
            Primitive(tailRotor.transform,"Tail blade B",PrimitiveType.Cube,Vector3.zero,
                new Vector3(.07f,.14f,1.95f),dark);

            for(int side=-1;side<=1;side+=2)
            {
                var points=new [] {
                    new Vector2(side*.92f,-1.40f),new Vector2(side*2.68f,-1.25f),
                    new Vector2(side*2.63f,.45f),new Vector2(side*.96f,.60f)
                };
                AddMesh(root.transform,"Actual wing geometry "+side,
                    SharedMesh(side<0?"attack-heli-wing-left":"attack-heli-wing-right",
                        ()=>BlackAlertMeshGeometry.MetalPlate("Weapon hardpoint wing",.15f,points)),
                    paint,new Vector3(0,-.27f,0),Quaternion.identity);
                Primitive(root.transform,"Hydra missile canister "+side,PrimitiveType.Cylinder,
                    new Vector3(side*2.1f,-.56f,.0f),new Vector3(.41f,.95f,.41f),
                    dark,new Vector3(90,0,0));
                for(int row=0;row<3;row++)
                    Primitive(root.transform,"Visible missile tube "+row,
                        PrimitiveType.Cylinder,new Vector3(side*2.1f+(row-1)*.23f,-.55f,.72f),
                        new Vector3(.10f,.09f,.10f),gunmetal,new Vector3(90,0,0));
                Primitive(root.transform,"Skid landing gear "+side,PrimitiveType.Cube,
                    new Vector3(side*.83f,-1.2f,0),new Vector3(.12f,.12f,3.6f),dark);
            }
            var mainRotor=new GameObject("Articulated primary rotor");
            mainRotor.transform.SetParent(root.transform,false);
            mainRotor.transform.localPosition=new Vector3(0,1.26f,-.42f);
            Primitive(mainRotor.transform,"Rotor hub",PrimitiveType.Cylinder,Vector3.zero,
                new Vector3(.32f,.20f,.32f),gunmetal);
            for(int i=0;i<4;i++)
            {
                var blade=Primitive(mainRotor.transform,"Aerodynamic blade "+i,
                    PrimitiveType.Cube,new Vector3(0,.11f,3.35f),
                    new Vector3(.41f,.065f,6.5f),dark);
                blade.localRotation=Quaternion.Euler(0,i*90f,0);
                blade.localPosition=Quaternion.Euler(0,i*90f,0)*new Vector3(0,.11f,3.35f);
            }
            var belly=Primitive(root.transform,"Chin machine cannon",PrimitiveType.Cylinder,
                new Vector3(0,-.81f,2.1f),new Vector3(.14f,.75f,.14f),gunmetal,
                new Vector3(90,0,0));
            var muzzle=new GameObject("Weapon muzzle flash origin");
            muzzle.transform.SetParent(root.transform,false);
            muzzle.transform.localPosition=new Vector3(0,-.8f,2.85f);
            var motion=root.AddComponent<BlackAlertVehicleAnimator>();
            motion.kind=BlackAlertVehicleAnimator.VehicleKind.Helicopter;
            motion.rotor=mainRotor.transform;
            motion.tailRotor=tailRotor.transform;
            motion.muzzle=muzzle.transform;
            return root;
        }

        public static GameObject Jet(Vector3 position,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Interceptor F-21":"Azure Interceptor F-21");
            root.transform.position=position;
            root.AddComponent<BoxCollider>().size=new Vector3(9.6f,2.0f,11.0f);
            var paint=Painted(hostile);
            var accent=Stripe(hostile);
            var dark=Mat("aircraft engine",Dark,.5f,.65f);
            var glass=Mat("jet mirror canopy",Window,.30f,.85f);
            var body=SharedMesh("jet-streamlined-12-ring",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Twin-engined interceptor body",12,
                new BlackAlertMeshGeometry.TubeSection(-5f,.45f,.48f),
                new BlackAlertMeshGeometry.TubeSection(-3.3f,.90f,.65f),
                new BlackAlertMeshGeometry.TubeSection(-1.1f,1.18f,.63f),
                new BlackAlertMeshGeometry.TubeSection(.55f,.83f,.66f),
                new BlackAlertMeshGeometry.TubeSection(2.5f,.58f,.43f),
                new BlackAlertMeshGeometry.TubeSection(4.65f,.02f,.05f)));
            AddMesh(root.transform,"Swept interceptor aerodynamic fuselage",body,
                paint,Vector3.zero,Quaternion.identity);

            for(int side=-1;side<=1;side+=2)
            {
                var polygon=new []{
                    new Vector2(side*.82f,1.18f),new Vector2(side*5.2f,-1.67f),
                    new Vector2(side*4.75f,-2.82f),new Vector2(side*.84f,-1.40f)
                };
                AddMesh(root.transform,"True swept metal wing "+side,
                    SharedMesh("jet-wing-"+side,()=>BlackAlertMeshGeometry.MetalPlate(
                        "Wing with angled leading and trailing edges",.16f,polygon)),
                    paint,Vector3.zero,Quaternion.identity);
                var tail=new []{
                    new Vector2(side*.55f,-3.05f),new Vector2(side*2.15f,-4.85f),
                    new Vector2(side*1.93f,-5.18f),new Vector2(side*.55f,-4.55f)
                };
                AddMesh(root.transform,"Swept tailplane "+side,
                    SharedMesh("jet-stabilizer-"+side,()=>BlackAlertMeshGeometry.MetalPlate(
                        "Interceptor rear stabilizer",.13f,tail)),
                    accent,new Vector3(0,.17f,0),Quaternion.identity);
                Primitive(root.transform,"Canted tail fin "+side,PrimitiveType.Cube,
                    new Vector3(side*.70f,.80f,-3.98f),new Vector3(.14f,1.40f,1.55f),
                    accent,new Vector3(0,0,side*15f));
                Primitive(root.transform,"Engine exhaust ring "+side,PrimitiveType.Cylinder,
                    new Vector3(side*.46f,-.03f,-4.85f),new Vector3(.43f,.25f,.43f),
                    dark,new Vector3(90,0,0));
                Primitive(root.transform,"Underwing missile "+side,PrimitiveType.Capsule,
                    new Vector3(side*2.6f,-.23f,-.42f),new Vector3(.22f,1.09f,.22f),
                    Mat("missile white",Steel),new Vector3(90,0,0));
            }
            var canopy=SharedMesh("jet-pilot-canopy",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Aerodynamic dark glass canopy",12,
                new BlackAlertMeshGeometry.TubeSection(.25f,.52f,.1f,.65f),
                new BlackAlertMeshGeometry.TubeSection(1.4f,.52f,.31f,.72f),
                new BlackAlertMeshGeometry.TubeSection(2.75f,.12f,.08f,.51f)));
            AddMesh(root.transform,"Pilot enclosure and canopy",canopy,glass,Vector3.zero,
                Quaternion.identity);
            var muzzle=new GameObject("Jet cannon origin");
            muzzle.transform.SetParent(root.transform,false);
            muzzle.transform.localPosition=new Vector3(.35f,-.1f,4.75f);
            var motion=root.AddComponent<BlackAlertVehicleAnimator>();
            motion.kind=BlackAlertVehicleAnimator.VehicleKind.Jet;
            motion.muzzle=muzzle.transform;
            return root;
        }

        public static GameObject Destroyer(Vector3 position,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Guided Missile Destroyer":"Azure Guided Missile Destroyer");
            root.transform.position=position;
            root.AddComponent<BoxCollider>().size=new Vector3(5.4f,3.2f,13.8f);
            var paint=Painted(hostile);
            var dark=Mat("ship underwater",Dark,.2f,.18f);
            var gray=Mat("ship plated hull",Steel,.55f,.31f);
            var deck=Mat("antislip deck",Armor,.32f,.18f);
            var hull=SharedMesh("destroyer-hull",()=>BlackAlertMeshGeometry.AngularHull(
                "Keel, narrow bow and tapered stern",
                new BlackAlertMeshGeometry.BodySection(-6.6f,.22f,-1.50f,.02f,.10f),
                new BlackAlertMeshGeometry.BodySection(-5.1f,2.1f,-1.52f,.31f,.30f),
                new BlackAlertMeshGeometry.BodySection(-.8f,2.55f,-1.57f,.44f,.38f),
                new BlackAlertMeshGeometry.BodySection(3.45f,2.0f,-1.22f,.42f,.31f),
                new BlackAlertMeshGeometry.BodySection(6.8f,.15f,-.65f,.15f,.08f)));
            AddMesh(root.transform,"Tapered deep displacement vessel hull",hull,gray,
                Vector3.zero,Quaternion.identity);
            var shipDeck=SharedMesh("warship-deck",()=>BlackAlertMeshGeometry.AngularHull(
                "Ship flight deck and anti-slip plating",
                new BlackAlertMeshGeometry.BodySection(-6.4f,.13f,.17f,.47f,.07f),
                new BlackAlertMeshGeometry.BodySection(-4.8f,1.92f,.17f,.48f,.17f),
                new BlackAlertMeshGeometry.BodySection(3.6f,1.84f,.17f,.48f,.18f),
                new BlackAlertMeshGeometry.BodySection(6.4f,.11f,.15f,.32f,.06f)));
            AddMesh(root.transform,"Upper marine armored deck",shipDeck,deck,Vector3.zero,
                Quaternion.identity);

            Mesh bridge=SharedMesh("ship-faceted-bridge",()=>BlackAlertMeshGeometry.AngularHull(
                "Faceted missile destroyer bridge",
                new BlackAlertMeshGeometry.BodySection(-2.8f,1.15f,.46f,1.70f,.30f),
                new BlackAlertMeshGeometry.BodySection(-1.0f,1.35f,.45f,2.1f,.33f),
                new BlackAlertMeshGeometry.BodySection(.9f,1.12f,.45f,1.86f,.30f)));
            AddMesh(root.transform,"Multi-level bridge",bridge,paint,Vector3.zero,Quaternion.identity);
            Primitive(root.transform,"Command bridge glass",PrimitiveType.Cube,
                new Vector3(0,1.68f,1.0f),new Vector3(1.72f,.32f,.10f),
                Mat("smoked ship radar bridge",Window,.22f,.77f));
            Primitive(root.transform,"Radar mast",PrimitiveType.Cylinder,
                new Vector3(0,3.10f,-1.3f),new Vector3(.13f,1.25f,.13f),gray);
            var radar=new GameObject("Rotating air-search radar");
            radar.transform.SetParent(root.transform,false);
            radar.transform.localPosition=new Vector3(0,4.23f,-1.3f);
            Primitive(radar.transform,"Radar antenna",PrimitiveType.Cube,Vector3.zero,
                new Vector3(2.75f,.27f,.13f),dark);

            for(int i=0;i<12;i++)
                Primitive(root.transform,"Vertical missile launch cell "+i,
                    PrimitiveType.Cube,new Vector3((i%4-1.5f)*.68f,.55f,-4f+i/4*.72f),
                    new Vector3(.58f,.18f,.57f),dark);

            var pivot=new GameObject("Primary naval gun turret yaw");
            pivot.transform.SetParent(root.transform,false);
            pivot.transform.localPosition=new Vector3(0,.85f,3.55f);
            var gunBody=SharedMesh("destroyer-stealth-turret",()=>BlackAlertMeshGeometry.AngularHull(
                "Naval stealth gun shield",
                new BlackAlertMeshGeometry.BodySection(-.85f,.77f,-.20f,.59f,.22f),
                new BlackAlertMeshGeometry.BodySection(.65f,.93f,-.18f,.58f,.24f),
                new BlackAlertMeshGeometry.BodySection(1.08f,.62f,-.11f,.36f,.19f)));
            AddMesh(pivot.transform,"Traversing gun shield",gunBody,paint,Vector3.zero,Quaternion.identity);
            var cannon=new GameObject("Independent naval main gun");
            cannon.transform.SetParent(pivot.transform,false);
            cannon.transform.localPosition=new Vector3(0,.12f,.40f);
            var tube=SharedMesh("naval-gun-barrel",()=>BlackAlertMeshGeometry.SmoothFuselage(
                "Naval bored artillery barrel",10,
                new BlackAlertMeshGeometry.TubeSection(0f,.17f,.17f),
                new BlackAlertMeshGeometry.TubeSection(3.75f,.11f,.11f)));
            AddMesh(cannon.transform,"102mm naval main cannon",tube,dark,Vector3.zero,Quaternion.identity);
            var muzzle=new GameObject("Naval muzzle origin");
            muzzle.transform.SetParent(cannon.transform,false);
            muzzle.transform.localPosition=new Vector3(0,0,3.92f);

            var motion=root.AddComponent<BlackAlertVehicleAnimator>();
            motion.kind=BlackAlertVehicleAnimator.VehicleKind.Ship;
            motion.turret=pivot.transform;
            motion.cannon=cannon.transform;
            motion.radar=radar.transform;
            motion.muzzle=muzzle.transform;
            return root;
        }
    }

    /// <summary>Independent rotor, suspension wheel, muzzle recoil, and heavy-hull motion.</summary>
    public sealed class BlackAlertVehicleAnimator : MonoBehaviour
    {
        public enum VehicleKind { Tank,Helicopter,Jet,Ship }
        public VehicleKind kind;
        public Transform turret,cannon,rotor,tailRotor,radar,muzzle;
        public Transform[] wheels;
        private Vector3 lastPosition;
        private Vector3 cannonRest;
        private float phase;
        private float lastFire=-100f;
        private float vehicleSpeed;

        public Vector3 MuzzlePosition => muzzle!=null?muzzle.position:transform.position+transform.forward;
        public void Fire()
        {
            lastFire=Time.time;
        }
        private void Start()
        {
            lastPosition=transform.position;
            if(cannon!=null)cannonRest=cannon.localPosition;
        }
        private void LateUpdate()
        {
            float dt=Mathf.Max(Time.deltaTime,.0001f);
            Vector3 travel=transform.position-lastPosition;
            vehicleSpeed=new Vector2(travel.x,travel.z).magnitude/dt;
            lastPosition=transform.position;
            phase+=dt;
            if(wheels!=null)
                foreach(var wheel in wheels)
                    if(wheel!=null)wheel.Rotate(Vector3.up,vehicleSpeed*dt*38f,Space.Self);
            if(rotor!=null)rotor.Rotate(Vector3.up,1200f*dt,Space.Self);
            if(tailRotor!=null)tailRotor.Rotate(Vector3.right,2100f*dt,Space.Self);
            if(radar!=null)radar.Rotate(Vector3.up,40f*dt,Space.Self);
            if(cannon!=null)
            {
                float recoil=Mathf.Max(0,1f-(Time.time-lastFire)*8f);
                cannon.localPosition=cannonRest-Vector3.forward*(recoil*.42f);
            }
            if(kind==VehicleKind.Ship)
            {
                // Heavy ship roll/pitch, without pinning the heading or its position.
                var forward=transform.forward;
                float heading=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
                transform.rotation=Quaternion.Euler(
                    Mathf.Sin(phase*.82f)*1.10f,heading,
                    Mathf.Sin(phase*.58f)*1.45f);
            }
            if(kind==VehicleKind.Helicopter)
            {
                // Banking is gentle to preserve target heading and avoid overwriting waypoints.
                var forward=transform.forward;
                float heading=Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg;
                transform.rotation=Quaternion.Euler(
                    Mathf.Clamp(vehicleSpeed*.5f,0,4f),heading,
                    Mathf.Sin(phase*1.2f)*2.2f);
            }
        }
    }
}
