using Godot;
using Rubezh.Core;

namespace Rubezh.Game;

public partial class BattleView : MarginContainer
{
    readonly App app;
    readonly Battle battle;
    readonly BattleBoard board;
    readonly Label hud=Ui.Text("",20), info=Ui.Text("",17,Ui.Muted), selection=Ui.Text("",17);
    readonly Button wave, pause, artillery, upgrade, sell, repair;
    readonly Dictionary<string,Button> buildButtons=new();
    string selectedKind="mg";
    bool paused, resultShown;
    int speed=1;
    float accumulator;
    public BattleView(App app,Battle battle)
    {
        this.app=app;this.battle=battle;
        foreach(var side in new[]{"left","right","top","bottom"}) AddThemeConstantOverride("margin_"+side,18);
        var page=Ui.VBox(10); AddChild(page);
        var top=Ui.HBox();top.AddChild(Ui.Button("← Штаб",ConfirmLeave));top.AddChild(Ui.Text(battle.Survival?"ВЫЖИВАНИЕ":$"ОПЕРАЦИЯ {battle.Mission.Id+1:D3}",20,Ui.Gold));top.AddChild(Ui.Spacer());top.AddChild(hud);
        pause=Ui.Button("Ⅱ",TogglePause); top.AddChild(pause);
        var speedButton=Ui.Button("×1",()=>{}); speedButton.Pressed+=()=>{speed=speed==1?2:1;speedButton.Text="×"+speed;};top.AddChild(speedButton);page.AddChild(top);
        board=new BattleBoard(app,battle);board.SlotClicked=ClickSlot;board.GroundClicked=p=>{if(paused)return;if(board.ArtilleryMode){if(battle.Artillery(p))board.ArtilleryMode=false;}else board.Selected=null;};page.AddChild(board);
        var selectedRow=Ui.HBox();selectedRow.AddChild(selection);selectedRow.AddChild(Ui.Spacer());
        repair=Ui.Button("Ремонт",()=>{if(board.Selected is not null)battle.Repair(board.Selected);});selectedRow.AddChild(repair);
        upgrade=Ui.Button("Улучшить",()=>{if(board.Selected is not null)battle.Upgrade(board.Selected);});selectedRow.AddChild(upgrade);
        sell=Ui.Button("Продать",()=>{if(board.Selected is not null){battle.Sell(board.Selected);board.Selected=null;}});selectedRow.AddChild(sell);page.AddChild(selectedRow);
        var bar=Ui.HBox(9);
        foreach(var d in app.Content.Towers)
        {
            string kind=d.Id;
            var b=Ui.Button(d.Name+"\n"+d.Cost,()=>{selectedKind=kind;board.Selected=null;board.ArtilleryMode=false;info.Text="Выберите свободную позицию (+).";});
            b.CustomMinimumSize=new Vector2(118,66);b.SizeFlagsHorizontal=SizeFlags.ExpandFill;b.TooltipText=d.Desc;buildButtons[kind]=b;bar.AddChild(b);
        }
        artillery=Ui.Button("Артудар",()=>{board.ArtilleryMode=!board.ArtilleryMode;info.Text=board.ArtilleryMode?"Укажите точку артиллерийского удара.":"Выберите укрепление и позицию.";});bar.AddChild(artillery);
        wave=Ui.Button("НАЧАТЬ ВОЛНУ →",()=>{battle.StartWave();},true);bar.AddChild(wave);page.AddChild(bar);
        info.Text="Выберите укрепление, затем позицию (+). Пробел — пауза; N — волна; A — артудар; 1–6 — укрепление.";page.AddChild(info);
        if(OS.GetCmdlineUserArgs().Contains("--capture"))
        {
            var slots=battle.Slots.Select((p,i)=>(p,i)).OrderBy(x=>System.Numerics.Vector2.Distance(x.p,battle.Route.At(220).Position)).Take(3).ToArray();
            for(int i=0;i<slots.Length;i++)battle.Build(i==2?"mortar":"mg",slots[i].i);
            battle.StartWave();
        }
        Refresh();
    }
    void ClickSlot(int slot)
    {
        var existing=battle.Towers.FirstOrDefault(t=>t.Slot==slot);
        if(existing is not null) {board.Selected=existing;return;}
        if(paused)return;
        var tower=battle.Build(selectedKind,slot);if(tower is not null)board.Selected=tower;else info.Text="Недостаточно снабжения или укрепление ещё закрыто.";
    }
    void TogglePause(){paused=!paused;pause.Text=paused?"▶":"Ⅱ";info.Text=paused?"ПАУЗА":"Оборона продолжается.";}
    void ConfirmLeave()
    {
        if(resultShown){app.ShowMenu();return;}
        bool previous=paused;paused=true;
        var dialog=new ConfirmationDialog{Title="Вернуться в штаб?",DialogText="Текущий бой будет завершён без награды. Прогресс кампании сохранён.",OkButtonText="В штаб",CancelButtonText="Продолжить"};
        AddChild(dialog);dialog.Confirmed+=app.ShowMenu;dialog.Canceled+=()=>{paused=previous;dialog.QueueFree();};dialog.PopupCentered(new Vector2I(460,180));
    }
    public override void _UnhandledKeyInput(InputEvent e)
    {
        if(e is not InputEventKey{Pressed:true,Echo:false} key)return;
        if(key.Keycode==Key.Space)TogglePause();
        if(key.Keycode==Key.N && !paused)battle.StartWave();
        if(key.Keycode==Key.A && !paused && battle.ArtilleryCooldown<=0)board.ArtilleryMode=!board.ArtilleryMode;
        if(key.Keycode==Key.Escape)board.ArtilleryMode=false;
        int n=(int)key.Keycode-(int)Key.Key1;
        if(n is >=0 and <6){selectedKind=app.Content.Towers[n].Id;board.Selected=null;board.ArtilleryMode=false;}
    }
    public override void _Process(double delta)
    {
        if(!paused && !resultShown)
        {
            accumulator+=Math.Min((float)delta,.1f)*speed;
            while(accumulator>=1f/60){battle.Step(1f/60);accumulator-=1f/60;}
            board.AdvanceEffects((float)delta*speed);
        }
        Refresh();
        if(!resultShown && battle.Phase is BattlePhase.Victory or BattlePhase.Defeat)ShowResult();
    }
    void Refresh()
    {
        hud.Text=$"♥ {battle.Lives}    ▣ {battle.Supplies}    ВОЛНА {battle.Wave}/{(battle.Survival?"∞":battle.Mission.Waves)}    ЦЕЛЕЙ {battle.Remaining}";
        wave.Disabled=paused || battle.Phase!=BattlePhase.Preparation;wave.Text=battle.Phase==BattlePhase.Preparation?"НАЧАТЬ ВОЛНУ →":"ВОЛНА ИДЁТ";
        artillery.Disabled=paused || battle.ArtilleryCooldown>0 || battle.Phase!=BattlePhase.Wave;artillery.Text=battle.ArtilleryCooldown>0?$"Удар {MathF.Ceiling(battle.ArtilleryCooldown)}с":board.ArtilleryMode?"УКАЖИТЕ ЦЕЛЬ":"Артудар [A]";
        foreach(var d in app.Content.Towers)
        {
            var b=buildButtons[d.Id];bool unlocked=battle.IsUnlocked(d);b.Disabled=paused || !unlocked || battle.Supplies<d.Cost;
            b.Text=unlocked?$"{(selectedKind==d.Id?"• ":"")}{d.Name}\n{d.Cost}":$"{d.Name}\nС миссии {d.Unlock+1}";
        }
        var t=board.Selected;if(t is not null && !battle.Towers.Contains(t)){board.Selected=null;t=null;}
        selection.Text=t is null?$"ПОЗИЦИЯ: {app.Content.Towers.First(d=>d.Id==selectedKind).Name} / щёлкните по (+)":$"{t.Definition.Name}  ·  ур. {t.Level}  ·  прочность {MathF.Ceiling(t.Health)}/{MathF.Ceiling(t.MaxHealth)}";
        upgrade.Disabled=paused || t is null || t.Level>=3 || battle.Supplies<battle.UpgradeCost(t);upgrade.Text=t is null?"Улучшить":t.Level>=3?"Макс. уровень":$"Улучшить · {battle.UpgradeCost(t)}";
        sell.Disabled=paused || t is null;sell.Text=t is null?"Продать":$"Продать · {(int)(t.Spent*.7f)}";
        repair.Disabled=paused || t is null || battle.RepairCost(t)<=0 || battle.Supplies<battle.RepairCost(t);repair.Text=t is null?"Ремонт":$"Ремонт · {battle.RepairCost(t)}";
    }
    void ShowResult()
    {
        resultShown=true;bool victory=battle.Phase==BattlePhase.Victory;
        bool saved=app.Transaction(p=>{if(victory)p.RecordWin(battle.Mission,battle.Lives);if(battle.Survival)p.BestSurvival=Math.Max(p.BestSurvival,Math.Max(0,battle.Wave-1));return true;});
        var overlay=new ColorRect{Color=new Color(0.035f,.065f,.05f,.94f)};AddChild(overlay);Ui.Fill(overlay);
        var center=new CenterContainer();overlay.AddChild(center);Ui.Fill(center);
        var col=Ui.VBox(22);col.CustomMinimumSize=new Vector2(620,0);center.AddChild(Ui.Card(col));
        col.AddChild(Ui.Text(victory?"РУБЕЖ УДЕРЖАН":"ЛИНИЯ ПРОРВАНА",38,victory?Ui.Gold:Ui.Red));
        col.AddChild(Ui.Wrap(victory?battle.Mission.Outro:"Перестройте позиции, сочетайте дальний огонь и контроль.",20));
        col.AddChild(Ui.Text($"Уничтожено: {battle.Kills}  /  Волн завершено: {(victory?battle.Wave:Math.Max(0,battle.Wave-1))}",18,Ui.Muted));
        if(!saved)col.AddChild(Ui.Wrap(app.Store.Warning??"Ошибка сохранения",18,Ui.Red));
        if(victory && saved && battle.Mission.Id<999)col.AddChild(Ui.Button("Следующая операция →",()=>app.StartBattle(battle.Mission.Id+1),true));
        col.AddChild(Ui.Button("Повторить",()=>app.StartBattle(battle.Mission.Id,battle.Survival)));col.AddChild(Ui.Button("В штаб",app.ShowMenu));
    }
}
