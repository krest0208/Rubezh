using Godot;
using Rubezh.Core;

namespace Rubezh.Game;

public partial class ArmoryView : MarginContainer
{
    public ArmoryView(App app,string? unit=null)
    {
        unit??="mg"; var unitId=unit;
        var page=app.Page("АРСЕНАЛ",app.ShowMenu); page.AddThemeConstantOverride("separation",14); AddChild(Ui.Margin(page));
        page.AddChild(Ui.Text("Иной силуэт. Тот же расчёт.",36));
        page.AddChild(Ui.Text("Модели меняют корпус, шасси и вооружение. Боевые характеристики одинаковы.",18,Ui.Muted));
        var row=Ui.HBox(24); row.SizeFlagsVertical=SizeFlags.ExpandFill; page.AddChild(row);
        var sidebar=new ScrollContainer{CustomMinimumSize=new Vector2(210,0),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,SizeFlagsVertical=SizeFlags.ExpandFill};
        row.AddChild(sidebar);
        var choices=Ui.VBox(7); choices.SizeFlagsHorizontal=SizeFlags.ExpandFill; sidebar.AddChild(choices);
        choices.AddChild(Ui.Text("УКРЕПЛЕНИЯ",14,Ui.Gold));
        foreach(var tower in app.Content.Towers) { string id=tower.Id; choices.AddChild(Ui.Button(tower.Name,()=>app.ShowArmory(id),unit==id)); }
        choices.AddChild(Ui.Text("ПРОТИВНИК",14,Ui.Gold));
        foreach(var enemy in app.Content.Enemies) { string id=enemy.Key; var b=Ui.Button(enemy.Value.Name,()=>app.ShowArmory(id),unit==id); b.CustomMinimumSize=new Vector2(0,36); choices.AddChild(b); }
        var models=Ui.HBox(16); models.SizeFlagsHorizontal=SizeFlags.ExpandFill; row.AddChild(models);
        string equipped=app.Content.ModelFor(unit,app.Store.Current.EquippedModels.GetValueOrDefault(unit)).Id;
        foreach(var model in app.Content.Models.Where(m=>m.Unit==unit))
        {
            var card=Ui.VBox(16); var panel=Ui.Card(card); panel.SizeFlagsHorizontal=SizeFlags.ExpandFill; models.AddChild(panel);
            card.AddChild(Ui.Text("0"+(model.Variant+1)+"   /   "+(model.Variant==0?"ШТАТНАЯ":"АЛЬТЕРНАТИВА"),14,Ui.Gold));
            var art=Ui.Model(model.Asset,220); art.SizeFlagsVertical=SizeFlags.ExpandFill; card.AddChild(art);
            card.AddChild(Ui.Wrap(model.Name,26)); card.AddChild(Ui.Wrap(model.Description,18,Ui.Muted)); card.AddChild(Ui.Spacer());
            bool owned=model.Variant==0 || app.Store.Current.OwnedModels.Contains(model.Id); bool active=model.Id==equipped;
            card.AddChild(Ui.Text(active?"НА ВООРУЖЕНИИ":owned?"В КОЛЛЕКЦИИ":"◆ "+model.Price,16,active?Ui.Gold:Ui.Muted));
            var b=Ui.Button(active?"Выбрана":owned?"Экипировать":"Купить и экипировать",()=>{
                if(app.Transaction(p=>p.BuyOrEquip(model))) app.ShowArmory(unitId);
                else if(app.Store.Warning is not null) {bMessage.Text=app.Store.Warning;bMessage.Show();}
            },!active); b.Disabled=active || (!owned && app.Store.Current.Gems<model.Price); card.AddChild(b);
        }
        bMessage.Hide(); page.AddChild(bMessage); page.AddChild(Ui.Text("+3 бриллианта за первое прохождение операции. 18 доступны в новом профиле.",16,Ui.Muted));
    }
    readonly Label bMessage=Ui.Text("",16,Ui.Red);
}
