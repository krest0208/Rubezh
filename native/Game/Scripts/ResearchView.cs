using Godot;
using Rubezh.Core;

namespace Rubezh.Game;

public partial class ResearchView : MarginContainer
{
    public ResearchView(App app)
    {
        var page=app.Page("РАЗВИТИЕ",app.ShowMenu); AddChild(Ui.Margin(page)); page.AddChild(Ui.Text("Подготовка решает исход боя.",34));
        var grid=new GridContainer{Columns=2,SizeFlagsVertical=SizeFlags.ExpandFill}; grid.AddThemeConstantOverride("h_separation",18); grid.AddThemeConstantOverride("v_separation",14); page.AddChild(grid);
        var warning=Ui.Wrap("",16,Ui.Red);
        foreach(var d in app.Content.Research)
        {
            var card=Ui.VBox(8); var panel=Ui.Card(card); panel.SizeFlagsHorizontal=SizeFlags.ExpandFill; grid.AddChild(panel);
            int level=app.Store.Current.Skill(d.Id), cost=Profile.ResearchCost(d,level);
            var header=Ui.HBox(); header.AddChild(Ui.Text(d.Name,22)); header.AddChild(Ui.Spacer()); header.AddChild(Ui.Text($"{level}/{d.Max}",20,Ui.Gold)); card.AddChild(header);
            card.AddChild(Ui.Wrap(d.Desc,16,Ui.Muted));
            var b=Ui.Button(level>=d.Max?"Завершено":$"Исследовать  /  {cost} жетонов",()=>{
                if(app.Transaction(p=>p.BuyResearch(d))) app.ShowResearch(); else warning.Text=app.Store.Warning??"Недостаточно жетонов.";
            }); b.Disabled=level>=d.Max || app.Store.Current.Coins<cost; card.AddChild(b);
        }
        page.AddChild(warning);
    }
}
