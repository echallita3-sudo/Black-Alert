using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Original runtime triangle meshes. Not screenshots, sprites, or imported prefabs.
    /// These meshes are mechanical art blockouts suitable for iterative texturing/LOD work.
    /// Meshes are shared between units to avoid duplicating vertices in mobile memory.
    /// </summary>
    public static class BlackAlertMeshGeometry
    {
        public struct BodySection
        {
            public float z, halfWidth, bottom, top, bevel;
            public BodySection(float z,float halfWidth,float bottom,float top,float bevel)
            {
                this.z=z;this.halfWidth=halfWidth;this.bottom=bottom;this.top=top;this.bevel=bevel;
            }
        }
        public struct TubeSection
        {
            public float z,rx,ry,cy;
            public TubeSection(float z,float rx,float ry,float cy=0f)
            {this.z=z;this.rx=rx;this.ry=ry;this.cy=cy;}
        }

        private static Mesh FinalizeMesh(string name,List<Vector3> vertices,List<int> triangles)
        {
            var mesh=new Mesh();
            mesh.name=name;
            if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        public static Mesh AngularHull(string name, params BodySection[] segments)
        {
            if(segments==null || segments.Length<2)throw new ArgumentException("A hull needs at least two sections.");
            const int sides=8;
            var verts=new List<Vector3>(segments.Length*sides);
            var triangles=new List<int>();
            foreach(var s in segments)
            {
                float b=Mathf.Clamp(s.bevel,.001f,Mathf.Min(s.halfWidth*.45f,(s.top-s.bottom)*.45f));
                verts.Add(new Vector3(-s.halfWidth+b,s.bottom,s.z));
                verts.Add(new Vector3( s.halfWidth-b,s.bottom,s.z));
                verts.Add(new Vector3( s.halfWidth,s.bottom+b,s.z));
                verts.Add(new Vector3( s.halfWidth,s.top-b,s.z));
                verts.Add(new Vector3( s.halfWidth-b,s.top,s.z));
                verts.Add(new Vector3(-s.halfWidth+b,s.top,s.z));
                verts.Add(new Vector3(-s.halfWidth,s.top-b,s.z));
                verts.Add(new Vector3(-s.halfWidth,s.bottom+b,s.z));
            }
            for(int r=0;r<segments.Length-1;r++)
                for(int s=0;s<sides;s++)
                {
                    int a=r*sides+s,b=r*sides+(s+1)%sides;
                    int c=a+sides,d=b+sides;
                    triangles.Add(a);triangles.Add(b);triangles.Add(d);
                    triangles.Add(a);triangles.Add(d);triangles.Add(c);
                }
            // End caps, with correct outward winding.
            for(int s=1;s<sides-1;s++)
            {triangles.Add(0);triangles.Add(s+1);triangles.Add(s);}
            int baseIndex=(segments.Length-1)*sides;
            for(int s=1;s<sides-1;s++)
            {triangles.Add(baseIndex);triangles.Add(baseIndex+s);triangles.Add(baseIndex+s+1);}
            return FinalizeMesh(name,verts,triangles);
        }

        public static Mesh SmoothFuselage(string name,int sides,params TubeSection[] rings)
        {
            if(sides<6 || rings==null || rings.Length<2)
                throw new ArgumentException("Fuselage requires at least 6 radial samples and two rings.");
            var verts=new List<Vector3>(rings.Length*sides+2);
            var tri=new List<int>();
            foreach(var ring in rings)
                for(int s=0;s<sides;s++)
                {
                    float a=2f*Mathf.PI*s/sides;
                    verts.Add(new Vector3(Mathf.Cos(a)*ring.rx,
                        ring.cy+Mathf.Sin(a)*ring.ry,ring.z));
                }
            for(int r=0;r<rings.Length-1;r++)
                for(int s=0;s<sides;s++)
                {
                    int a=r*sides+s,b=r*sides+(s+1)%sides,c=a+sides,d=b+sides;
                    // Around the tube the winding must point away from its axis.
                    tri.Add(a);tri.Add(b);tri.Add(d);
                    tri.Add(a);tri.Add(d);tri.Add(c);
                }
            int low=verts.Count;
            verts.Add(new Vector3(0,rings[0].cy,rings[0].z));
            int high=verts.Count;
            verts.Add(new Vector3(0,rings[rings.Length-1].cy,rings[rings.Length-1].z));
            for(int s=0;s<sides;s++)
            {
                tri.Add(low);tri.Add((s+1)%sides);tri.Add(s);
                int a=(rings.Length-1)*sides+s,b=(rings.Length-1)*sides+(s+1)%sides;
                tri.Add(high);tri.Add(a);tri.Add(b);
            }
            return FinalizeMesh(name,verts,tri);
        }

        public static Mesh MetalPlate(string name,float thickness,params Vector2[] polygon)
        {
            // Polygon in x-z coordinates, triangulated fan, intended for convex armor/wings.
            if(polygon==null || polygon.Length<3)
                throw new ArgumentException("Plate requires at least three corners.");
            int n=polygon.Length;
            var verts=new List<Vector3>(n*2);
            var tris=new List<int>();
            for(int i=0;i<n;i++)verts.Add(new Vector3(polygon[i].x,thickness*.5f,polygon[i].y));
            for(int i=0;i<n;i++)verts.Add(new Vector3(polygon[i].x,-thickness*.5f,polygon[i].y));
            float area=0;
            for(int i=0;i<n;i++)
            {
                Vector2 a=polygon[i],b=polygon[(i+1)%n];
                area+=a.x*b.y-b.x*a.y;
            }
            bool clockwise=area<0;
            for(int i=1;i<n-1;i++)
            {
                if(clockwise){tris.Add(0);tris.Add(i);tris.Add(i+1);}
                else {tris.Add(0);tris.Add(i+1);tris.Add(i);}
                if(clockwise){tris.Add(n);tris.Add(n+i+1);tris.Add(n+i);}
                else{tris.Add(n);tris.Add(n+i);tris.Add(n+i+1);}
            }
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                if(clockwise)
                {
                    tris.Add(i);tris.Add(n+i);tris.Add(n+j);
                    tris.Add(i);tris.Add(n+j);tris.Add(j);
                }
                else
                {
                    tris.Add(i);tris.Add(n+j);tris.Add(n+i);
                    tris.Add(i);tris.Add(j);tris.Add(n+j);
                }
            }
            return FinalizeMesh(name,verts,tris);
        }

        public static Mesh TrackBelt(string name,float width,float halfLength,
                                    float bottom,float top,float rounded)
        {
            // Continuous extruded track loop with rounded front/rear rollers.
            var yz=new List<Vector2>();
            int cornerSegments=5;
            Vector2[] centers={
                new Vector2(-halfLength+rounded,bottom+rounded),
                new Vector2(halfLength-rounded,bottom+rounded),
                new Vector2(halfLength-rounded,top-rounded),
                new Vector2(-halfLength+rounded,top-rounded)
            };
            for(int c=0;c<4;c++)
            {
                float begin=(c-1)*Mathf.PI/2f;
                for(int s=0;s<=cornerSegments;s++)
                {
                    float a=begin+s*Mathf.PI/(2f*cornerSegments);
                    yz.Add(centers[c]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rounded);
                }
            }
            int n=yz.Count;
            var verts=new List<Vector3>(n*2+2);
            var tri=new List<int>();
            for(int i=0;i<n;i++)verts.Add(new Vector3(-width*.5f,yz[i].y,yz[i].x));
            for(int i=0;i<n;i++)verts.Add(new Vector3(width*.5f,yz[i].y,yz[i].x));
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                tri.Add(i);tri.Add(j);tri.Add(i+n);
                tri.Add(j);tri.Add(j+n);tri.Add(i+n);
            }
            int left=verts.Count;verts.Add(new Vector3(-width*.5f,(bottom+top)*.5f,0));
            int right=verts.Count;verts.Add(new Vector3(width*.5f,(bottom+top)*.5f,0));
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                tri.Add(left);tri.Add(j);tri.Add(i);
                tri.Add(right);tri.Add(i+n);tri.Add(j+n);
            }
            return FinalizeMesh(name,verts,tri);
        }
    }
}
