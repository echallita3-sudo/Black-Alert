using UnityEngine;
using UnityEngine.Rendering;

namespace BlackAlert
{
    /// <summary>
    /// Actual procedurally tessellated/meshed terrain with height-dependent traversal.
    /// First-engineering-pass geometry; final painted textures, foliage and cliffs are not present.
    /// </summary>
    public sealed class BlackAlertTerrain : MonoBehaviour
    {
        public const float HalfX = 50f, HalfZ = 45f;
        public const float RiverWidth = 2.8f;
        public const float WaterY = 0.05f;
        public const int CellX = 120, CellZ = 108;
        private Material terrainMaterial;

        public float RiverCenter(float z) { return 1.5f * Mathf.Sin(z * .085f); }
        public bool IsWater(Vector3 p) { return Mathf.Abs(p.x-RiverCenter(p.z)) < RiverWidth; }
        public bool IsBridge(Vector3 p)
        {
            return Mathf.Abs(p.x-RiverCenter(p.z)) < 4.2f
                && (Mathf.Abs(p.z - 20f) < 3f || Mathf.Abs(p.z + 20f) < 3f);
        }
        public bool OnRoad(Vector3 p)
        {
            return Mathf.Abs(p.z - 20f) < 2.7f || Mathf.Abs(p.z+20f) < 2.7f;
        }

        public float Height(float x, float z)
        {
            float land = .18f +
                (Mathf.PerlinNoise(x*.075f+101f,z*.075f+15f)-.5f)*.65f;
            float rough = (Mathf.PerlinNoise(x*.21f+39f,z*.21f+11f)-.5f)*.32f;
            float edgeX = Mathf.Clamp01((Mathf.Abs(x)-30f)/15f);
            float edgeZ = Mathf.Clamp01((Mathf.Abs(z)-34f)/10f);
            float mountains = (edgeX*edgeX*9f + edgeZ*edgeZ*5f) *
                (.5f + Mathf.PerlinNoise(x*.09f+100f,z*.09f+110f)*.55f);
            float gully = Mathf.Clamp01(1f - Mathf.Abs(x-RiverCenter(z))/6.3f);
            return land + rough + mountains - gully*1.45f;
        }

        public float SlopeDegrees(Vector3 position)
        {
            var dx=(Height(position.x+1f,position.z)-Height(position.x-1f,position.z))*.5f;
            var dz=(Height(position.x,position.z+1f)-Height(position.x,position.z-1f))*.5f;
            return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
        }

        public bool CanCross(Vector3 at, bool infantry, bool tracked)
        {
            if(Mathf.Abs(at.x)>48f || Mathf.Abs(at.z)>43f) return false;
            float grade=SlopeDegrees(at);
            if(grade>(infantry?47f:tracked?34f:22f))return false;
            return true;
        }

        public float SpeedFactor(Vector3 at, bool infantry, bool tracked)
        {
            if(IsBridge(at)) return 1.15f;
            if(IsWater(at)) return infantry?.48f:tracked?.52f:.22f;
            float grade=SlopeDegrees(at);
            float gradePenalty=infantry?grade*.010f:tracked?grade*.017f:grade*.025f;
            return Mathf.Clamp((OnRoad(at)?1.2f:1f)-gradePenalty,.23f,1.25f);
        }

        public void Build()
        {
            if(transform.Find("3D mountain terrain") != null)return;
            int count=(CellX+1)*(CellZ+1);
            var vertices=new Vector3[count];
            var uv=new Vector2[count];
            var colors=new Color[count];
            var tris=new int[CellX*CellZ*6];
            for(int z=0;z<=CellZ;z++)
                for(int x=0;x<=CellX;x++)
                {
                    float wx=-HalfX+2*HalfX*x/CellX;
                    float wz=-HalfZ+2*HalfZ*z/CellZ;
                    float h=Height(wx,wz);
                    int i=z*(CellX+1)+x;
                    var p=new Vector3(wx,h,wz);
                    vertices[i]=p;
                    uv[i]=new Vector2(wx*.04f,wz*.04f);
                    float slope=SlopeDegrees(p);
                    float granule=Mathf.PerlinNoise(wx*.19f+4f,wz*.19f+9f)*.09f;
                    colors[i]=IsWater(p)?new Color(.27f,.30f,.26f):
                        slope>30f?new Color(.36f+granule,.33f+granule,.29f+granule):
                        h>6f?new Color(.37f,.39f,.32f):
                        new Color(.17f+granule,.29f+granule,.20f+granule);
                }
            int t=0;
            for(int z=0;z<CellZ;z++)
                for(int x=0;x<CellX;x++)
                {
                    int v=z*(CellX+1)+x;
                    tris[t++]=v;tris[t++]=v+CellX+1;tris[t++]=v+1;
                    tris[t++]=v+1;tris[t++]=v+CellX+1;tris[t++]=v+CellX+2;
                }
            var mesh=new Mesh{ name="Black Alert Terrain + Mountains" };
            mesh.indexFormat=IndexFormat.UInt32;
            mesh.vertices=vertices; mesh.triangles=tris; mesh.colors=colors;
            mesh.uv=uv;
            mesh.RecalculateBounds(); mesh.RecalculateNormals();
            var go=new GameObject("3D mountain terrain");
            go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var shader=Shader.Find("BlackAlert/VertexColorTerrain");
            if(shader == null) shader=Shader.Find("Standard");
            terrainMaterial=new Material(shader);
            terrainMaterial.color=Color.white;
            go.AddComponent<MeshRenderer>().sharedMaterial=terrainMaterial;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
            BuildDecor();
        }

        private void BuildDecor()
        {
            var treeBark=NewMat(new Color(.20f,.14f,.11f));
            var leaves=NewMat(new Color(.11f,.25f,.14f));
            var stone=NewMat(new Color(.33f,.34f,.34f));
            var random=new System.Random(731);
            for(int i=0;i<95;i++)
            {
                float x=(float)(random.NextDouble() * 92-46);
                float z=(float)(random.NextDouble() * 80-40);
                var at=new Vector3(x,0,z);
                if(IsWater(at) || IsBridge(at) || OnRoad(at) || SlopeDegrees(at)>29f) continue;
                float ground=Height(x,z);
                var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name="Terrain fir tree trunk";
                trunk.transform.SetParent(transform,false);
                trunk.transform.position=new Vector3(x,ground+1.0f,z);
                trunk.transform.localScale=new Vector3(.3f,1f,.3f);
                trunk.GetComponent<Renderer>().sharedMaterial=treeBark;
                Object.Destroy(trunk.GetComponent<Collider>());
                for(int j=0;j<3;j++)
                {
                    var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crown.name="Fir foliage canopy";
                    crown.transform.SetParent(trunk.transform,false);
                    crown.transform.localPosition=new Vector3(0,.35f+j*.60f,0);
                    crown.transform.localScale=new Vector3(5-j*.65f,.5f,5-j*.65f);
                    crown.GetComponent<Renderer>().sharedMaterial=leaves;
                    Object.Destroy(crown.GetComponent<Collider>());
                }
            }
            for(int i=0;i<33;i++)
            {
                float x=(float)(random.NextDouble()*90-45);
                float z=(float)(random.NextDouble()*80-40);
                var rock=GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name="Rock / collision prop";
                rock.transform.SetParent(transform,false);
                rock.transform.position=new Vector3(x,Height(x,z)+.6f,z);
                rock.transform.localScale=new Vector3(1.4f,1.2f,1.7f);
                rock.transform.rotation=Quaternion.Euler(12f,(float)random.NextDouble()*180f,10f);
                rock.GetComponent<Renderer>().sharedMaterial=stone;
            }
        }
        private static Material NewMat(Color color)
        {
            var shader=Shader.Find("Standard");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            var m=new Material(shader);
            m.color=color;
            return m;
        }
    }
}
