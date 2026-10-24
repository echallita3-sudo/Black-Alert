using System.Collections.Generic;
using UnityEngine;

namespace BlackAlert
{
    // Small, self-contained testbed for scene boot, camera, simple physics and unit commands.
    // Not a substitute for the full production battle simulation or final animated models.
    public sealed class BattlefieldPrototype : MonoBehaviour
    {
        private sealed class Agent
        {
            public GameObject model;
            public bool enemy;
            public float hp = 100f;
            public Vector3 goal;
            public float cooldown;
            public float speed;
            public bool selected;
        }

        private readonly List<Agent> agents = new List<Agent>();
        private Camera cam;
        private Vector3 camFocus = new Vector3(0, 0, 0);
        private float camHeight = 53f;
        private Agent selected;
        private bool dragCamera;
        private Vector2 lastPointer;
        private Material friendMat;
        private Material foeMat;
        private Material earthMat;
        private Material waterMat;
        private int friendlyHQ = 1;
        private int enemyHQ = 1;
        private float battleTime;

        private Material NewMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private GameObject Cube(string label, Vector3 position, Vector3 scale, Material mat, Transform parent = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            if (parent != null) cube.transform.SetParent(parent, true);
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            friendMat = NewMaterial(new Color(0.12f, 0.6f, 0.95f));
            foeMat = NewMaterial(new Color(0.86f, 0.19f, 0.16f));
            earthMat = NewMaterial(new Color(0.20f, 0.31f, 0.25f));
            waterMat = NewMaterial(new Color(0.08f, 0.35f, 0.6f));
            BuildGround();
            BuildLighting();
            BuildBases();
            for (int i = 0; i < 7; i++)
            {
                Spawn(new Vector3(-22 + i * 2.5f, 0, -9 + i % 3 * 3f), false);
                Spawn(new Vector3(18 + i * 2.5f, 0, 7 + i % 3 * 3f), true);
            }
        }

        private void BuildGround()
        {
            Cube("Walkable battlefield", new Vector3(0,-0.6f,0), new Vector3(100,1,90), earthMat);
            // Narrow ford intentionally slows ground units; bank elevations are visible.
            Cube("Shallow river", new Vector3(0,-0.015f,0), new Vector3(5,0.05f,89), waterMat);
            Cube("West bridge", new Vector3(0,0.15f,-20), new Vector3(7,0.35f,5), NewMaterial(new Color(0.5f,0.47f,0.4f)));
            Cube("East bridge", new Vector3(0,0.15f,20), new Vector3(7,0.35f,5), NewMaterial(new Color(0.5f,0.47f,0.4f)));
            for (int i = 0; i < 14; i++)
            {
                float z = -39 + i * 6f;
                float x = i % 2 == 0 ? -43f : 43f;
                Cube("Rocky ridge", new Vector3(x,1.5f,z), new Vector3(8,3 + i % 3 * 2,5), NewMaterial(new Color(0.30f,0.29f,0.26f)));
            }
        }

        private void BuildLighting()
        {
            var l = new GameObject("Sun").AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.5f;
            l.transform.rotation = Quaternion.Euler(48,-30,0);
            var go = new GameObject("RTS Camera");
            cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50;
            AlignCamera();
        }

        private void BuildBases()
        {
            Cube("Azure HQ", new Vector3(-33,3,-2), new Vector3(7,6,9), friendMat);
            Cube("Crimson HQ", new Vector3(33,3,2), new Vector3(7,6,9), foeMat);
            for (int i=0;i<3;i++)
            {
                Cube("Azure Factory",new Vector3(-33,2,12+i*9),new Vector3(6,4,6),friendMat);
                Cube("Crimson Factory",new Vector3(33,2,-12-i*9),new Vector3(6,4,6),foeMat);
            }
        }

        private void Spawn(Vector3 p, bool hostile)
        {
            var body = Cube(hostile ? "Crimson Tank" : "Azure Tank", p + Vector3.up*0.6f,
                new Vector3(1.8f,1.2f,2.7f), hostile ? foeMat : friendMat);
            var turret = Cube("Turret",p + Vector3.up * 1.35f,new Vector3(1.2f,0.5f,1.2f),
                NewMaterial(new Color(0.25f,0.26f,0.29f)),body.transform);
            turret.transform.localPosition = new Vector3(0,0.68f,0);
            var barrel = Cube("Barrel",p + Vector3.up * 1.3f + Vector3.forward, new Vector3(0.25f,0.25f,2.2f),
                turret.GetComponent<Renderer>().sharedMaterial,turret.transform);
            barrel.transform.localPosition = new Vector3(0,0,1.1f);
            agents.Add(new Agent {model=body,enemy=hostile,goal=p,speed=hostile?3.1f:3.8f});
        }

        private void Update()
        {
            battleTime += Time.deltaTime;
            UpdateInput();
            Simulate();
        }

        private void UpdateInput()
        {
            if (cam == null) return;
            if (Input.touchCount >= 2)
            {
                var a=Input.GetTouch(0); var b=Input.GetTouch(1);
                var prev=(a.position-a.deltaPosition)-(b.position-b.deltaPosition);
                float before=prev.magnitude, now=(a.position-b.position).magnitude;
                camHeight=Mathf.Clamp(camHeight+(before-now)*0.05f,20f,100f);
                camFocus -= new Vector3((a.deltaPosition.x+b.deltaPosition.x)*0.025f,0,(a.deltaPosition.y+b.deltaPosition.y)*0.025f);
                AlignCamera();
                return;
            }
            if (Input.touchCount == 1)
            {
                var t=Input.GetTouch(0);
                if (t.phase==TouchPhase.Began) {lastPointer=t.position;dragCamera=false;}
                else if (t.phase==TouchPhase.Moved)
                {
                    if ((t.position-lastPointer).magnitude > 7f) dragCamera=true;
                    if (dragCamera) {camFocus -= new Vector3(t.deltaPosition.x*0.025f,0,t.deltaPosition.y*0.025f); AlignCamera();}
                }
                else if (t.phase==TouchPhase.Ended && !dragCamera) SelectOrOrder(t.position);
                return;
            }
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0)) SelectOrOrder(Input.mousePosition);
            camHeight=Mathf.Clamp(camHeight-Input.mouseScrollDelta.y*3f,20f,100f);
            AlignCamera();
#endif
        }

        private void AlignCamera()
        {
            if (cam == null) return;
            camFocus.x=Mathf.Clamp(camFocus.x,-45,45);
            camFocus.z=Mathf.Clamp(camFocus.z,-39,39);
            cam.transform.position=camFocus + new Vector3(0,camHeight,-camHeight*0.65f);
            cam.transform.rotation=Quaternion.LookRotation(camFocus-cam.transform.position,Vector3.up);
        }

        private void SelectOrOrder(Vector2 point)
        {
            if (!Physics.Raycast(cam.ScreenPointToRay(point),out RaycastHit hit,500f)) return;
            Agent clicked=null;
            foreach(var a in agents)
            {
                if(a.model != null && hit.collider != null && hit.collider.gameObject.transform.IsChildOf(a.model.transform))
                    {clicked=a;break;}
            }
            if(clicked!=null && !clicked.enemy) {selected=clicked;return;}
            if(selected!=null && selected.model!=null)
            {
                selected.goal=new Vector3(Mathf.Clamp(hit.point.x,-47,47),0,Mathf.Clamp(hit.point.z,-43,43));
                if(clicked!=null && clicked.enemy) selected.goal=clicked.model.transform.position;
            }
        }

        private void Simulate()
        {
            foreach(var a in agents)
            {
                if(a.model==null || a.hp<=0) continue;
                Agent enemy=null; float best=999999;
                foreach(var b in agents)
                {
                    if(b.enemy==a.enemy || b.hp<=0 || b.model==null) continue;
                    var d=(b.model.transform.position-a.model.transform.position).sqrMagnitude;
                    if(d<best) {best=d;enemy=b;}
                }
                if(a.enemy && enemy!=null && best<190f) a.goal=enemy.model.transform.position;
                var position=a.model.transform.position;
                var flatGoal=new Vector3(a.goal.x,0.6f,a.goal.z);
                var diff=flatGoal-position;
                if(diff.sqrMagnitude>4.5f && (enemy==null || best>70f || !a.enemy))
                {
                    float inRiver=Mathf.Abs(position.x)<3.5f?0.5f:1f;
                    position=Vector3.MoveTowards(position,flatGoal,Time.deltaTime*a.speed*inRiver);
                    a.model.transform.position=position;
                    if(diff.sqrMagnitude>0.1f) a.model.transform.rotation=Quaternion.Slerp(a.model.transform.rotation,Quaternion.LookRotation(diff),Time.deltaTime*4f);
                }
                a.cooldown-=Time.deltaTime;
                if(enemy!=null && best < 120f && a.cooldown<=0)
                {
                    a.cooldown=1.3f;
                    enemy.hp-=18f;
                    if(enemy.hp<=0) {Object.Destroy(enemy.model);enemy.model=null;}
                    SpawnTracer(position+Vector3.up,enemy.model!=null?enemy.model.transform.position+Vector3.up:position+Vector3.forward*3);
                }
            }
        }

        private void SpawnTracer(Vector3 start,Vector3 end)
        {
            var projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name="Impact flash";
            projectile.transform.position=end;
            projectile.transform.localScale=Vector3.one*1.2f;
            projectile.GetComponent<Renderer>().sharedMaterial=NewMaterial(new Color(1f,0.48f,0.05f));
            Object.Destroy(projectile,0.22f);
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(15, 20, 170, 55),"BLACK ALERT")) { }
            GUI.Label(new Rect(20, 82, 420, 30), "SKIRMISH TECH TEST — NOT FINAL ART");
            GUI.Label(new Rect(20, 105, 450, 30), "Select a blue tank, then tap ground to move");
            GUI.Label(new Rect(20, 128, 450, 30), "Elapsed: "+battleTime.ToString("0")+"s / Units: "+agents.Count);
            if(GUI.Button(new Rect(20, 165, 200, 55), "DEPLOY TANK"))
                Spawn(new Vector3(-23,0,-12+Random.Range(-12,12)),false);
        }
    }
}
