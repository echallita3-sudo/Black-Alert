using System;
using BlackAlert.Simulation;
using UnityEngine;

namespace BlackAlert
{
    /// <summary>
    /// Client-side battlefield intelligence visualization. Uses a separate, headless
    /// per-team FogOfWarGrid for visibility rules. Hides enemy actors outside friendly
    /// vision and displays a clickable tactical minimap. Server-authoritative sight
    /// must replace client authority before online multiplayer.
    /// </summary>
    public sealed class BlackAlertTacticalIntel : MonoBehaviour
    {
        public const int Width=100,Height=90;
        public const float MinX=-50f,MinZ=-45f;
        private FogOfWarGrid fog;
        private BattlefieldPrototype battle;
        private Texture2D fogTexture;
        private Texture2D mapTexture;
        private Color32[] fogPixels;
        private Color32[] mapPixels;
        private Color32[] groundPixels;
        private float nextUpdate;
        private bool enabledIntel=true;
        private bool initialized;
        private GUIStyle captionStyle;
        private Texture2D markerTexture;
        private int visibleEnemies;
        private int exploredCells;

        public bool IsInitialized { get {return initialized;} }
        public bool FogEnabled { get {return enabledIntel;} }
        public int VisibleEnemyCount { get {return visibleEnemies;} }
        public int ExploredCellCount { get {return exploredCells;} }

        private static int CellX(float x) =>
            Mathf.Clamp(Mathf.FloorToInt(x-MinX),0,Width-1);
        private static int CellZ(float z) =>
            Mathf.Clamp(Mathf.FloorToInt(z-MinZ),0,Height-1);
        private static Vector3 CellCenter(int x,int z) =>
            new Vector3(MinX+x+.5f,0,MinZ+z+.5f);

        public void Initialize(BattlefieldPrototype host)
        {
            battle=host;
            bool Blocks(int x,int z)
            {
                Vector3 position=CellCenter(x,z);
                // Tall rock outcrops are local line-of-sight blockers for ground scouts.
                return battle.TerrainHeight(position.x,position.z)>3.8f;
            }
            fog=new FogOfWarGrid(Width,Height,Blocks);
            fogTexture=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
            fogTexture.name="Black Alert faction sight texture";
            fogTexture.filterMode=FilterMode.Point;
            fogTexture.wrapMode=TextureWrapMode.Clamp;
            mapTexture=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
            mapTexture.name="Black Alert tactical map";
            mapTexture.filterMode=FilterMode.Point;
            mapTexture.wrapMode=TextureWrapMode.Clamp;
            fogPixels=new Color32[Width*Height];
            mapPixels=new Color32[Width*Height];
            groundPixels=new Color32[Width*Height];
            for(int z=0;z<Height;z++)
                for(int x=0;x<Width;x++)
                {
                    Vector3 p=CellCenter(x,z);
                    float height=battle.TerrainHeight(p.x,p.z);
                    bool water=battle.IsWater(p);
                    bool mountain=height>4f;
                    var baseColor=water ? new Color32(23,91,113,255)
                         : mountain?new Color32(105,104,87,255)
                         : height>1.5f?new Color32(71,95,70,255)
                         : new Color32(49,83,60,255);
                    groundPixels[z*Width+x]=baseColor;
                }
            markerTexture=new Texture2D(1,1,TextureFormat.RGBA32,false);
            markerTexture.SetPixel(0,0,Color.white);
            markerTexture.Apply();
            battle.SetFogMask(fogTexture);
            initialized=true;
            RefreshSight();
        }

        public void ToggleIntel()
        {
            enabledIntel=!enabledIntel;
            RefreshSight();
        }

        public bool CanSpot(bool hostile,Vector3 position)
        {
            if(!initialized || !enabledIntel)return true;
            return fog.IsVisible(hostile?1:0,CellX(position.x),CellZ(position.z));
        }

        public bool CanSelectEnemy(Vector3 position)
        {
            return CanSpot(false,position);
        }

        private void Update()
        {
            if(!initialized || battle==null)return;
            if(Time.unscaledTime<nextUpdate)return;
            nextUpdate=Time.unscaledTime+.36f;
            RefreshSight();
        }

        private void RefreshSight()
        {
            if(!initialized)return;
            fog.BeginFrame(0);
            fog.BeginFrame(1);
            // Bases remain active radar/scouting sources, preventing starting blindness.
            fog.Reveal(0,CellX(-33),CellZ(-2),11);
            fog.Reveal(1,CellX(33),CellZ(2),11);
            battle.EnumerateScoutPositions((hostile,at,radius)=>
            {
                fog.Reveal(hostile?1:0,CellX(at.x),CellZ(at.z),radius);
            });
            visibleEnemies=0;
            battle.ApplyEnemyVisibility(enemyWorld=>
            {
                bool visible=CanSelectEnemy(enemyWorld);
                if(visible)visibleEnemies++;
                return visible;
            });
            exploredCells=Width*Height-fog.Count(0,IntelState.Unknown);
            for(int z=0;z<Height;z++)
                for(int x=0;x<Width;x++)
                {
                    int offset=z*Width+x;
                    var status=enabledIntel?fog.Get(0,x,z):IntelState.Visible;
                    byte luminance=status==IntelState.Visible?(byte)255:
                        status==IntelState.Explored?(byte)100:(byte)20;
                    fogPixels[offset]=new Color32(luminance,luminance,luminance,255);
                    Color32 original=groundPixels[offset];
                    if(status==IntelState.Visible)
                        mapPixels[offset]=original;
                    else if(status==IntelState.Explored)
                        mapPixels[offset]=new Color32((byte)(original.r*.40f),
                            (byte)(original.g*.40f),(byte)(original.b*.40f),255);
                    else
                        mapPixels[offset]=new Color32(9,12,15,255);
                }
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false,false);
            mapTexture.SetPixels32(mapPixels);
            mapTexture.Apply(false,false);
        }

        private Rect MapRect()
        {
            // Keep minimap below the mode panel; scale with small landscape screens.
            float width=Mathf.Min(190f,Screen.width*.225f);
            float height=width*.9f;
            return new Rect(Screen.width-width-12f,Screen.height-height-12f,width,height);
        }
        private Rect ButtonRect()
        {
            var r=MapRect();
            return new Rect(r.x,r.y-35f,r.width,32f);
        }

        public bool BlocksTap(Vector2 point)
        {
            float y=Screen.height-point.y;
            var gui=new Vector2(point.x,y);
            return MapRect().Contains(gui)||ButtonRect().Contains(gui);
        }

        public bool ProcessTap(Vector2 point)
        {
            float y=Screen.height-point.y;
            var gui=new Vector2(point.x,y);
            if(!MapRect().Contains(gui))return false;
            Rect area=MapRect();
            float u=Mathf.Clamp01((gui.x-area.x)/area.width);
            float v=Mathf.Clamp01((gui.y-area.y)/area.height);
            float x=MinX+u*Width;
            // GUI Y increases downward; world z increases north/up the minimap.
            float z=MinZ+(1f-v)*Height;
            battle.JumpCameraTo(new Vector3(x,0,z));
            return true;
        }

        private void OnGUI()
        {
            if(!initialized || mapTexture==null)return;
            if(captionStyle==null)
            {
                captionStyle=new GUIStyle(GUI.skin.label);
                captionStyle.fontSize=13;
                captionStyle.alignment=TextAnchor.MiddleCenter;
                captionStyle.normal.textColor=Color.white;
            }
            var rect=MapRect();
            GUI.Box(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),"");
            GUI.DrawTexture(rect,mapTexture,ScaleMode.StretchToFill,false);
            // World-space colored markers remain subject to the player's fog knowledge.
            battle.EnumerateScoutPositions((hostile,at,radius)=>
            {
                if(hostile && !CanSelectEnemy(at))return;
                float u=(at.x-MinX)/Width;
                float v=(at.z-MinZ)/Height;
                var pixel=new Rect(rect.x+u*rect.width-3,
                    rect.y+(1f-v)*rect.height-3,6,6);
                Color previous=GUI.color;
                GUI.color=hostile?new Color(.99f,.23f,.21f):new Color(.17f,.78f,1f);
                GUI.DrawTexture(pixel,markerTexture);
                GUI.color=previous;
            });
            GUI.Label(new Rect(rect.x,rect.y-24,rect.width,22),
                "SCOUT RADAR | ENEMIES "+visibleEnemies,captionStyle);
            if(GUI.Button(ButtonRect(),enabledIntel?"FOG OF WAR: ON":"FOG OF WAR: OFF"))
                ToggleIntel();
        }

        private void OnDestroy()
        {
            if(fogTexture!=null)Destroy(fogTexture);
            if(mapTexture!=null)Destroy(mapTexture);
            if(markerTexture!=null)Destroy(markerTexture);
        }
    }
}
