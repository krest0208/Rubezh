using Godot;
using Rubezh.Core;
using System.IO.Compression;

namespace Rubezh.Game;

public partial class App : Control
{
    public Content Content { get; private set; }=null!;
    public ProfileStore Store { get; private set; }=null!;
    Control? screen;
    double captureSeconds;
    string? capturePath;
    public override void _Ready()
    {
        Theme=Ui.MakeTheme();
        var args=OS.GetCmdlineUserArgs();
        int captureIndex=Array.IndexOf(args,"--capture");
        if(captureIndex>=0 && captureIndex+1<args.Length)capturePath=args[captureIndex+1];
        string save=args.Contains("--smoke-test")?"user://smoke-profile.json":"user://profile-v1.json";
        Store=new ProfileStore(ProjectSettings.GlobalizePath(save));
        LoadContent(); ShowMenu();
        if(args.Contains("--armory")) ShowArmory();
        if(args.Contains("--battle")) StartBattle(0);
        if(args.Contains("--smoke-test")) Callable.From(SmokeTest).CallDeferred();
    }
    public override void _Process(double delta)
    {
        if(capturePath is null)return;
        captureSeconds+=delta;
        if(captureSeconds<5)return;
        var error=GetViewport().GetTexture().GetImage().SavePng(capturePath);
        GD.Print("CAPTURE "+capturePath+" "+error);GetTree().Quit(error==Error.Ok?0:1);
    }
    void LoadContent() => Content=Content.Load(ReadData,Store.Current.Language);
    static string ReadData(string name)
    {
        if(!name.StartsWith("campaign."))return Godot.FileAccess.GetFileAsString("res://Data/"+name);
        using var file=Godot.FileAccess.Open("res://Data/"+name+".gz",Godot.FileAccess.ModeFlags.Read);
        using var memory=new MemoryStream(file.GetBuffer((long)file.GetLength()));
        using var gzip=new GZipStream(memory,CompressionMode.Decompress);
        using var reader=new StreamReader(gzip);return reader.ReadToEnd();
    }
    public void Show(Control next)
    {
        if(screen is not null) { RemoveChild(screen); screen.QueueFree(); }
        screen=next; AddChild(next); Ui.Fill(next);
        if(OS.IsDebugBuild()) GD.Print("RUBEZH_SCREEN="+next.GetType().Name);
    }
    public void ShowMenu() => Show(new MenuView(this));
    public void ShowArmory(string? unit=null) => Show(new ArmoryView(this,unit));
    public void ShowCampaign(int page=-1) => Show(new CampaignView(this,page<0?Store.Current.HighestUnlocked/20:page));
    public void ShowResearch() => Show(new ResearchView(this));
    public void StartBattle(int id,bool survival=false) => Show(new BattleView(this,new Battle(Content,Store.Current,id,survival)));
    public bool Transaction(Func<Profile,bool> change) => Store.Transact(change);
    public void ChangeLanguage(string language)
    { if(Transaction(p=>{p.Language=language;return true;})) { LoadContent(); ShowCampaign(); } }
    public VBoxContainer Page(string section,Action back)
    {
        var col=Ui.VBox(22); var header=Ui.HBox(); header.AddChild(Ui.Button("←  ШТАБ",back));
        header.AddChild(Ui.Text("РУБЕЖ   /   "+section,23)); header.AddChild(Ui.Spacer());
        header.AddChild(Ui.Text($"{Store.Current.Coins}  жетонов     ◆ {Store.Current.Gems}",20,Ui.Gold)); col.AddChild(header); return col;
    }
    public void SmokeTest()
    {
        try
        {
            foreach(var model in Content.Models) if(GD.Load<Texture2D>("res://"+model.Asset) is null) throw new Exception(model.Asset);
            ShowCampaign(); ShowResearch(); ShowArmory(); StartBattle(0); ShowMenu();
            GD.Print("GODOT_SMOKE_OK: all 36 textures, campaign, research, armory, battle and menu constructed.");
            GetTree().Quit();
        }
        catch(Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
