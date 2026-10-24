using System;
using System.Collections.Generic;

namespace BlackAlert.Simulation
{
    public enum IntelState : byte { Unknown = 0, Explored = 1, Visible = 2 }

    /// <summary>
    /// Separate per-faction knowledge of a strategy map. Hidden locations stay black,
    /// previously seen cells remain dim, and enemy locations are disclosed only while
    /// currently visible. Pure deterministic logic, usable in authoritative multiplayer.
    /// </summary>
    public sealed class FogOfWarGrid
    {
        private readonly Dictionary<int,byte[]> factionCells=new Dictionary<int,byte[]>();
        private readonly Func<int,int,bool> visionBlocker;
        public int Width { get; }
        public int Height { get; }
        public FogOfWarGrid(int width,int height,Func<int,int,bool> obstacle=null)
        {
            if(width<4 || height<4 || width>512 || height>512)
                throw new ArgumentOutOfRangeException("Fog grid dimensions must be 4–512.");
            Width=width;Height=height;visionBlocker=obstacle;
        }
        private bool InBounds(int x,int y) => x>=0 && x<Width && y>=0 && y<Height;
        private byte[] GetFaction(int team)
        {
            if(team<0 || team>255)throw new ArgumentOutOfRangeException(nameof(team));
            if(!factionCells.TryGetValue(team,out byte[] cells))
            {
                cells=new byte[Width*Height];
                factionCells.Add(team,cells);
            }
            return cells;
        }
        public IntelState Get(int team,int x,int y)
        {
            if(!InBounds(x,y))return IntelState.Unknown;
            return (IntelState)GetFaction(team)[y*Width+x];
        }
        public bool IsVisible(int team,int x,int y) => Get(team,x,y)==IntelState.Visible;
        public bool IsExplored(int team,int x,int y) => Get(team,x,y)!=IntelState.Unknown;
        public void BeginFrame(int team)
        {
            var map=GetFaction(team);
            for(int i=0;i<map.Length;i++)
                if(map[i]==(byte)IntelState.Visible)
                    map[i]=(byte)IntelState.Explored;
        }
        private bool ClearSight(int sx,int sy,int tx,int ty)
        {
            if(visionBlocker==null)return true;
            int dx=Math.Abs(tx-sx),dy=Math.Abs(ty-sy);
            int stepX=sx<tx?1:-1,stepY=sy<ty?1:-1;
            int error=dx-dy;
            int x=sx,y=sy;
            while(x!=tx || y!=ty)
            {
                int twice=2*error;
                if(twice>-dy) {error-=dy;x+=stepX;}
                if(twice<dx) {error+=dx;y+=stepY;}
                // One can observe the leading face of a blocking hill, but never
                // units hidden behind it. The observer's own cell never blocks.
                if(x==tx && y==ty) return true;
                if(visionBlocker(x,y))return false;
            }
            return true;
        }
        public void Reveal(int team,int x,int y,int radius)
        {
            if(!InBounds(x,y))return;
            if(radius<0 || radius>128)throw new ArgumentOutOfRangeException(nameof(radius));
            var map=GetFaction(team);
            int left=Math.Max(0,x-radius),right=Math.Min(Width-1,x+radius);
            int bottom=Math.Max(0,y-radius),top=Math.Min(Height-1,y+radius);
            int r2=radius*radius;
            for(int yy=bottom; yy<=top;yy++)
                for(int xx=left;xx<=right;xx++)
                {
                    int dx=xx-x,dy=yy-y;
                    if(dx*dx+dy*dy>r2 || !ClearSight(x,y,xx,yy))continue;
                    map[yy*Width+xx]=(byte)IntelState.Visible;
                }
        }
        public void CopyState(int team,byte[] target)
        {
            if(target==null || target.Length!=Width*Height)
                throw new ArgumentException("Fog snapshot size mismatch.");
            Array.Copy(GetFaction(team),target,target.Length);
        }
        public int Count(int team,IntelState state)
        {
            int result=0;
            foreach(byte value in GetFaction(team))
                if(value==(byte)state)result++;
            return result;
        }
    }
}
