using UnityEngine;

namespace BlackAlert
{
    // Original editable 3D engine-test meshes. These are NOT the approved final AAA models.
    // Named independent transforms provide a path for rigged art replacement.
    public static class BlackAlertModelFactory
    {
        private static readonly Color Steel = new Color(.18f,.22f,.28f);
        private static readonly Color Rubber = new Color(.06f,.07f,.09f);
        private static readonly Color Glass = new Color(.16f,.36f,.47f);
        private static Material Material(Color c)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader);
            m.color = c;
            return m;
        }
        private static GameObject Part(Transform parent, PrimitiveType shape, string name,
            Vector3 pos, Vector3 scale, Color color, Vector3 euler = default(Vector3))
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject CreateTank(Vector3 world, bool red)
        {
            Color paint = red ? new Color(.70f,.13f,.13f) : new Color(.10f,.46f,.80f);
            var root = new GameObject(red ? "Crimson Battle Tank" : "Azure Battle Tank");
            root.transform.position = world;
            var hit = root.AddComponent<BoxCollider>();
            hit.center = new Vector3(0,0.25f,0);
            hit.size = new Vector3(2.9f,1.9f,4.4f);
            Part(root.transform, PrimitiveType.Cube, "Angled armored hull", new Vector3(0,0,0),
                new Vector3(2.8f,.9f,4.2f), paint);
            Part(root.transform, PrimitiveType.Cube, "Upper glacis", new Vector3(0,.55f,-.2f),
                new Vector3(2.25f,.65f,2.95f), Steel);
            for(int side=-1; side<=1; side+=2)
            {
                Part(root.transform,PrimitiveType.Cube,"Track enclosure",new Vector3(side*1.26f,-.35f,0),
                    new Vector3(.65f,.75f,4.1f),Rubber);
                for (int i=-2;i<=2;i++)
                    Part(root.transform,PrimitiveType.Cylinder,"Road wheel "+i,new Vector3(side*1.56f,-.44f,i*.78f),
                        new Vector3(.53f,.14f,.53f),Steel,new Vector3(0,0,90));
            }
            var turret = new GameObject("Independent rotating turret");
            turret.transform.SetParent(root.transform,false);
            turret.transform.localPosition = new Vector3(0,.94f,.15f);
            Part(turret.transform,PrimitiveType.Cylinder,"Armored turret",Vector3.zero,
                new Vector3(1.5f,.42f,1.45f),paint);
            Part(turret.transform,PrimitiveType.Cube,"Main cannon",new Vector3(0,.03f,1.58f),
                new Vector3(.3f,.3f,3.3f),Steel);
            Part(turret.transform,PrimitiveType.Cube,"Turret sight",new Vector3(.55f,.53f,.32f),
                new Vector3(.38f,.45f,.6f),Glass);
            var selection = new GameObject("Selection Ring");
            selection.transform.SetParent(root.transform,false);
            selection.transform.localPosition = new Vector3(0,-.49f,0);
            var positions = new Vector3[96];
            var triangles = new int[144];
            for(int i=0;i<48;i++)
            {
                float angle=(i / 48f)*Mathf.PI*2;
                var radial=new Vector3(Mathf.Cos(angle),.01f,Mathf.Sin(angle));
                positions[i*2]=radial*2.65f;
                positions[i*2+1]=radial*2.45f;
                int n=(i+1)%48;
                int j=i*3;
                triangles[j]=i*2; triangles[j+1]=n*2; triangles[j+2]=i*2+1;
            }
            // Two triangle strips for a clear ring visible at gameplay zoom.
            var tri=new int[48*6];
            for(int i=0;i<48;i++)
            {
                int n=(i+1)%48, j=i*6;
                tri[j]=i*2; tri[j+1]=n*2; tri[j+2]=i*2+1;
                tri[j+3]=i*2+1;tri[j+4]=n*2;tri[j+5]=n*2+1;
            }
            var selectionMesh = new Mesh { vertices=positions, triangles=tri };
            selectionMesh.RecalculateNormals();
            selection.AddComponent<MeshFilter>().sharedMesh=selectionMesh;
            selection.AddComponent<MeshRenderer>().sharedMaterial=Material(new Color(.13f,.95f,1f));
            selection.SetActive(false);
            var animate = root.AddComponent<BlackAlertVisualMotion>();
            animate.turret = turret.transform;
            return root;
        }

        public static GameObject CreateHelicopter(Vector3 world, bool red)
        {
            Color paint = red ? new Color(.68f,.12f,.12f) : new Color(.08f,.42f,.69f);
            var root = new GameObject(red ? "Crimson Attack Helicopter" : "Azure Attack Helicopter");
            root.transform.position = world;
            var hit = root.AddComponent<BoxCollider>();
            hit.size = new Vector3(3.8f,2.2f,8f);
            Part(root.transform,PrimitiveType.Capsule,"Armored fuselage",Vector3.zero,
                new Vector3(2.8f,1.25f,4.5f),paint,new Vector3(90,0,0));
            Part(root.transform,PrimitiveType.Cube,"Cockpit",new Vector3(0,.42f,1.5f),
                new Vector3(1.8f,.8f,2.0f),Glass);
            Part(root.transform,PrimitiveType.Cube,"Tail",new Vector3(0,.25f,-3.55f),
                new Vector3(.35f,.32f,5.3f),Steel);
            for (int side=-1;side<=1;side+=2)
            {
                Part(root.transform,PrimitiveType.Cube,"Weapon wing",new Vector3(side*2.15f,-.2f,-.15f),
                    new Vector3(2.6f,.18f,1.2f),paint);
                Part(root.transform,PrimitiveType.Cylinder,"Rocket pod",new Vector3(side*2.8f,-.47f,.23f),
                    new Vector3(.46f,1.1f,.46f),Rubber,new Vector3(90,0,0));
            }
            var rotor = new GameObject("Main rotor spinning hub");
            rotor.transform.SetParent(root.transform,false);
            rotor.transform.localPosition = new Vector3(0,1.2f,-.3f);
            Part(rotor.transform,PrimitiveType.Cylinder,"Rotor cap",Vector3.zero,
                new Vector3(.6f,.1f,.6f),Steel);
            Part(rotor.transform,PrimitiveType.Cube,"Rotor blade A",Vector3.zero,
                new Vector3(.22f,.08f,8.9f),Rubber);
            Part(rotor.transform,PrimitiveType.Cube,"Rotor blade B",Vector3.zero,
                new Vector3(8.9f,.08f,.22f),Rubber);
            var animate = root.AddComponent<BlackAlertVisualMotion>();
            animate.rotor = rotor.transform;
            animate.hover = true;
            return root;
        }

        public static GameObject CreateFactory(Vector3 at, bool red)
        {
            Color color = red ? new Color(.47f,.16f,.16f) : new Color(.18f,.33f,.49f);
            var root = new GameObject(red ? "Crimson Animated War Factory" : "Azure Animated War Factory");
            root.transform.position = at;
            Part(root.transform,PrimitiveType.Cube,"Reinforced industrial slab",new Vector3(0,0,0),
                new Vector3(8,.7f,9),Steel);
            Part(root.transform,PrimitiveType.Cube,"Factory shell",new Vector3(0,3,-1),
                new Vector3(7,5.5f,7),color);
            Part(root.transform,PrimitiveType.Cube,"Hazard-lined roof",new Vector3(0,5.9f,-1),
                new Vector3(8,.7f,8),Steel);
            for(int side=-1;side<=1;side+=2)
                Part(root.transform,PrimitiveType.Cylinder,"Exhaust chimney",
                    new Vector3(side*2.4f,7,-3),new Vector3(.5f,2.3f,.5f),Steel);
            var gate = new GameObject("Animated factory door");
            gate.transform.SetParent(root.transform,false);
            gate.transform.localPosition = new Vector3(0,2.2f,2.65f);
            Part(gate.transform,PrimitiveType.Cube,"Segmented blast gate",Vector3.zero,
                new Vector3(5,3.7f,.25f),Rubber);
            var motion=root.AddComponent<BlackAlertVisualMotion>();
            motion.door=gate.transform;
            return root;
        }
    }

    public sealed class BlackAlertVisualMotion : MonoBehaviour
    {
        public Transform turret;
        public Transform rotor;
        public Transform door;
        public bool hover;
        private Vector3 startingPosition;
        private Vector3 gateStartingLocal;
        private float phase;
        private void Start()
        {
            startingPosition=transform.position;
            if (door!=null) gateStartingLocal=door.localPosition;
        }
        private void Update()
        {
            phase+=Time.deltaTime;
            if(rotor!=null) rotor.Rotate(Vector3.up,1100f*Time.deltaTime,Space.Self);
            if(door!=null)
                door.localPosition=gateStartingLocal + Vector3.up*(1.0f+.85f*Mathf.Sin(phase*1.1f));
            if(hover) transform.position=new Vector3(transform.position.x,
                startingPosition.y+.1f*Mathf.Sin(phase*4f),transform.position.z);
        }
    }
}
