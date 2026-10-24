using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Articulated original infantry model: independent shoulder/hip/knee joints,
    /// chest armor, ballistic helmet, boots, rifle and backpack.
    /// Not a skinned mocap soldier or final-quality character texture set.
    /// </summary>
    public static class BlackAlertDetailedInfantryArt
    {
        private static Mesh torso,helmet,chest,boots;
        private static Material cyan,crimson,armor,bootsMaterial,visors,rifle;

        private static Material Make(Color c,float metal=.25f)
        {
            var s=Shader.Find("Standard");
            if(s==null)s=Shader.Find("Universal Render Pipeline/Lit");
            if(s==null)s=Shader.Find("Sprites/Default");
            var m=new Material(s);m.color=c;
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.35f);
            return m;
        }
        private static void Materials()
        {
            if(cyan!=null)return;
            cyan=Make(new Color(.13f,.34f,.40f),.32f);
            crimson=Make(new Color(.49f,.20f,.18f),.32f);
            armor=Make(new Color(.23f,.27f,.29f),.43f);
            bootsMaterial=Make(new Color(.09f,.10f,.11f),.09f);
            visors=Make(new Color(.09f,.39f,.49f),.25f);
            rifle=Make(new Color(.15f,.16f,.16f),.50f);
        }
        private static Transform Block(Transform parent,string name,Vector3 at,Vector3 scale,
            Material mat,PrimitiveType shape=PrimitiveType.Cube,Vector3 angles=default(Vector3))
        {
            var go=GameObject.CreatePrimitive(shape);
            go.name=name;
            go.transform.SetParent(parent,false);
            go.transform.localPosition=at;
            go.transform.localScale=scale;
            go.transform.localEulerAngles=angles;
            go.GetComponent<Renderer>().sharedMaterial=mat;
            var hit=go.GetComponent<Collider>();
            if(hit!=null)Object.Destroy(hit);
            return go.transform;
        }
        private static Transform Geometry(Transform parent,string name,Mesh mesh,
            Vector3 at,Material material)
        {
            var go=new GameObject(name);
            go.transform.SetParent(parent,false);
            go.transform.localPosition=at;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            return go.transform;
        }
        public static GameObject Create(Vector3 location,bool hostile)
        {
            Materials();
            if(torso==null)
            {
                torso=BlackAlertMeshGeometry.AngularHull("Rifleman's fitted armored torso",
                    new BlackAlertMeshGeometry.BodySection(-.19f,.24f,-.34f,.32f,.085f),
                    new BlackAlertMeshGeometry.BodySection(.19f,.34f,-.39f,.35f,.10f));
                chest=BlackAlertMeshGeometry.AngularHull("Tactical body plates",
                    new BlackAlertMeshGeometry.BodySection(-.10f,.29f,-.17f,.17f,.045f),
                    new BlackAlertMeshGeometry.BodySection(.15f,.30f,-.19f,.19f,.06f));
                helmet=BlackAlertMeshGeometry.SmoothFuselage("Ballistic helmet surface",12,
                    new BlackAlertMeshGeometry.TubeSection(-.30f,.16f,.17f,.02f),
                    new BlackAlertMeshGeometry.TubeSection(-.14f,.28f,.24f,0f),
                    new BlackAlertMeshGeometry.TubeSection(.19f,.28f,.25f,0f),
                    new BlackAlertMeshGeometry.TubeSection(.29f,.20f,.18f,0f));
                boots=BlackAlertMeshGeometry.AngularHull("Reinforced combat boot",
                    new BlackAlertMeshGeometry.BodySection(-.18f,.14f,-.13f,.12f,.04f),
                    new BlackAlertMeshGeometry.BodySection(.24f,.17f,-.14f,.13f,.04f));
            }
            var root=new GameObject(hostile?"Crimson Infantry Rifleman Mk II":"Azure Infantry Rifleman Mk II");
            root.transform.position=location;
            var collision=root.AddComponent<CapsuleCollider>();
            collision.radius=.34f;
            collision.height=2.06f;
            collision.center=new Vector3(0,1.03f,0);

            Material faction=hostile?crimson:cyan;
            var pelvis=Block(root.transform,"Armored pelvis",new Vector3(0,.89f,0),
                new Vector3(.53f,.29f,.39f),faction);
            Geometry(root.transform,"Meshed and tapered armored torso",torso,
                new Vector3(0,1.28f,0),faction);
            Geometry(root.transform,"Tactical chest plate",chest,
                new Vector3(0,1.33f,.25f),armor);
            Block(root.transform,"Plate carrier harness",new Vector3(0,1.36f,-.27f),
                new Vector3(.42f,.51f,.19f),armor);
            Block(root.transform,"Backpack radio unit",new Vector3(0,1.31f,-.39f),
                new Vector3(.46f,.51f,.26f),bootsMaterial);
            Block(root.transform,"Radio antenna",new Vector3(.16f,1.73f,-.38f),
                new Vector3(.025f,.65f,.025f),rifle);
            Block(root.transform,"Neck guard",new Vector3(0,1.75f,0),
                new Vector3(.25f,.15f,.20f),armor);
            var head=new GameObject("Head and helmet pose");
            head.transform.SetParent(root.transform,false);
            head.transform.localPosition=new Vector3(0,1.90f,0);
            Geometry(head.transform,"Helmet custom rounded mesh",helmet,Vector3.zero,armor);
            Block(head.transform,"Polycarbonate shield",new Vector3(0,-.04f,.27f),
                new Vector3(.41f,.18f,.12f),visors);
            Block(head.transform,"Armor side ear protection L",new Vector3(-.27f,-.09f,0),
                new Vector3(.08f,.17f,.30f),faction);
            Block(head.transform,"Armor side ear protection R",new Vector3(.27f,-.09f,0),
                new Vector3(.08f,.17f,.30f),faction);
            var animator=root.AddComponent<BlackAlertInfantryAnimator>();
            animator.head=head.transform;

            for(int side=-1;side<=1;side+=2)
            {
                var shoulder=new GameObject(side<0?"Left shoulder joint":"Right shoulder joint");
                shoulder.transform.SetParent(root.transform,false);
                shoulder.transform.localPosition=new Vector3(side*.37f,1.58f,0);
                Block(shoulder.transform,"Upper arm armor",new Vector3(side*.03f,-.24f,0),
                    new Vector3(.23f,.47f,.29f),faction,PrimitiveType.Capsule);
                var elbow=new GameObject("Elbow and lower arm");
                elbow.transform.SetParent(shoulder.transform,false);
                elbow.transform.localPosition=new Vector3(0,-.45f,0);
                Block(elbow.transform,"Forearm steel guard",new Vector3(0,-.20f,.02f),
                    new Vector3(.21f,.36f,.23f),armor,PrimitiveType.Capsule);
                Block(elbow.transform,"Protective glove",new Vector3(0,-.42f,.06f),
                    new Vector3(.22f,.18f,.22f),bootsMaterial);
                var hip=new GameObject(side<0?"Left leg hip joint":"Right leg hip joint");
                hip.transform.SetParent(root.transform,false);
                hip.transform.localPosition=new Vector3(side*.19f,.83f,0);
                Block(hip.transform,"Reinforced thigh armor",new Vector3(0,-.20f,0),
                    new Vector3(.27f,.42f,.32f),faction,PrimitiveType.Capsule);
                var knee=new GameObject("Independent hinged knee");
                knee.transform.SetParent(hip.transform,false);
                knee.transform.localPosition=new Vector3(0,-.46f,0);
                Block(knee.transform,"Shin guard",new Vector3(0,-.20f,0),
                    new Vector3(.24f,.42f,.30f),armor,PrimitiveType.Capsule);
                Geometry(knee.transform,"Separate heavy combat boot",boots,
                    new Vector3(0,-.43f,.08f),bootsMaterial);
                if(side<0)
                {
                    animator.leftShoulder=shoulder.transform;
                    animator.leftElbow=elbow.transform;
                    animator.leftHip=hip.transform;
                    animator.leftKnee=knee.transform;
                }
                else
                {
                    animator.rightShoulder=shoulder.transform;
                    animator.rightElbow=elbow.transform;
                    animator.rightHip=hip.transform;
                    animator.rightKnee=knee.transform;
                }
            }
            var heldRifle=new GameObject("Assault rifle / removable independent weapon");
            heldRifle.transform.SetParent(root.transform,false);
            heldRifle.transform.localPosition=new Vector3(.16f,1.36f,.46f);
            Block(heldRifle.transform,"Weapon receiver",Vector3.zero,
                new Vector3(.17f,.19f,.68f),rifle);
            Block(heldRifle.transform,"Weapon front handguard",new Vector3(0,.02f,.53f),
                new Vector3(.14f,.11f,.58f),armor);
            Block(heldRifle.transform,"Long metal barrel",new Vector3(0,.02f,1.02f),
                new Vector3(.05f,.06f,.60f),bootsMaterial);
            Block(heldRifle.transform,"Detachable magazine",new Vector3(0,-.16f,.17f),
                new Vector3(.13f,.31f,.15f),bootsMaterial,
                PrimitiveType.Cube,new Vector3(12,0,0));
            Block(heldRifle.transform,"Optical reflex sight",new Vector3(0,.18f,.23f),
                new Vector3(.13f,.13f,.19f),visors);
            var muzzle=new GameObject("Rifle muzzle origin");
            muzzle.transform.SetParent(heldRifle.transform,false);
            muzzle.transform.localPosition=new Vector3(0,.02f,1.33f);
            animator.muzzle=muzzle.transform;
            return root;
        }
    }

    public sealed class BlackAlertInfantryAnimator:MonoBehaviour
    {
        public Transform leftShoulder,rightShoulder,leftElbow,rightElbow;
        public Transform leftHip,rightHip,leftKnee,rightKnee,head,muzzle;
        private Vector3 previous;
        private float phase;
        private float walkBlend;
        private void Start() { previous=transform.position; }
        private void LateUpdate()
        {
            float dt=Mathf.Max(Time.deltaTime,.001f);
            var delta=transform.position-previous; delta.y=0;
            float speed=delta.magnitude/dt;
            previous=transform.position;
            walkBlend=Mathf.MoveTowards(walkBlend,Mathf.Clamp01(speed/2.7f),dt*4f);
            phase+=dt*(4f+speed*2.6f);
            float step=Mathf.Sin(phase)*walkBlend;
            float opposite=Mathf.Sin(phase+Mathf.PI)*walkBlend;
            if(leftHip!=null)leftHip.localRotation=Quaternion.Euler(step*28f,0,0);
            if(rightHip!=null)rightHip.localRotation=Quaternion.Euler(opposite*28f,0,0);
            if(leftKnee!=null)leftKnee.localRotation=Quaternion.Euler(
                Mathf.Max(0,-step)*42f,0,0);
            if(rightKnee!=null)rightKnee.localRotation=Quaternion.Euler(
                Mathf.Max(0,-opposite)*42f,0,0);
            if(leftShoulder!=null)leftShoulder.localRotation=Quaternion.Euler(
                -24f+opposite*15f,0,-12f);
            if(rightShoulder!=null)rightShoulder.localRotation=Quaternion.Euler(
                -38f+step*10f,0,12f);
            if(leftElbow!=null)leftElbow.localRotation=Quaternion.Euler(-18f,0,0);
            if(rightElbow!=null)rightElbow.localRotation=Quaternion.Euler(-20f,0,0);
            if(head!=null)head.localRotation=Quaternion.Euler(
                Mathf.Sin(phase*.49f)*1.7f,Mathf.Sin(phase*.32f)*2.3f,0);
        }
    }
}
