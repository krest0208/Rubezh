using System.Numerics;

namespace Rubezh.Core;

public enum BattlePhase { Preparation, Wave, Victory, Defeat }
public sealed class Enemy
{
    public required string Kind { get; init; }
    public required EnemyDefinition Definition { get; init; }
    public int Id { get; init; }
    public float Health { get; set; }
    public float MaxHealth { get; init; }
    public float Distance { get; set; }
    public Vector2 Position { get; set; }
    public float Angle { get; set; }
    public float AttackTimer { get; set; } = 3;
    public bool Slowed { get; set; }
    public bool Dead => Health <= 0;
}
public sealed class Tower
{
    public required TowerDefinition Definition { get; init; }
    public int Slot { get; init; }
    public Vector2 Position { get; init; }
    public int Level { get; set; } = 1;
    public int Spent { get; set; }
    public float Cooldown { get; set; }
    public float Angle { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
}
public sealed record Shot(Vector2 From, Vector2 To, string Kind, float Radius);

/// <summary>Pure C# simulation. No renderer, filesystem, engine objects, wall clock or cosmetics.</summary>
public sealed class Battle
{
    readonly Content content;
    readonly Profile profile;
    readonly Queue<string> spawnQueue=new();
    float spawnClock;
    float enemyScale;
    int nextId;
    public Mission Mission { get; }
    public bool Survival { get; }
    public Route Route { get; }
    public List<Vector2> Slots { get; }=new();
    public List<Enemy> Enemies { get; }=new();
    public List<Tower> Towers { get; }=new();
    public event Action<Shot>? Fired;
    public BattlePhase Phase { get; private set; }=BattlePhase.Preparation;
    public int Wave { get; private set; }
    public int Supplies { get; private set; }
    public int Lives { get; private set; }=20;
    public int Kills { get; private set; }
    public float ArtilleryCooldown { get; private set; }
    public float Elapsed { get; private set; }
    public int Remaining => Enemies.Count + spawnQueue.Count;
    public Battle(Content content, Profile profile, int missionId=0, bool survival=false)
    {
        this.content=content; this.profile=profile.Copy(); Survival=survival;
        Mission=content.Campaign.Missions[Math.Clamp(missionId,0,999)];
        Supplies=370+25*profile.Skill("logistics");
        var plans=Mission.Chapter>0?content.AdvancedPaths:content.Paths;
        var plan=plans[Math.Abs(Mission.Map)%plans.Length];
        bool mirror=(Mission.LayoutSeed%2)!=0;
        Route=new Route(plan.Select(p=>new Vector2(60+Math.Clamp(p[0],0,1)*1080,60+(mirror?1-p[1]:p[1])*500)));
        for (int y=70; y<=560; y+=82) for (int x=92; x<=1120; x+=86)
        {
            var pos=new Vector2(x,y); float d=Route.DistanceTo(pos);
            if(d>48 && d<140 && Slots.All(s=>Vector2.Distance(s,pos)>80)) Slots.Add(pos);
        }
    }
    public bool IsUnlocked(TowerDefinition definition) => Survival || profile.HighestUnlocked >= definition.Unlock;
    public float Range(Tower t) => t.Definition.Range*(1+.02f*profile.Skill("optics")+.06f*(t.Level-1));
    public int UpgradeCost(Tower t) => (int)(t.Definition.Cost*(.65f+.3f*t.Level)*(1-.03f*profile.Skill("engineers")));
    public int RepairCost(Tower t) => (int)MathF.Ceiling((t.MaxHealth-t.Health)*.22f);
    bool Active => Phase is BattlePhase.Preparation or BattlePhase.Wave;
    public Tower? Build(string kind,int slot)
    {
        var d=content.Towers.FirstOrDefault(t=>t.Id==kind);
        if (!Active || d is null || !IsUnlocked(d) || slot<0 || slot>=Slots.Count || Towers.Any(t=>t.Slot==slot) || Supplies<d.Cost) return null;
        Supplies-=d.Cost;
        var hp=130*(1+.1f*profile.Skill("fortification"));
        var tower=new Tower{Definition=d,Slot=slot,Position=Slots[slot],Spent=d.Cost,Health=hp,MaxHealth=hp};
        Towers.Add(tower); return tower;
    }
    public bool Upgrade(Tower tower)
    {
        int cost=UpgradeCost(tower);
        if(!Active || !Towers.Contains(tower) || tower.Level>=3 || Supplies<cost) return false;
        Supplies-=cost; tower.Spent+=cost; tower.Level++; tower.MaxHealth*=1.25f; tower.Health=tower.MaxHealth; return true;
    }
    public bool Sell(Tower tower)
    {
        if (!Active || !Towers.Remove(tower)) return false;
        Supplies+=(int)(tower.Spent*.7f); return true;
    }
    public bool Repair(Tower tower)
    {
        int cost=RepairCost(tower);
        if(!Active || !Towers.Contains(tower) || cost<=0 || Supplies<cost) return false;
        Supplies-=cost; tower.Health=tower.MaxHealth; return true;
    }
    public bool StartWave()
    {
        if(Phase!=BattlePhase.Preparation) return false;
        Wave++; Phase=BattlePhase.Wave; spawnClock=0;
        var rank=Survival?Math.Max(0,(Wave-1)*.7f):Mission.Rank;
        enemyScale=1+rank*.085f+Math.Max(0,Wave-5)*.035f;
        int count=Math.Min(75,7+Wave*2+(int)rank);
        for(int i=0;i<count;i++)
        {
            string kind="infantry";
            if((i+Wave)%4==0) kind="scout";
            if(Wave>=3 && i%5==0) kind="truck";
            if((Wave>=5 || rank>=2) && i%6==0) kind="halftrack";
            if((Wave>=7 || rank>=4) && i%8==0) kind="tank";
            if((Wave>=10 || rank>=8) && i%13==0) kind="heavy";
            spawnQueue.Enqueue(kind);
        }
        return true;
    }
    public bool Artillery(Vector2 target)
    {
        if(Phase!=BattlePhase.Wave || ArtilleryCooldown>0 || target.X<0 || target.X>1200 || target.Y<0 || target.Y>620) return false;
        ArtilleryCooldown=35*(1-.035f*profile.Skill("signal"));
        foreach(var e in Enemies.Where(e=>Vector2.Distance(e.Position,target)<105).ToArray()) Damage(e,320,1);
        Fired?.Invoke(new Shot(target-new Vector2(100,250),target,"artillery",105)); RemoveDead(); return true;
    }
    void Damage(Enemy e,float amount,float penetration)
    {
        if(e.Dead) return;
        e.Health-=amount*(1-e.Definition.Armor*(1-penetration));
        if(e.Dead) { Supplies+=(int)(e.Definition.Reward*(1+.02f*profile.Skill("salvage"))); Kills++; }
    }
    void RemoveDead() => Enemies.RemoveAll(e=>e.Dead);
    public void Step(float dt)
    {
        if(Phase!=BattlePhase.Wave || dt<=0 || !float.IsFinite(dt)) return;
        dt=Math.Min(dt,.05f); Elapsed+=dt; ArtilleryCooldown=Math.Max(0,ArtilleryCooldown-dt); spawnClock-=dt;
        if(spawnQueue.Count>0 && spawnClock<=0)
        {
            var kind=spawnQueue.Dequeue(); var d=content.Enemies[kind]; var at=Route.At(0);
            Enemies.Add(new Enemy{Id=nextId++,Kind=kind,Definition=d,Health=d.Hp*enemyScale,MaxHealth=d.Hp*enemyScale,Position=at.Position,Angle=at.Angle});
            spawnClock=Math.Max(.25f,.85f-Wave*.02f);
        }
        foreach(var e in Enemies)
        {
            e.Slowed=Towers.Any(t=>t.Definition.Id=="sapper" && Vector2.Distance(t.Position,e.Position)<Range(t));
            e.Distance+=e.Definition.Speed*(e.Slowed ? .52f : 1)*dt;
            (e.Position,e.Angle)=Route.At(e.Distance);
            if(e.Distance>=Route.Length) { Lives=Math.Max(0,Lives-e.Definition.Leak); e.Health=0; }
            // Armoured units threaten positions; fortification and repairs have an actual role.
            if(e.Definition.Armor>=.5f && !e.Dead)
            {
                e.AttackTimer-=dt;
                var t=Towers.OrderBy(t=>Vector2.DistanceSquared(t.Position,e.Position)).FirstOrDefault();
                if(e.AttackTimer<=0 && t is not null && Vector2.Distance(t.Position,e.Position)<135)
                { t.Health-=e.Kind=="heavy"?14:7; e.AttackTimer=3; Fired?.Invoke(new Shot(e.Position,t.Position,"hostile",0)); }
            }
        }
        RemoveDead(); Towers.RemoveAll(t=>t.Health<=0);
        if(Lives==0) { Phase=BattlePhase.Defeat; return; }
        foreach(var t in Towers)
        {
            t.Cooldown-=dt;
            var eligible=Enemies.Where(e=>!e.Dead && Vector2.Distance(e.Position,t.Position)<=Range(t));
            var target=t.Definition.Id=="rifle"?eligible.OrderByDescending(e=>e.Definition.Speed).FirstOrDefault():eligible.OrderByDescending(e=>e.Distance).FirstOrDefault();
            if(target is null) continue;
            var delta=target.Position-t.Position; t.Angle=MathF.Atan2(delta.Y,delta.X);
            if(t.Cooldown>0) continue;
            t.Cooldown=t.Definition.Rate/(1+.12f*(t.Level-1));
            float damage=t.Definition.Damage*(1+.5f*(t.Level-1))*(1+.03f*profile.Skill("arsenal"));
            if(t.Definition.Id=="rocket") damage*=3;
            if(t.Definition.Splash>0)
            { foreach(var e in Enemies.Where(e=>Vector2.Distance(e.Position,target.Position)<=t.Definition.Splash).ToArray()) Damage(e,damage,t.Definition.Pen); }
            else Damage(target,damage,t.Definition.Pen);
            Fired?.Invoke(new Shot(t.Position,target.Position,t.Definition.Id,t.Definition.Splash));
        }
        RemoveDead();
        if(spawnQueue.Count==0 && Enemies.Count==0)
        { Supplies+=35+Wave*4; Phase=!Survival && Wave>=Mission.Waves?BattlePhase.Victory:BattlePhase.Preparation; }
    }
}
