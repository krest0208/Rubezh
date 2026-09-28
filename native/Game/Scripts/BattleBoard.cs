using Godot;
using Rubezh.Core;
using V=System.Numerics.Vector2;

namespace Rubezh.Game;

public partial class BattleBoard : Control
{
    readonly Battle battle;
    readonly App app;
    readonly Dictionary<string,Texture2D> textures=new();
    readonly List<(Shot Shot,float Age)> effects=new();
    public Tower? Selected { get; set; }
    public bool ArtilleryMode { get; set; }
    public Action<int>? SlotClicked;
    public Action<V>? GroundClicked;
    float scale=1;
    Vector2 offset;
    readonly List<(Vector2 P,float S)> trees=new();
    public BattleBoard(App app,Battle battle)
    {
        this.app=app; this.battle=battle; CustomMinimumSize=new Vector2(500,310); SizeFlagsHorizontal=SizeFlags.ExpandFill; SizeFlagsVertical=SizeFlags.ExpandFill;
        MouseDefaultCursorShape=CursorShape.Cross; ClipContents=true;
        foreach(var unit in app.Content.Towers.Select(t=>t.Id).Concat(app.Content.Enemies.Keys))
        { var model=app.Content.ModelFor(unit,app.Store.Current.EquippedModels.GetValueOrDefault(unit)); textures[unit]=GD.Load<Texture2D>("res://"+model.Asset); }
        var rng=new Random(battle.Mission.LayoutSeed);
        for(int i=0;i<80;i++) { var p=new V(rng.Next(20,1180),rng.Next(20,600)); if(battle.Route.DistanceTo(p)>74 && battle.Slots.All(s=>V.Distance(s,p)>44)) trees.Add((ToGodot(p),rng.Next(14,27))); }
        battle.Fired+=OnShot;
    }
    void OnShot(Shot shot) { if(effects.Count<120) effects.Add((shot,0)); }
    public override void _ExitTree() => battle.Fired-=OnShot;
    public void AdvanceEffects(float dt)
    { for(int i=effects.Count-1;i>=0;i--) {var e=effects[i]; e.Age+=dt;if(e.Age>.35f)effects.RemoveAt(i);else effects[i]=e;} QueueRedraw(); }
    static Vector2 ToGodot(V p)=>new(p.X,p.Y);
    Vector2[] Path => battle.Route.Points.Select(ToGodot).ToArray();
    public override void _GuiInput(InputEvent e)
    {
        if(e is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Left} click)
        {
            var p=(click.Position-offset)/scale; var world=new V(p.X,p.Y);
            if(ArtilleryMode) {GroundClicked?.Invoke(world); AcceptEvent();return;}
            int closest=-1; float distance=36;
            for(int i=0;i<battle.Slots.Count;i++) {float d=V.Distance(battle.Slots[i],world);if(d<distance){distance=d;closest=i;}}
            if(closest>=0) SlotClicked?.Invoke(closest); else GroundClicked?.Invoke(world); AcceptEvent();
        }
    }
    public override void _Draw()
    {
        scale=Math.Min(Size.X/1200,Size.Y/620); offset=(Size-new Vector2(1200,620)*scale)/2;
        DrawRect(new Rect2(Vector2.Zero,Size),new Color("#14221c")); DrawSetTransform(offset,0,Vector2.One*scale);
        bool snow=battle.Mission.Theme.Contains("snow") || battle.Mission.Theme.Contains("frost");
        Color ground=new(snow?"#9cae9e":"#526246"), patch=new(snow?"#b5c4b2":"#5a6b4b"), road=new(snow?"#788b7c":"#ad9a6e");
        DrawRect(new Rect2(0,0,1200,620),ground);
        for(int i=0;i<95;i++) {float x=(i*317+battle.Mission.LayoutSeed*17)%1200,y=(i*139)%620; DrawCircle(new Vector2(x,y),22+i%41,patch);}
        for(int x=0;x<1200;x+=60) DrawLine(new Vector2(x,0),new Vector2(x,620),new Color(1,1,1,.025f));
        for(int y=0;y<620;y+=60) DrawLine(new Vector2(0,y),new Vector2(1200,y),new Color(1,1,1,.025f));
        var path=Path;
        DrawPolyline(path,new Color("#354734"),66,true); DrawPolyline(path,road,54,true);
        foreach(var p in path) {DrawCircle(p,32,new Color("#354734"));DrawCircle(p,26,road);}
        DrawPolyline(path,new Color(snow?"#899b8b":"#b9a87d"),34,true);
        for(float d=0;d<battle.Route.Length;d+=37)
        { var (p,a)=battle.Route.At(d); var v=ToGodot(p); var normal=new Vector2(-Mathf.Sin(a),Mathf.Cos(a))*14; DrawLine(v-normal,v+normal,new Color(0,0,0,.1f),3,true); }
        foreach(var (p,s) in trees)
        {DrawCircle(p+new Vector2(6,8),s,new Color(0,0,0,.18f));DrawCircle(p,s,new Color("#2e4936"));DrawCircle(p-new Vector2(4,5),s*.72f,new Color(snow?"#c4d2bc":"#698058"));}
        var end=path[^1]; DrawRect(new Rect2(end-new Vector2(27,35),new Vector2(54,70)),new Color("#354339"));
        DrawRect(new Rect2(end-new Vector2(18,25),new Vector2(36,50)),Ui.Gold); DrawString(ThemeDB.FallbackFont,end+new Vector2(-25,-45),"ШТАБ",HorizontalAlignment.Left,-1,14,Ui.Ink);
        if(Selected is not null && battle.Towers.Contains(Selected))
        { var p=ToGodot(Selected.Position); float r=battle.Range(Selected);DrawCircle(p,r,new Color(1,.87f,.45f,.065f));DrawArc(p,r,0,Mathf.Tau,100,new Color(1,.88f,.5f,.55f),1.5f,true); }
        for(int i=0;i<battle.Slots.Count;i++)
        {
            var p=ToGodot(battle.Slots[i]); if(battle.Towers.Any(t=>t.Slot==i)) continue;
            DrawCircle(p,20,new Color(.07f,.13f,.1f,.6f)); DrawArc(p,20,0,Mathf.Tau,32,new Color("#899777"),1.5f,true);
            DrawLine(p-new Vector2(6,0),p+new Vector2(6,0),Ui.Ink,1.5f);DrawLine(p-new Vector2(0,6),p+new Vector2(0,6),Ui.Ink,1.5f);
        }
        foreach(var t in battle.Towers)
        {
            var p=ToGodot(t.Position); DrawSetTransform(offset+p*scale,t.Angle,Vector2.One*scale);
            DrawTextureRect(textures[t.Definition.Id],new Rect2(-40,-40,80,80),false); DrawSetTransform(offset,0,Vector2.One*scale);
            for(int i=0;i<t.Level;i++) DrawCircle(p+new Vector2(-6+i*6,32),2,Ui.Gold);
            if(t.Health<t.MaxHealth) Bar(p+new Vector2(0,39),t.Health/t.MaxHealth,Ui.Gold,36);
        }
        foreach(var e in battle.Enemies)
        {
            var p=ToGodot(e.Position); float size=e.Kind is "infantry" or "scout"?42:e.Kind=="heavy"?77:64;
            DrawSetTransform(offset+p*scale,e.Angle,Vector2.One*scale); DrawTextureRect(textures[e.Kind],new Rect2(-size/2,-size/2,size,size),false);
            DrawSetTransform(offset,0,Vector2.One*scale); Bar(p-new Vector2(0,size/2+3),e.Health/e.MaxHealth,e.Slowed?new Color("#7ac7c5"):Ui.Red,28);
        }
        foreach(var (shot,age) in effects)
        {
            var from=ToGodot(shot.From); var to=ToGodot(shot.To); float fade=1-age/.35f;
            Color c=shot.Kind=="hostile"?Ui.Red:Ui.Gold;c.A=fade;
            if(age<.14f) DrawLine(from,to,c,shot.Kind=="rocket"?4:2,true);
            if(shot.Radius>0) {DrawCircle(to,Mathf.Lerp(5,shot.Radius,age/.35f),new Color(c.R,c.G,c.B,fade*.12f));DrawArc(to,Mathf.Lerp(5,shot.Radius,age/.35f),0,Mathf.Tau,24,c,2,true);}
            else DrawCircle(to,4*fade,c);
        }
        DrawSetTransform(Vector2.Zero);
    }
    void Bar(Vector2 p,float value,Color color,float w) {DrawRect(new Rect2(p.X-w/2,p.Y,w,4),new Color("#18241d"));DrawRect(new Rect2(p.X-w/2,p.Y,w*Math.Clamp(value,0,1),4),color);}
}
