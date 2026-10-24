using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Real editable 3D part hierarchies for combat-test infantry, jet, ship and reactor.
    /// Engineering models: not final cinematic-quality assets, skins or animations.
    /// </summary>
    public static class BlackAlertAdditionalModels
    {
        static readonly Color Metal=new Color(.22f,.27f,.33f);
        static readonly Color Dark=new Color(.08f,.09f,.11f);
        static readonly Color Glass=new Color(.18f,.58f,.76f);

        static Material Paint(Color c)
        {
            var shader=Shader.Find("Standard");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            var material=new Material(shader);
            material.color=c;
            return material;
        }
        static Transform Piece(Transform parent,PrimitiveType primitive,string name,
            Vector3 local,Vector3 size,Color color)
        {
            var go=GameObject.CreatePrimitive(primitive);
            go.name=name;
            go.transform.SetParent(parent,false);
            go.transform.localPosition=local;
            go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=Paint(color);
            Object.Destroy(go.GetComponent<Collider>());
            return go.transform;
        }
        static Color Faction(bool hostile)
        {
            return hostile?new Color(.73f,.18f,.15f):new Color(.15f,.47f,.79f);
        }
        public static GameObject Rifleman(Vector3 at,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Rifleman":"Azure Rifleman");
            root.transform.position=at;
            var body=root.AddComponent<CapsuleCollider>();
            body.center=Vector3.up*1.05f;body.radius=.36f;body.height=2.0f;
            var paint=Faction(hostile);
            Piece(root.transform,PrimitiveType.Capsule,"Armored vest",new Vector3(0,1.1f,0),
                new Vector3(.65f,.6f,.4f),paint);
            Piece(root.transform,PrimitiveType.Sphere,"Ballistic helmet",new Vector3(0,1.93f,0),
                new Vector3(.5f,.45f,.52f),Metal);
            Piece(root.transform,PrimitiveType.Cube,"Helmet visor",new Vector3(0,1.96f,.27f),
                new Vector3(.39f,.13f,.10f),Glass);
            Piece(root.transform,PrimitiveType.Cube,"Rifle body",new Vector3(.18f,1.25f,.50f),
                new Vector3(.22f,.15f,.65f),Dark);
            Piece(root.transform,PrimitiveType.Cylinder,"Rifle barrel",new Vector3(.18f,1.25f,.95f),
                new Vector3(.045f,.40f,.045f),Metal).localRotation=Quaternion.Euler(90,0,0);
            var motion=root.AddComponent<BlackAlertAdditionalMotion>();
            for(int side=-1;side<=1;side+=2)
            {
                var arm=new GameObject(side<0?"Left articulated arm":"Right articulated arm");
                arm.transform.SetParent(root.transform,false);
                arm.transform.localPosition=new Vector3(side*.41f,1.50f,0);
                Piece(arm.transform,PrimitiveType.Capsule,"Arm",new Vector3(0,-.27f,0),
                    new Vector3(.20f,.32f,.23f),paint);
                var leg=new GameObject(side<0?"Left articulated leg":"Right articulated leg");
                leg.transform.SetParent(root.transform,false);
                leg.transform.localPosition=new Vector3(side*.19f,.78f,0);
                Piece(leg.transform,PrimitiveType.Capsule,"Boot",new Vector3(0,-.34f,0),
                    new Vector3(.22f,.36f,.22f),Metal);
                if(side<0) {motion.leftArm=arm.transform;motion.leftLeg=leg.transform;}
                else {motion.rightArm=arm.transform;motion.rightLeg=leg.transform;}
            }
            return root;
        }
        public static GameObject Destroyer(Vector3 at,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Heavy Destroyer":"Azure Heavy Destroyer");
            root.transform.position=at;
            root.AddComponent<BoxCollider>().size=new Vector3(3.8f,2.1f,11f);
            Color paint=Faction(hostile);
            Piece(root.transform,PrimitiveType.Cube,"Displacement hull",new Vector3(0,-.30f,0),
                new Vector3(3.5f,1.4f,10.7f),Metal);
            Piece(root.transform,PrimitiveType.Cube,"Armored deck",new Vector3(0,.43f,0),
                new Vector3(3.75f,.24f,9.4f),paint);
            Piece(root.transform,PrimitiveType.Cube,"Superstructure",new Vector3(0,1.30f,-.75f),
                new Vector3(2.0f,1.65f,3.1f),paint);
            Piece(root.transform,PrimitiveType.Cylinder,"Radar tower",new Vector3(0,2.65f,-1f),
                new Vector3(.22f,1.1f,.22f),Metal);
            var gun=new GameObject("Independent Naval Turret");
            gun.transform.SetParent(root.transform,false);
            gun.transform.localPosition=new Vector3(0,.80f,3f);
            Piece(gun.transform,PrimitiveType.Cylinder,"Rotating gun base",Vector3.zero,
                new Vector3(1f,.35f,1f),Metal);
            Piece(gun.transform,PrimitiveType.Cube,"Main naval cannon",new Vector3(0,.1f,1.25f),
                new Vector3(.25f,.2f,2.4f),Dark);
            var motion=root.AddComponent<BlackAlertAdditionalMotion>();
            motion.rotatePart=gun.transform;motion.ship=true;
            return root;
        }
        public static GameObject Jet(Vector3 at,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Interceptor":"Azure Interceptor");
            root.transform.position=at;
            root.AddComponent<BoxCollider>().size=new Vector3(9f,1.6f,10f);
            Color paint=Faction(hostile);
            Piece(root.transform,PrimitiveType.Capsule,"Jet fuselage",Vector3.zero,
                new Vector3(1.4f,4.6f,1.4f),paint).localRotation=Quaternion.Euler(90,0,0);
            var left=Piece(root.transform,PrimitiveType.Cube,"Swept left wing",new Vector3(-2,0,-.25f),
                new Vector3(4.4f,.18f,2.15f),paint);
            left.localRotation=Quaternion.Euler(0,-17,0);
            var right=Piece(root.transform,PrimitiveType.Cube,"Swept right wing",new Vector3(2,0,-.25f),
                new Vector3(4.4f,.18f,2.15f),paint);
            right.localRotation=Quaternion.Euler(0,17,0);
            Piece(root.transform,PrimitiveType.Cube,"Pilot canopy",new Vector3(0,.58f,1.6f),
                new Vector3(.7f,.30f,1.6f),Glass);
            for(int side=-1;side<=1;side+=2)
                Piece(root.transform,PrimitiveType.Cube,"Tail stabilizer",
                    new Vector3(side*.62f,.76f,-3.4f),new Vector3(.13f,1.45f,1.4f),Metal);
            return root;
        }
        public static GameObject Reactor(Vector3 at,bool hostile)
        {
            var root=new GameObject(hostile?"Crimson Reactor":"Azure Reactor");
            root.transform.position=at;
            Color paint=Faction(hostile);
            Piece(root.transform,PrimitiveType.Cylinder,"Pressure vessel",new Vector3(0,2,0),
                new Vector3(2.35f,2.0f,2.35f),paint);
            Piece(root.transform,PrimitiveType.Cylinder,"Steel cap",new Vector3(0,4.1f,0),
                new Vector3(2.55f,.2f,2.55f),Metal);
            for(int i=0;i<4;i++)
            {
                float angle=i*Mathf.PI/2f;
                Piece(root.transform,PrimitiveType.Cylinder,"Cooling system",
                    new Vector3(Mathf.Cos(angle)*3.1f,2.5f,Mathf.Sin(angle)*3.1f),
                    new Vector3(.58f,2.4f,.58f),Metal);
            }
            var coil=Piece(root.transform,PrimitiveType.Sphere,"Pulsing reactor heart",
                new Vector3(0,4.7f,0),new Vector3(.75f,.55f,.75f),hostile?Color.red:Color.cyan);
            root.AddComponent<BlackAlertAdditionalMotion>().pulse=coil;
            return root;
        }
    }

    public sealed class BlackAlertAdditionalMotion:MonoBehaviour
    {
        public Transform leftArm,rightArm,leftLeg,rightLeg,rotatePart,pulse;
        public bool ship;
        private Vector3 previousPosition;
        private float phase;
        private void Start(){previousPosition=transform.position;}
        private void Update()
        {
            phase+=Time.deltaTime;
            float move=(transform.position-previousPosition).magnitude/Mathf.Max(Time.deltaTime,.001f);
            previousPosition=transform.position;
            float walk=move>.15f?Mathf.Sin(phase*9f)*Mathf.Clamp01(move/3f)*25f:0;
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-walk*.6f,0,0);
            if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(walk*.6f,0,0);
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(walk,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-walk,0,0);
            if(pulse!=null)pulse.localScale=Vector3.one*(1f+.08f*Mathf.Sin(phase*4f));
            if(ship)
            {
                var e=transform.localEulerAngles;
                transform.localRotation=Quaternion.Euler(.8f*Mathf.Sin(phase*1.2f),e.y,
                    1.1f*Mathf.Cos(phase*.8f));
            }
        }
    }
}
