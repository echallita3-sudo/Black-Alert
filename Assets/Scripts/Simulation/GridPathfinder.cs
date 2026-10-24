using System;
using System.Collections.Generic;

namespace BlackAlert.Simulation
{
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int X,Y;
        public GridCell(int x,int y){X=x;Y=y;}
        public bool Equals(GridCell other) => X==other.X && Y==other.Y;
        public override bool Equals(object obj) => obj is GridCell p && Equals(p);
        public override int GetHashCode() => (X*397) ^ Y;
        public override string ToString() => X+","+Y;
    }

    /// <summary>
    /// Deterministic A* with 8-way adjacency and no diagonal corner-cutting.
    /// Passability and terrain penalties supplied by the RTS physics world.
    /// Pure C# so map navigation can be checked without Unity build minutes.
    /// </summary>
    public sealed class GridPathfinder
    {
        private struct OpenCell
        {
            public int Index;
            public double Score;
            public int Serial;
        }
        private sealed class MinHeap
        {
            private readonly List<OpenCell> nodes=new List<OpenCell>();
            private static bool Less(OpenCell a,OpenCell b) =>
                a.Score<b.Score || (a.Score==b.Score && a.Serial<b.Serial);
            public int Count => nodes.Count;
            public void Push(OpenCell node)
            {
                nodes.Add(node);
                int i=nodes.Count-1;
                while(i>0)
                {
                    int parent=(i-1)/2;
                    if(!Less(nodes[i],nodes[parent]))break;
                    var tmp=nodes[i];nodes[i]=nodes[parent];nodes[parent]=tmp;i=parent;
                }
            }
            public OpenCell Pop()
            {
                var result=nodes[0];
                var last=nodes[nodes.Count-1];
                nodes.RemoveAt(nodes.Count-1);
                if(nodes.Count==0)return result;
                nodes[0]=last;
                int i=0;
                while(true)
                {
                    int l=i*2+1,r=l+1,best=i;
                    if(l<nodes.Count && Less(nodes[l],nodes[best]))best=l;
                    if(r<nodes.Count && Less(nodes[r],nodes[best]))best=r;
                    if(best==i)break;
                    var t=nodes[i];nodes[i]=nodes[best];nodes[best]=t;i=best;
                }
                return result;
            }
        }

        private readonly int width,height;
        private readonly Func<int,int,bool> passable;
        private readonly Func<int,int,double> additionalCost;
        private static readonly int[] DX={-1,0,1,-1,1,-1,0,1};
        private static readonly int[] DY={-1,-1,-1,0,0,1,1,1};
        public GridPathfinder(int width,int height,Func<int,int,bool> isPassable,
                              Func<int,int,double> moveExtraCost=null)
        {
            if(width<2 || height<2 || width>512 || height>512)
                throw new ArgumentOutOfRangeException("Grid dimensions must be 2–512.");
            this.width=width;this.height=height;
            passable=isPassable ?? throw new ArgumentNullException(nameof(isPassable));
            additionalCost=moveExtraCost ?? ((x,y)=>0d);
        }
        private bool InBounds(int x,int y) => x>=0 && x<width && y>=0 && y<height;
        private int Index(int x,int y) => y*width+x;
        private double Heuristic(int x,int y,int gx,int gy)
        {
            int dx=Math.Abs(x-gx),dy=Math.Abs(y-gy);
            return Math.Max(dx,dy)+(Math.Sqrt(2)-1)*Math.Min(dx,dy);
        }
        public List<GridCell> Find(GridCell start,GridCell goal)
        {
            var empty=new List<GridCell>();
            if(!InBounds(start.X,start.Y) || !InBounds(goal.X,goal.Y)
                || !passable(start.X,start.Y) || !passable(goal.X,goal.Y))
                return empty;
            int count=width*height;
            var cost=new double[count];
            var cameFrom=new int[count];
            var closed=new bool[count];
            for(int i=0;i<count;i++){cost[i]=double.PositiveInfinity;cameFrom[i]=-1;}
            int origin=Index(start.X,start.Y),dest=Index(goal.X,goal.Y);
            cost[origin]=0;
            var heap=new MinHeap();
            int serial=0;
            heap.Push(new OpenCell{Index=origin,Score=Heuristic(start.X,start.Y,goal.X,goal.Y),Serial=serial++});
            int expansions=0;
            while(heap.Count>0 && expansions++<count*5)
            {
                var next=heap.Pop();
                int u=next.Index;
                if(closed[u])continue;
                if(u==dest)
                {
                    var result=new List<GridCell>();
                    for(int n=dest;n>=0;n=cameFrom[n])
                    {
                        result.Add(new GridCell(n%width,n/width));
                        if(n==origin)break;
                    }
                    result.Reverse();
                    return result;
                }
                closed[u]=true;
                int x=u%width,y=u/width;
                for(int i=0;i<8;i++)
                {
                    int nx=x+DX[i],ny=y+DY[i];
                    if(!InBounds(nx,ny) || !passable(nx,ny))continue;
                    bool diagonal=DX[i]!=0 && DY[i]!=0;
                    if(diagonal && (!passable(x+DX[i],y) || !passable(x,y+DY[i])))
                        continue;
                    int v=Index(nx,ny);
                    if(closed[v])continue;
                    double extra=additionalCost(nx,ny);
                    if(double.IsNaN(extra) || double.IsInfinity(extra) || extra<0)
                        throw new ArgumentException("Movement cost must be finite and nonnegative.");
                    double proposed=cost[u]+(diagonal?Math.Sqrt(2):1)+extra;
                    if(proposed>=cost[v])continue;
                    cameFrom[v]=u;cost[v]=proposed;
                    heap.Push(new OpenCell{Index=v,
                        Score=proposed+Heuristic(nx,ny,goal.X,goal.Y),Serial=serial++});
                }
            }
            return empty;
        }
    }
}
