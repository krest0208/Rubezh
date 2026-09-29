using Godot;

namespace Rubezh.Game;

public partial class MenuView : MarginContainer
{
    public MenuView(App app)
    {
        foreach(var side in new[]{"left","right","top","bottom"}) AddThemeConstantOverride("margin_"+side,42);
        var page=Ui.VBox(24); AddChild(page);
        var top=Ui.HBox(); top.AddChild(Ui.Text("ПОЛЕВОЙ ШТАБ  /  01",17,Ui.Muted)); top.AddChild(Ui.Spacer()); top.AddChild(Ui.Text("НОВАЯ ЛИНИЯ",17,Ui.Gold)); page.AddChild(top);
        var row=Ui.HBox(44); row.SizeFlagsVertical=SizeFlags.ExpandFill; page.AddChild(row);
        var left=Ui.VBox(18); left.CustomMinimumSize=new Vector2(385,0); row.AddChild(left);
        left.AddChild(Ui.Spacer()); left.AddChild(Ui.Text("РУБЕЖ",84)); left.AddChild(Ui.Text("ДЕРЖАТЬ. ДО ПОСЛЕДНЕГО.",18,Ui.Gold));
        left.AddChild(Ui.Wrap("Твоя позиция. Твой расчёт.\nОдна дорога за спиной.",25));
        left.AddChild(Ui.Text("ТАКТИЧЕСКАЯ ОБОРОНА · 1941–1945",14,Ui.Muted));
        left.AddChild(Ui.Button("В БОЙ  →",()=>app.StartBattle(app.Store.Current.HighestUnlocked),true));
        left.AddChild(Ui.Button("Кампания  /  1000 операций",()=>app.ShowCampaign()));
        left.AddChild(Ui.Button("Выживание",()=>app.StartBattle(0,true)));
        var pair=Ui.HBox(); var arm=Ui.Button("Арсенал",()=>app.ShowArmory()); arm.SizeFlagsHorizontal=SizeFlags.ExpandFill; pair.AddChild(arm);
        var research=Ui.Button("Исследования",()=>app.ShowResearch()); research.SizeFlagsHorizontal=SizeFlags.ExpandFill; pair.AddChild(research); left.AddChild(pair); left.AddChild(Ui.Spacer());
        var art=Ui.VBox(12); art.SizeFlagsHorizontal=SizeFlags.ExpandFill; row.AddChild(art);
        var hero=new HeroPanel(); hero.SizeFlagsVertical=SizeFlags.ExpandFill; art.AddChild(hero);
        var detail=Ui.HBox(); detail.AddChild(Ui.Text("06   ТИПОВ УКРЕПЛЕНИЙ",16,Ui.Muted)); detail.AddChild(Ui.Spacer()); detail.AddChild(Ui.Text("36   МОДЕЛЕЙ",16,Ui.Gold)); art.AddChild(detail);
        var bottom=Ui.HBox(); bottom.AddChild(Ui.Text("ОПЕРАЦИЯ "+(app.Store.Current.HighestUnlocked+1).ToString("D3")+"  /  "+app.Content.Campaign.Missions[app.Store.Current.HighestUnlocked].Title,16,Ui.Muted));
        bottom.AddChild(Ui.Spacer()); bottom.AddChild(Ui.Text("◆ "+app.Store.Current.Gems+"   АРСЕНАЛ",16,Ui.Gold)); page.AddChild(bottom);
        if(app.Store.Warning is not null) page.AddChild(Ui.Wrap(app.Store.Warning,15,Ui.Red));
    }
}

public partial class HeroPanel : Control
{
    public HeroPanel() { CustomMinimumSize=new Vector2(480,470); MouseFilter=MouseFilterEnum.Ignore; }
    public override void _Draw()
    {
        var r=new Rect2(Vector2.Zero,Size); DrawStyleBox(Ui.Box(new Color("#1e3027"),12,1),r);
        for(int i=0;i<20;i++) DrawLine(new Vector2(i*50,0),new Vector2(i*50,Size.Y),new Color(1,1,1,.025f));
        for(int i=0;i<20;i++) DrawLine(new Vector2(0,i*50),new Vector2(Size.X,i*50),new Color(1,1,1,.025f));
        var c=Size*.5f;
        DrawArc(c,Size.X*.37f,0,Mathf.Tau,100,new Color("#465c43"),2,true);
        DrawArc(c,Size.X*.27f,-.7f,3.8f,70,Ui.Gold,2,true);
        DrawLine(c-new Vector2(Size.X*.42f,0),c+new Vector2(Size.X*.42f,0),new Color("#465c43"),1);
        DrawLine(c-new Vector2(0,Size.Y*.4f),c+new Vector2(0,Size.Y*.4f),new Color("#465c43"),1);
        DrawSetTransform(c,-.4f,Vector2.One);
        var texture=GD.Load<Texture2D>("res://Art/Models/cannon_1.svg");
        DrawTextureRect(texture,new Rect2(-210,-210,420,420),false);
        DrawSetTransform(Vector2.Zero);
        DrawString(ThemeDB.FallbackFont,new Vector2(26,35),"САУ «БАСТИОН»",HorizontalAlignment.Left,-1,20,Ui.Ink);
        DrawString(ThemeDB.FallbackFont,new Vector2(26,Size.Y-30),"ПРОТИВОТАНКОВАЯ ПОЗИЦИЯ / 04",HorizontalAlignment.Left,-1,14,Ui.Muted);
    }
}
