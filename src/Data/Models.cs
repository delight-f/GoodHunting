namespace GoodHunting.Data;

/// <summary>A single resolved drop entry: an item/block code with a quantity range.</summary>
public sealed class DropSpec
{
    public string Type { get; init; } = "item";
    public string Code { get; init; } = "";
    public float Avg { get; init; }
    public float Var { get; init; }

    public DropSpec() { }

    public DropSpec(string type, string code, float avg, float var)
    {
        Type = type;
        Code = code;
        Avg = avg;
        Var = var;
    }
}

/// <summary>Changed data for one expanded entity type (e.g. "bear-black-adult-male").</summary>
public sealed class EntityEntry
{
    public bool DropsChanged { get; init; }
    public DropSpec[] Drops { get; init; } = System.Array.Empty<DropSpec>();

    public bool HealthChanged { get; init; }
    public float MaxHealth { get; init; }
    public float CurrentHealth { get; init; }
}
