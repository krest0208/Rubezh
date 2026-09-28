using Godot;

namespace Rubezh.Game;

public static class Ui
{
    public static readonly Color Ink=new("#e2e6cf"), Muted=new("#92a697"), Gold=new("#dcb563"), Panel=new("#1b2b24"), Edge=new("#3e5040"), Red=new("#cf7963");
    public static StyleBoxFlat Box(Color c,int radius=8,int border=0)
    {
        var s=new StyleBoxFlat{BgColor=c,BorderColor=Edge,ContentMarginLeft=18,ContentMarginRight=18,ContentMarginTop=12,ContentMarginBottom=12};
        s.SetCornerRadiusAll(radius); s.SetBorderWidthAll(border); return s;
    }
    public static Theme MakeTheme()
    {
        var t=new Theme{DefaultFontSize=18};
        t.SetColor("font_color","Label",Ink); t.SetColor("font_color","Button",Ink);
        t.SetColor("font_disabled_color","Button",new Color("#627365"));
        t.SetStylebox("normal","Button",Box(Panel,7,1)); t.SetStylebox("hover","Button",Box(new Color("#334636"),7,1));
        t.SetStylebox("pressed","Button",Box(new Color("#55513a"),7,1)); t.SetStylebox("disabled","Button",Box(new Color("#17221d"),7,1));
        var focus=Box(Colors.Transparent,7,2); focus.BorderColor=Gold; t.SetStylebox("focus","Button",focus);
        t.SetStylebox("panel","PanelContainer",Box(Panel,10,1));
        return t;
    }
    public static Label Text(string text,int size=18,Color? color=null)
    {
        var l=new Label{Text=text,MouseFilter=Control.MouseFilterEnum.Ignore}; l.AddThemeFontSizeOverride("font_size",size);
        if(color.HasValue) l.AddThemeColorOverride("font_color",color.Value); return l;
    }
    public static Label Wrap(string text,int size=18,Color? color=null)
    { var l=Text(text,size,color); l.AutowrapMode=TextServer.AutowrapMode.WordSmart; l.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill; return l; }
    public static Button Button(string text,Action action,bool accent=false)
    {
        var b=new Button{Text=text,CustomMinimumSize=new Vector2(0,48),MouseDefaultCursorShape=Control.CursorShape.PointingHand}; b.Pressed+=action;
        if(accent) { b.AddThemeStyleboxOverride("normal",Box(Gold)); b.AddThemeColorOverride("font_color",new Color("#19241c")); }
        return b;
    }
    public static VBoxContainer VBox(int gap=14) { var b=new VBoxContainer(); b.AddThemeConstantOverride("separation",gap); return b; }
    public static HBoxContainer HBox(int gap=14) { var b=new HBoxContainer(); b.AddThemeConstantOverride("separation",gap); return b; }
    public static Control Spacer() => new(){SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsVertical=Control.SizeFlags.ExpandFill,MouseFilter=Control.MouseFilterEnum.Ignore};
    public static MarginContainer Margin(Control child,int amount=28)
    {
        var m=new MarginContainer(); foreach(var side in new[]{"left","top","right","bottom"}) m.AddThemeConstantOverride("margin_"+side,amount); m.AddChild(child); return m;
    }
    public static PanelContainer Card(Control child) { var p=new PanelContainer(); p.AddChild(child); return p; }
    public static TextureRect Model(string asset,int size=180)
    { return new TextureRect{Texture=GD.Load<Texture2D>("res://"+asset),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new Vector2(size,size),MouseFilter=Control.MouseFilterEnum.Ignore}; }
    public static void Fill(Control c) => c.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
}
