using Godot;

namespace Rubezh.Game;

public partial class CampaignView : MarginContainer
{
    public CampaignView(App app,int pageIndex)
    {
        var page=app.Page("ОПЕРАЦИИ",app.ShowMenu); AddChild(Ui.Margin(page));
        var nav=Ui.HBox(); nav.AddChild(Ui.Text("Дорога длиной в тысячу рубежей",32)); nav.AddChild(Ui.Spacer());
        var lang=new OptionButton(); var languages=new[]{"ru","en","de","zh","fr","ja"};
        foreach(var l in languages) lang.AddItem(l.ToUpperInvariant()); lang.Selected=Array.IndexOf(languages,app.Store.Current.Language);
        lang.ItemSelected+=index=>app.ChangeLanguage(languages[(int)index]); nav.AddChild(Ui.Text("Текст миссий",16,Ui.Muted)); nav.AddChild(lang); page.AddChild(nav);
        int selected=Math.Clamp(app.Store.Current.HighestUnlocked,pageIndex*20,Math.Min(999,pageIndex*20+19));
        var row=Ui.HBox(28); row.SizeFlagsVertical=SizeFlags.ExpandFill; page.AddChild(row);
        var list=Ui.VBox(); list.SizeFlagsHorizontal=SizeFlags.ExpandFill; row.AddChild(list);
        var grid=new GridContainer{Columns=4,SizeFlagsVertical=SizeFlags.ExpandFill}; grid.AddThemeConstantOverride("h_separation",12); grid.AddThemeConstantOverride("v_separation",12); list.AddChild(grid);
        var detail=Ui.VBox(20); detail.CustomMinimumSize=new Vector2(400,0); row.AddChild(Ui.Card(detail));
        void Detail(int id)
        {
            foreach(Node c in detail.GetChildren()) { detail.RemoveChild(c); c.QueueFree(); }
            var m=app.Content.Campaign.Missions[id]; bool unlocked=id<=app.Store.Current.HighestUnlocked;
            detail.AddChild(Ui.Text("ОПЕРАЦИЯ "+(id+1).ToString("D3"),16,Ui.Gold)); detail.AddChild(Ui.Wrap(m.Title,30));
            detail.AddChild(Ui.Wrap(m.Place+" · "+m.Date,16,Ui.Muted)); detail.AddChild(Ui.Wrap(m.Story,18)); detail.AddChild(Ui.Wrap(m.Objective,20));
            detail.AddChild(Ui.Text($"{m.Waves} волн  /  {m.FirstReward} жетонов",18,Ui.Gold)); detail.AddChild(Ui.Wrap(m.Tip,16,Ui.Muted)); detail.AddChild(Ui.Spacer());
            var start=Ui.Button(unlocked?"Начать операцию →":"Пройдите предыдущую операцию",()=>app.StartBattle(id),true); start.Disabled=!unlocked; detail.AddChild(start);
        }
        for(int i=pageIndex*20;i<Math.Min(1000,pageIndex*20+20);i++)
        {
            int id=i; var m=app.Content.Campaign.Missions[i]; string stars=new('★',app.Store.Current.Stars.GetValueOrDefault(i));
            var b=Ui.Button($"{i+1:D3}  {stars}\n{(i<=app.Store.Current.HighestUnlocked?"ГОТОВА":"ЗАКРЫТА")}",()=>Detail(id));
            b.SizeFlagsHorizontal=SizeFlags.ExpandFill; b.SizeFlagsVertical=SizeFlags.ExpandFill; b.TooltipText=m.Title; grid.AddChild(b);
        }
        var paging=Ui.HBox(); var previous=Ui.Button("←",()=>app.ShowCampaign(pageIndex-1)); previous.Disabled=pageIndex==0; paging.AddChild(previous);
        paging.AddChild(Ui.Text($"{pageIndex+1} / 50",18,Ui.Muted)); var next=Ui.Button("→",()=>app.ShowCampaign(pageIndex+1)); next.Disabled=pageIndex==49; paging.AddChild(next); list.AddChild(paging); Detail(selected);
    }
}
