using BlackAlert.Simulation;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Physical ore truck drives between a mine and refinery. Cargo is credited ONLY after
    /// successful return to the refinery. This is a first-pass kinematic implementation;
    /// later art must add independent suspension, excavation, treads, and path finding.
    /// </summary>
    public sealed class BlackAlertOreHarvester : MonoBehaviour
    {
        private enum Task { ToOre, Loading, ToRefinery, Unloading, Parked }
        private Task task;
        private RtsEconomy economy;
        private BlackAlertTerrain terrain;
        private string oreId;
        private Vector3 refinery;
        private Vector3 mine;
        private int cargo;
        private float workTimer;
        private Transform drill;
        private Transform[] wheels;
        private Renderer orePileRenderer;
        private GameObject worldVisualRoot;
        private Material body;
        private float traveled;

        public void Configure(RtsEconomy simulation,BlackAlertTerrain physicalTerrain,
                              string fieldId,Vector3 refineryWorld,Vector3 oreWorld)
        {
            economy=simulation;
            terrain=physicalTerrain;
            oreId=fieldId;
            refinery=refineryWorld;
            mine=oreWorld;
            task=Task.ToOre;
            worldVisualRoot=new GameObject("Black Alert Ore and Refinery Props");
            wheels=new Transform[8];
            body=Mat(new Color(.14f,.46f,.67f));
            transform.position=WorldHeight(refinery);
            BuildTruck();
            BuildOrePatch();
            BuildRefinery();
        }
        private Vector3 WorldHeight(Vector3 p)
        {
            p.y=terrain!=null?terrain.Height(p.x,p.z)+.67f:.67f;
            return p;
        }
        private static Material Mat(Color c)
        {
            var shader=Shader.Find("Standard");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            var m=new Material(shader);m.color=c;
            return m;
        }
        private GameObject Part(PrimitiveType shape,string label,Vector3 local,Vector3 size,Material color)
        {
            var p=GameObject.CreatePrimitive(shape);
            p.name=label;
            p.transform.SetParent(transform,false);
            p.transform.localPosition=local;
            p.transform.localScale=size;
            p.GetComponent<Renderer>().sharedMaterial=color;
            var col=p.GetComponent<Collider>();if(col!=null)Destroy(col);
            return p;
        }
        private void BuildTruck()
        {
            gameObject.name="Azure Physical Ore Harvester";
            var matSteel=Mat(new Color(.23f,.27f,.31f));
            var matRubber=Mat(new Color(.06f,.08f,.1f));
            Part(PrimitiveType.Cube,"Heavy armored cabin",new Vector3(0,.5f,1),
                new Vector3(2.5f,1.6f,2),body);
            Part(PrimitiveType.Cube,"Mining cargo hopper",new Vector3(0,.43f,-1.1f),
                new Vector3(2.6f,1.35f,2.2f),matSteel);
            Part(PrimitiveType.Cube,"Hopper roof",new Vector3(0,1.23f,-1.1f),
                new Vector3(2.5f,.13f,2.2f),body);
            Part(PrimitiveType.Cube,"Safety windshield",new Vector3(0,1.04f,2.04f),
                new Vector3(2,.6f,.12f),Mat(new Color(.14f,.42f,.54f)));
            for(int side=-1;side<=1;side+=2)
            for(int axle=0;axle<4;axle++)
            {
                int idx=(side<0?0:4)+axle;
                var w=Part(PrimitiveType.Cylinder,"Animated truck wheel "+idx,
                    new Vector3(side*1.42f,-.45f,-1.85f+axle*1.22f),
                    new Vector3(.58f,.24f,.58f),matRubber);
                w.transform.localRotation=Quaternion.Euler(0,0,90);
                wheels[idx]=w.transform;
            }
            var excavator=new GameObject("Rotating ore intake drill");
            excavator.transform.SetParent(transform,false);
            excavator.transform.localPosition=new Vector3(0,-.13f,2.3f);
            var drillHead=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drillHead.name="Excavator roller";
            drillHead.transform.SetParent(excavator.transform,false);
            drillHead.transform.localScale=new Vector3(.58f,1.35f,.58f);
            drillHead.transform.localRotation=Quaternion.Euler(0,0,90);
            drillHead.GetComponent<Renderer>().sharedMaterial=matSteel;
            Destroy(drillHead.GetComponent<Collider>());
            drill=excavator.transform;
            gameObject.AddComponent<BoxCollider>().size=new Vector3(3.2f,2.3f,4.5f);
        }

        private void BuildOrePatch()
        {
            var patch=new GameObject("Harvestable Azure ore node / finite volume");
            patch.transform.SetParent(worldVisualRoot.transform,false);
            patch.transform.position=new Vector3(mine.x,terrain.Height(mine.x,mine.z),mine.z);
            var mat=Mat(new Color(.54f,.59f,.71f));
            for(int i=0;i<11;i++)
            {
                float angle=i*Mathf.PI*2/11f;
                var p=GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name="Ore seam "+(i+1);
                p.transform.SetParent(patch.transform,false);
                p.transform.localPosition=new Vector3(Mathf.Cos(angle)*2.2f,.55f,Mathf.Sin(angle)*2.2f);
                p.transform.localRotation=Quaternion.Euler(23f,i*34f,12f);
                p.transform.localScale=new Vector3(.8f,.95f,.75f);
                p.GetComponent<Renderer>().sharedMaterial=mat;
                Destroy(p.GetComponent<Collider>());
            }
            orePileRenderer=patch.GetComponentInChildren<Renderer>();
        }

        private void BuildRefinery()
        {
            var structure=new GameObject("Azure Ore Refinery / unloading depot");
            structure.transform.SetParent(worldVisualRoot.transform,false);
            structure.transform.position=new Vector3(refinery.x,terrain.Height(refinery.x,refinery.z),refinery.z);
            var mat=Mat(new Color(.23f,.34f,.47f));
            var foundation=GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name="Refinery building";
            foundation.transform.SetParent(structure.transform,false);
            foundation.transform.localPosition=new Vector3(-4.3f,1.1f,0);
            foundation.transform.localScale=new Vector3(6.1f,2.2f,5.7f);
            foundation.GetComponent<Renderer>().sharedMaterial=mat;
            for(int i=0;i<3;i++)
            {
                var tower=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name="Ore refinery silo";
                tower.transform.SetParent(structure.transform,false);
                tower.transform.localPosition=new Vector3(-6f+i*1.3f,3f,-.5f);
                tower.transform.localScale=new Vector3(.85f,2.0f,.85f);
                tower.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.18f,.23f,.29f));
                Destroy(tower.GetComponent<Collider>());
            }
        }

        private void OnDestroy()
        {
            if(worldVisualRoot!=null)Destroy(worldVisualRoot);
        }

        private void Update()
        {
            if(economy==null)return;
            switch(task)
            {
                case Task.ToOre:
                    if(DriveToward(mine))
                    {
                        task=Task.Loading;
                        workTimer=2.3f;
                    }
                    break;
                case Task.Loading:
                    drill.Rotate(Vector3.forward,Time.deltaTime*360f,Space.Self);
                    workTimer-=Time.deltaTime;
                    if(workTimer<=0)
                    {
                        cargo=economy.LoadOre(oreId,110);
                        task=cargo>0?Task.ToRefinery:Task.Parked;
                    }
                    break;
                case Task.ToRefinery:
                    if(DriveToward(refinery))
                    {
                        task=Task.Unloading;
                        workTimer=1.8f;
                    }
                    break;
                case Task.Unloading:
                    workTimer-=Time.deltaTime;
                    if(workTimer<=0)
                    {
                        economy.DeliverOre(cargo);
                        cargo=0;
                        task=Task.ToOre;
                    }
                    break;
            }
        }

        private bool DriveToward(Vector3 destination)
        {
            Vector3 next=WorldHeight(destination);
            Vector3 delta=next-transform.position;
            delta.y=0;
            if(delta.sqrMagnitude<3f)return true;
            Vector3 flat=delta.normalized;
            float speed=3.7f;
            if(terrain!=null)speed*=terrain.SpeedFactor(transform.position,false,true);
            var updated=transform.position+flat*(speed*Time.deltaTime);
            if(terrain!=null && !terrain.CanCross(updated,false,true))
                return false;
            transform.position=WorldHeight(updated);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat),
                Time.deltaTime*3.8f);
            traveled+=Time.deltaTime*speed;
            if(wheels!=null)foreach(var wheel in wheels)
                if(wheel!=null)wheel.Rotate(Vector3.up,Time.deltaTime*190f,Space.Self);
            return false;
        }
    }
}
