using System.Text.Json;

namespace Rubezh.Core;

public sealed class Profile
{
    public int Version { get; set; } = 1;
    public int Coins { get; set; }
    public int Gems { get; set; } = 18;
    public int HighestUnlocked { get; set; }
    public int BestSurvival { get; set; }
    public string Language { get; set; } = "ru";
    public Dictionary<int,int> Stars { get; set; } = new();
    public Dictionary<string,int> Research { get; set; } = new();
    public HashSet<string> OwnedModels { get; set; } = new();
    public Dictionary<string,string> EquippedModels { get; set; } = new();
    public int Skill(string id) => Research.GetValueOrDefault(id);
    public Profile Copy() => Content.Parse<Profile>(JsonSerializer.Serialize(this));
    public static int ResearchCost(ResearchDefinition d, int level) => (int)MathF.Round((90+level*70+level*level*22)*d.Price/10)*10;
    public bool BuyResearch(ResearchDefinition d)
    {
        int level=Skill(d.Id), cost=ResearchCost(d,level);
        if (level>=d.Max || Coins<cost) return false;
        Coins-=cost; Research[d.Id]=level+1; return true;
    }
    public bool BuyOrEquip(ModelDefinition model)
    {
        if (model.Variant != 0 && !OwnedModels.Contains(model.Id))
        {
            if (Gems < model.Price) return false;
            Gems -= model.Price; OwnedModels.Add(model.Id);
        }
        EquippedModels[model.Unit] = model.Id; return true;
    }
    public void RecordWin(Mission mission, int lives)
    {
        bool first = !Stars.ContainsKey(mission.Id);
        Stars[mission.Id] = Math.Max(Stars.GetValueOrDefault(mission.Id), lives >= 18 ? 3 : lives >= 10 ? 2 : 1);
        HighestUnlocked = Math.Min(999, Math.Max(HighestUnlocked, mission.Id+1));
        Coins += first ? mission.FirstReward : mission.RepeatReward;
        if (first) Gems += 3;
    }
}

/// <summary>Write-through transactions: failures never consume the in-memory balance.</summary>
public sealed class ProfileStore
{
    readonly string path;
    public Profile Current { get; private set; }
    public string? Warning { get; private set; }
    public ProfileStore(string path)
    {
        this.path=path; Current=new();
        if (!File.Exists(path)) return;
        try { Current=Read(path); }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            Warning="Сохранение повреждено. ";
            try { Current=Read(path+".bak"); Warning+="Загружена резервная копия."; }
            catch { Warning+="Начат новый профиль; исходный файл сохранён."; }
        }
    }
    static Profile Read(string path)
    {
        var p=Content.Parse<Profile>(File.ReadAllText(path));
        if (p.Version != 1 || p.Coins < 0 || p.Gems < 0 || p.HighestUnlocked is < 0 or > 999 || p.Stars is null || p.Research is null || p.OwnedModels is null || p.EquippedModels is null) throw new InvalidDataException("Invalid profile");
        return p;
    }
    public bool Transact(Func<Profile,bool> change)
    {
        var next=Current.Copy(); if (!change(next)) return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temp=path+".tmp";
            using (var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            { JsonSerializer.Serialize(stream,next,Content.JsonOptions); stream.Flush(true); }
            if (File.Exists(path))
            {
                // Only promote a validated save to backup; preserve corrupt originals separately.
                try { Read(path); File.Copy(path,path+".bak",true); }
                catch (Exception ex) when (ex is JsonException or InvalidDataException) { File.Copy(path,path+".corrupt",true); }
            }
            File.Move(temp,path,true); Current=next; Warning=null; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Warning="Не удалось сохранить: "+ex.Message; return false; }
    }
}
