using System.Numerics;
using System.Text.Json;
using Rubezh.Core;

var data=Path.GetFullPath(args.FirstOrDefault()??"native/Game/Data");
Content Load(string lang="ru") => Content.Load(n=>File.ReadAllText(Path.Combine(data,n)),lang);
var content=Load(); int passed=0;
void Check(bool test,string message) { if(!test)throw new Exception(message); }
void Test(string name,Action test) {test();passed++;Console.WriteLine("PASS "+name);}
Test("6000 mission records and 36 unique models",()=>{
    foreach(var lang in new[]{"ru","en","de","zh","fr","ja"}) {
        var c=Load(lang);Check(c.Campaign.Missions.Length==1000,lang);
        Check(c.Campaign.Missions.Select(m=>m.Id).SequenceEqual(Enumerable.Range(0,1000)),"IDs");
    }
    Check(content.Models.Length==36,"models");
    var geometry=new HashSet<string>();
    foreach(var m in content.Models) {
        var file=Path.Combine(data,"..",m.Asset);Check(File.Exists(file),m.Asset);Check(geometry.Add(File.ReadAllText(file)),"Duplicate model geometry");
    }
});
Test("Placement, currency, unlocks, upgrades and sale",()=>{
    var b=new Battle(content,new Profile());int initial=b.Supplies;
    Check(b.Build("rocket",0) is null,"Locked rocket");Check(b.Supplies==initial,"Debit on rejection");
    Check(b.Build("mg",-1) is null,"Invalid slot");var t=b.Build("mg",0)!;Check(t is not null,"build");
    Check(b.Build("mg",0) is null,"Overlap");Check(b.Upgrade(t!),"upgrade");int refund=(int)(t!.Spent*.7f),before=b.Supplies;
    Check(b.Sell(t),"sell");Check(b.Supplies==before+refund,"refund");Check(!b.Sell(t),"double sale");
});
Test("Wave transitions, damage, rewards and targeting",()=>{
    var b=new Battle(content,new Profile());
    int slot=b.Slots.Select((p,i)=>(p,i)).OrderBy(x=>Vector2.Distance(x.p,b.Route.At(150).Position)).First().i;
    b.Build("mg",slot);Check(b.StartWave(),"start");Check(!b.StartWave(),"double start");
    int steps=0;while(b.Phase==BattlePhase.Wave && steps++<18000)b.Step(1f/60);
    Check(b.Kills>0,"No damage");Check(b.Phase==BattlePhase.Preparation,"wave did not complete");Check(b.Wave==1,"wave count");
});
Test("Unattended defense loses and final state rejects writes",()=>{
    var b=new Battle(content,new Profile());
    for(int i=0;i<60000 && b.Phase!=BattlePhase.Defeat;i++){if(b.Phase==BattlePhase.Preparation)b.StartWave();b.Step(1f/60);}
    Check(b.Phase==BattlePhase.Defeat && b.Lives==0,"defeat");Check(b.Build("mg",0) is null,"build after defeat");Check(!b.StartWave(),"start after defeat");
});
Test("Artillery range, cooldown and repeated damage rewards",()=>{
    var b=new Battle(content,new Profile());Check(!b.Artillery(Vector2.Zero),"strike before wave");b.StartWave();b.Step(.02f);
    Check(!b.Artillery(new Vector2(-10,0)),"out of bounds");Check(b.Artillery(b.Enemies[0].Position),"strike");
    int balance=b.Supplies;Check(!b.Artillery(Vector2.Zero),"cooldown");Check(b.Supplies==balance,"double reward");
});
Test("Cosmetic variants cannot alter combat",()=>{
    var a=new Profile();var p=a.Copy();foreach(var m in content.Models.Where(m=>m.Variant==2))p.EquippedModels[m.Unit]=m.Id;
    var first=new Battle(content,a);var second=new Battle(content,p);
    foreach(var b in new[]{first,second}){b.Build("mg",0);b.Build("mortar",1);b.StartWave();}
    for(int i=0;i<3000;i++){first.Step(1f/60);second.Step(1f/60);}
    Check(first.Kills==second.Kills && first.Lives==second.Lives && first.Supplies==second.Supplies,"cosmetic stats changed");
});
Test("Progress, repeat rewards and ownership",()=>{
    var p=new Profile();var m=content.Campaign.Missions[0];p.RecordWin(m,20);int coins=p.Coins,gems=p.Gems;
    p.RecordWin(m,12);Check(p.Coins==coins+m.RepeatReward && p.Gems==gems,"repeat rewards");Check(p.Stars[0]==3 && p.HighestUnlocked==1,"progress");
    var model=content.Models.First(m=>m.Variant==2);Check(p.BuyOrEquip(model),"buy");gems=p.Gems;Check(p.BuyOrEquip(model) && gems==p.Gems,"charged twice");
});
Test("Atomic saves, rollback, backup and corrupt-save recovery",()=>{
    var dir=Path.Combine(Path.GetTempPath(),"rubezh-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var file=Path.Combine(dir,"save.json");
    try {
        var store=new ProfileStore(file);Check(store.Transact(p=>{p.Coins=400;return true;}),"save");
        Check(store.Transact(p=>{p.Coins=800;return true;}),"second save");Check(new ProfileStore(file).Current.Coins==800,"reload");
        File.WriteAllText(file,"{broken");var recovered=new ProfileStore(file);Check(recovered.Current.Coins==400,"backup");
        Check(recovered.Transact(p=>{p.Coins=500;return true;}),"recover write");Check(File.Exists(file+".corrupt"),"corrupt copy");
        var blocker=Path.Combine(dir,"not-a-directory");File.WriteAllText(blocker,"block");var bad=new ProfileStore(Path.Combine(blocker,"save.json"));
        Check(!bad.Transact(p=>{p.Gems=0;return true;}) && bad.Current.Gems==18,"rollback");
    } finally {Directory.Delete(dir,true);}
});
Test("All 1000 routes have legal build sites and stable endpoints",()=>{
    for(int id=0;id<1000;id++){
        var b=new Battle(content,new Profile(),id);Check(b.Slots.Count>=12,$"slots {id}");Check(b.Route.Length>1000,$"route {id}");
        Check(b.Slots.All(p=>b.Route.DistanceTo(p)>48),"slot on road");Check(float.IsFinite(b.Route.At(b.Route.Length).Position.X),"endpoint");
    }
});
Test("Complete first operation using a deterministic defense",()=>{
    var p=new Profile();var b=new Battle(content,p);int loops=0;
    while(b.Phase is not (BattlePhase.Victory or BattlePhase.Defeat) && loops++<180000){
        if(b.Phase==BattlePhase.Preparation){
            foreach(var i in b.Slots.Select((pos,i)=>(pos,i)).OrderBy(x=>b.Route.DistanceTo(x.pos)).Select(x=>x.i))
                if(b.Towers.All(t=>t.Slot!=i))b.Build(b.Towers.Count%3==2?"mortar":"mg",i);
            foreach(var t in b.Towers.OrderBy(t=>t.Level))b.Upgrade(t);
            b.StartWave();
        }
        if(b.Enemies.Count>3 && b.ArtilleryCooldown<=0)b.Artillery(b.Enemies.OrderByDescending(e=>e.Distance).First().Position);
        b.Step(1f/60);
    }
    Check(b.Phase==BattlePhase.Victory,$"operation ended {b.Phase}, lives={b.Lives}, wave={b.Wave}, kills={b.Kills}");
    Console.WriteLine($"  First operation: {b.Wave} waves / {b.Kills} kills / {b.Lives} lives / {b.Elapsed:F1}s simulated");
});
Console.WriteLine($"{passed} tests passed.");
