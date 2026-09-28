using System.Numerics;
using System.Text.Json;

namespace Rubezh.Core;

public sealed record TowerDefinition(string Id, string Name, int Cost, float Range, float Damage, float Rate, float Pen, int Unlock, string Desc, float Splash = 0);
public sealed record EnemyDefinition(string Name, float Hp, float Speed, float Armor, int Reward, int Leak, float Size);
public sealed record ResearchDefinition(string Id, string Name, int Max, float Price, string Desc);
public sealed record Mission(int Id, string Title, string Place, string Date, int Chapter, string Theme, int Map, string Weather, string Trait, string Story, string Objective, string Tip, string Outro, string Letter, int Waves, float Rank, int FirstReward, int RepeatReward, int LayoutSeed);
public sealed record Campaign(int CampaignSize, Mission[] Missions);
public sealed record ModelDefinition(string Id, string Unit, string Name, string Description, int Variant, int Price, string Asset);

public sealed class Content
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public required TowerDefinition[] Towers { get; init; }
    public required Dictionary<string, EnemyDefinition> Enemies { get; init; }
    public required ResearchDefinition[] Research { get; init; }
    public required Campaign Campaign { get; init; }
    public required ModelDefinition[] Models { get; init; }
    public required float[][][] Paths { get; init; }
    public required float[][][] AdvancedPaths { get; init; }
    public static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidDataException(typeof(T).Name);
    public static Content Load(Func<string,string> read, string language = "ru") => new()
    {
        Towers = Parse<TowerDefinition[]>(read("towers.json")), Enemies = Parse<Dictionary<string,EnemyDefinition>>(read("enemies.json")),
        Research = Parse<ResearchDefinition[]>(read("research.json")), Campaign = Parse<Campaign>(read($"campaign.{language}.json")),
        Models = Parse<ModelDefinition[]>(read("models.json")), Paths = Parse<float[][][]>(read("paths.json")), AdvancedPaths = Parse<float[][][]>(read("advanced_paths.json"))
    };
    public ModelDefinition ModelFor(string unit, string? selected) => Models.FirstOrDefault(m => m.Unit == unit && m.Id == selected) ?? Models.First(m => m.Unit == unit && m.Variant == 0);
}

public sealed class Route
{
    public Vector2[] Points { get; }
    public float[] Lengths { get; }
    public float Length => Lengths[^1];
    public Route(IEnumerable<Vector2> points)
    {
        Points = points.ToArray(); Lengths = new float[Points.Length];
        for (int i = 1; i < Points.Length; i++) Lengths[i] = Lengths[i-1] + Vector2.Distance(Points[i-1], Points[i]);
    }
    public (Vector2 Position, float Angle) At(float distance)
    {
        distance = Math.Clamp(distance, 0, Length); int i = 1;
        while (i < Lengths.Length - 1 && Lengths[i] < distance) i++;
        var a = Points[i-1]; var b = Points[i];
        return (Vector2.Lerp(a,b,(distance-Lengths[i-1])/(Lengths[i]-Lengths[i-1])), MathF.Atan2(b.Y-a.Y,b.X-a.X));
    }
    public float DistanceTo(Vector2 p)
    {
        float min = float.MaxValue;
        for (int i = 1; i < Points.Length; i++)
        {
            var a = Points[i-1]; var d = Points[i]-a;
            var t = Math.Clamp(Vector2.Dot(p-a,d)/d.LengthSquared(),0,1);
            min = Math.Min(min, Vector2.Distance(p,a+d*t));
        }
        return min;
    }
}
