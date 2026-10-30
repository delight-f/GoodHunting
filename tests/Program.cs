using System.Text.Json;
using System.Text.RegularExpressions;
using GoodHunting;
using GoodHunting.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoodHunting.Tests;

/// <summary>
/// Lightweight self-contained test runner (no external test framework / network needed).
/// Exit code 0 = all pass, 1 = failure. Run from the repository root:
///     dotnet run --project tests
/// </summary>
public static class Program
{
    private static int failures;
    private static int checks;

    public static int Main(string[] args)
    {
        var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

        ClassifierTests();
        ClampTests();
        MultiplierTests();
        DropJsonTests();
        ModdedScalingTests();
        ParityTests(Path.Combine(root, "tools", "golden_drops.json"));

        Console.WriteLine();
        Console.WriteLine($"{checks - failures}/{checks} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void ClassifierTests()
    {
        Section("classifier");
        Check(DropCategories.Classify("hide-raw-bear-black-complete") == DropCategory.Hide, "hide-raw-* -> hide");
        Check(DropCategories.Classify("hide-raw-huge") == DropCategory.Hide, "hide-raw-huge -> hide");
        Check(DropCategories.Classify("fat") == DropCategory.Fat, "fat -> fat");
        Check(DropCategories.Classify("redmeat-raw") == DropCategory.Meat, "redmeat-raw -> meat");
        Check(DropCategories.Classify("bushmeat-raw") == DropCategory.Meat, "bushmeat-raw -> meat");
        Check(DropCategories.Classify("poultry-raw") == DropCategory.Meat, "poultry-raw -> meat");
        Check(DropCategories.Classify("game:fish-raw") == DropCategory.Meat, "game:fish-raw -> meat");
        Check(DropCategories.Classify("feather") == DropCategory.Feather, "feather -> feather");
        Check(DropCategories.Classify("bone") == DropCategory.None, "bone -> none");
        Check(DropCategories.Classify("bovinae:pelt-female-bos") == DropCategory.Hide, "species pelt -> hide");
        Check(DropCategories.Classify(null) == DropCategory.None, "null -> none");
    }

    private static void ClampTests()
    {
        Section("clamp");
        Check(GoodHuntingConfig.Clamp(1_000_000f) == 100f, "huge -> 100");
        Check(GoodHuntingConfig.Clamp(-5f) == 0f, "negative -> 0");
        Check(GoodHuntingConfig.Clamp(float.NaN) == 1f, "NaN -> identity");
        Check(GoodHuntingConfig.Clamp(2.5f) == 2.5f, "in-range unchanged");

        var cfg = new GoodHuntingConfig { HideMultiplier = 999f, AllMultiplier = -1f };
        Check(cfg.ClampAll(), "ClampAll reports change");
        Check(cfg.HideMultiplier == 100f && cfg.AllMultiplier == 0f, "fields clamped");
    }

    private static void MultiplierTests()
    {
        Section("multipliers");

        var identity = new GoodHuntingConfig();
        var meat = new DropSpec("item", "redmeat-raw", 10f, 4f);
        var scaledIdentity = DropScaling.Scale(meat, identity);
        Check(scaledIdentity.Avg == 10f && scaledIdentity.Var == 4f, "1.0 is identity");

        var cfg = new GoodHuntingConfig { MeatMultiplier = 2f };
        var doubled = DropScaling.Scale(meat, cfg);
        Check(doubled.Avg == 20f && doubled.Var == 8f, "2x doubles avg and var");
        Check(doubled.Code == "redmeat-raw" && doubled.Type == "item", "2x leaves code/type untouched");

        var hide = new DropSpec("item", "hide-raw-large", 1f, 0f);
        Check(DropScaling.Scale(hide, cfg).Avg == 1f, "meat multiplier does not touch hide");

        // 'all' applies last, on top of the category multiplier (multiplicative)
        var compound = new GoodHuntingConfig { MeatMultiplier = 2f, AllMultiplier = 3f };
        var c = DropScaling.Scale(meat, compound);
        Check(c.Avg == 60f && c.Var == 24f, "all compounds with category (2*3)");
        Check(DropScaling.Scale(new DropSpec("item", "bone", 5f, 1f), compound).Avg == 15f, "all applies to uncategorised drops");

        // disabled => identity regardless of multipliers
        var off = new GoodHuntingConfig { Enabled = false, MeatMultiplier = 5f, AllMultiplier = 7f };
        var o = DropScaling.Scale(meat, off);
        Check(o.Avg == 10f && o.Var == 4f, "disabled => identity");

        // scaling must not mutate the shared source table
        var source = new DropSpec("item", "fat", 5f, 2f);
        DropScaling.Scale(source, new GoodHuntingConfig { FatMultiplier = 4f });
        Check(source.Avg == 5f && source.Var == 2f, "source table not mutated");
    }

    private static void ParityTests(string goldenPath)
    {
        Section("parity vs golden (1x)");
        if (!File.Exists(goldenPath))
        {
            Fail($"golden fixture missing: {goldenPath}");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(goldenPath));
        var rootEl = doc.RootElement;
        var goldenEntities = rootEl.GetProperty("entities");
        var goldenFish = rootEl.GetProperty("fish");

        // every golden entity must exist in the C# table, exactly
        var seen = new HashSet<string>();
        foreach (var prop in goldenEntities.EnumerateObject())
        {
            seen.Add(prop.Name);
            var code = prop.Name;
            var g = prop.Value;
            if (!GoodHuntingData.Entities.TryGetValue(code, out var entry))
            {
                Fail($"entity missing from C# table: {code}");
                continue;
            }

            if (g.TryGetProperty("drops", out var goldDrops))
            {
                Check(entry.DropsChanged, $"{code}: DropsChanged");
                Check(entry.Drops.Length == goldDrops.GetArrayLength(), $"{code}: drop count {entry.Drops.Length} == {goldDrops.GetArrayLength()}");
                for (var i = 0; i < Math.Min(entry.Drops.Length, goldDrops.GetArrayLength()); i++)
                {
                    var gd = goldDrops[i];
                    var cd = entry.Drops[i];
                    Check(cd.Code == gd.GetProperty("code").GetString(), $"{code}[{i}]: code {cd.Code}");
                    Check(cd.Type == gd.GetProperty("type").GetString(), $"{code}[{i}]: type");
                    Check(Math.Abs(cd.Avg - gd.GetProperty("avg").GetSingle()) < 1e-4, $"{code}[{i}]: avg {cd.Avg}");
                    Check(Math.Abs(cd.Var - gd.GetProperty("var").GetSingle()) < 1e-4, $"{code}[{i}]: var {cd.Var}");
                }
            }
            else
            {
                Check(!entry.DropsChanged, $"{code}: no unexpected drop change");
            }

            if (g.TryGetProperty("health", out var goldHealth))
            {
                Check(entry.HealthChanged, $"{code}: HealthChanged");
                Check(Math.Abs(entry.MaxHealth - goldHealth.GetProperty("max").GetSingle()) < 1e-4, $"{code}: max {entry.MaxHealth}");
                Check(Math.Abs(entry.CurrentHealth - goldHealth.GetProperty("current").GetSingle()) < 1e-4, $"{code}: current {entry.CurrentHealth}");
            }
        }

        foreach (var key in GoodHuntingData.Entities.Keys)
        {
            Check(seen.Contains(key), $"no extra entity in C# table: {key}");
        }

        var seenFish = new HashSet<string>();
        foreach (var prop in goldenFish.EnumerateObject())
        {
            seenFish.Add(prop.Name);
            if (!GoodHuntingData.Fish.TryGetValue(prop.Name, out var fish))
            {
                Fail($"fish missing from C# table: {prop.Name}");
                continue;
            }
            var arr = prop.Value;
            Check(fish.Length == arr.GetArrayLength(), $"{prop.Name}: fish count");
            for (var i = 0; i < Math.Min(fish.Length, arr.GetArrayLength()); i++)
            {
                Check(fish[i].Code == arr[i].GetProperty("code").GetString(), $"{prop.Name}[{i}]: code");
                Check(Math.Abs(fish[i].Avg - arr[i].GetProperty("avg").GetSingle()) < 1e-4, $"{prop.Name}[{i}]: avg");
                Check(Math.Abs(fish[i].Var - arr[i].GetProperty("var").GetSingle()) < 1e-4, $"{prop.Name}[{i}]: var");
            }
        }
        foreach (var key in GoodHuntingData.Fish.Keys)
        {
            Check(seenFish.Contains(key), $"no extra fish in C# table: {key}");
        }

        Console.WriteLine($"  golden: {goldenEntities.EnumerateObject().Count()} entity types, {goldenFish.EnumerateObject().Count()} fish variants");
    }

    private static void DropJsonTests()
    {
        Section("drop json");

        // A float must survive serialisation without being promoted to a double, or the JSON
        // we inject would carry values like 0.7200000286102295 into the engine.
        var drops = DropJson.Drops(new[] { new DropSpec("item", "hide-raw-large", 0.72f, 0f) });
        var text = drops.ToString(Formatting.None);

        Check((float)drops[0]!["quantity"]!["avg"]! == 0.72f, "float survives the round trip exactly");
        Check(drops[0]!["quantity"]!["var"]!.Type == JTokenType.Float, "var is written as a float");
        Check(text.Contains("\"avg\":0.72"), $"compact number formatting: {text}");
        Check(!Regex.IsMatch(text, @"\.\d{6,}"), "no float/double promotion artefacts");
        Check(text.Contains("\"type\":\"item\"") && text.Contains("\"code\":\"hide-raw-large\""), "type and code present");

        var scaled = DropJson.Drops(DropScaling.ScaleAll(
            new[] { new DropSpec("item", "redmeat-raw", 40f, 0f) },
            new GoodHuntingConfig { MeatMultiplier = 2f }));
        Check((float)scaled[0]!["quantity"]!["avg"]! == 80f, "configured multiplier reaches the injected json");
    }

    // Nothing here is specific to one mod: the scaler works on whatever drops the engine resolved
    // for another mod's animal, so these cases use a Fauna of the Stone Age style array.
    private static void ModdedScalingTests()
    {
        Section("modded animal drops");

        Check(!new GoodHuntingConfig().ScalesDomain("bovinae"), "empty domain list leaves modded animals alone");
        Check(!new GoodHuntingConfig().ScalesDomain(null), "null domain is not scaled");
        Check(!new GoodHuntingConfig().ScalesDomain("game"), "vanilla domain is never scaled twice");
        Check(!new GoodHuntingConfig { ModdedAnimalDomains = null! }.ScalesDomain("bovinae"), "null list is safe");
        Check(!new GoodHuntingConfig { Enabled = false, ModdedAnimalDomains = new[] { "*" } }.ScalesDomain("bovinae"),
            "disabled mod scales nothing");

        var listed = new GoodHuntingConfig { ModdedAnimalDomains = new[] { "bovinae" } };
        Check(listed.ScalesDomain("bovinae"), "listed domain is scaled");
        Check(listed.ScalesDomain("Bovinae"), "domain match is case-insensitive");
        Check(!listed.ScalesDomain("caninae"), "unlisted domain is untouched");

        var everyDomain = new GoodHuntingConfig { ModdedAnimalDomains = new[] { "*" } };
        Check(everyDomain.ScalesDomain("caninae") && everyDomain.ScalesDomain("bovinae"), "'*' covers every modded domain");
        Check(!everyDomain.ScalesDomain("game"), "'*' still excludes the vanilla domain");

        var source = ModdedDrops();

        // At 1x the array must come back untouched, including int-typed numbers staying ints.
        var identity = DropScaling.ScaleExisting(source, new GoodHuntingConfig());
        Check(JToken.DeepEquals(source, identity), "1.0 leaves the array structurally identical");
        Check(identity[2]!["quantity"]!["avg"]!.ToString(Formatting.None) == "40",
            "1.0 keeps integer formatting");

        var cfg = new GoodHuntingConfig { MeatMultiplier = 2f, HideMultiplier = 0.5f };
        var scaled = DropScaling.ScaleExisting(source, cfg);
        Check((float)scaled[0]!["quantity"]!["avg"]! == 1.5f, "hide halved");
        Check((float)scaled[2]!["quantity"]!["avg"]! == 80f, "redmeat avg doubled");
        Check((float)scaled[2]!["quantity"]!["var"]! == 12f, "redmeat var doubled");
        Check((float)scaled[3]!["quantity"]!["avg"]! == 20f, "bushmeat doubled");
        Check((float)scaled[1]!["quantity"]!["avg"]! == 1f, "species pelt treated as a hide and halved");
        Check((float)scaled[4]!["quantity"]!["avg"]! == 8f, "fat untouched by a meat multiplier");
        Check((float)scaled[5]!["quantity"]!["avg"]! == 1f, "uncategorised drop untouched");

        var all = DropScaling.ScaleExisting(source, new GoodHuntingConfig { AllMultiplier = 3f });
        Check((float)all[1]!["quantity"]!["avg"]! == 6f, "allMultiplier reaches modded item codes");
        Check((float)all[5]!["quantity"]!["avg"]! == 3f, "allMultiplier reaches uncategorised drops");
        Check((float)all[6]!["quantity"]!["min"]! == 6f && (float)all[6]!["quantity"]!["max"]! == 12f,
            "allMultiplier reaches min/max shapes");

        // A pelt written as {offset:1, avg:0, var:0} drops exactly one; only the offset carries
        // the count, so it must scale or the hide multiplier does nothing to it.
        Check((float)identity[8]!["quantity"]!["offset"]! == 1f, "1.0 leaves the flat offset alone");
        Check((float)scaled[8]!["quantity"]!["offset"]! == 0.5f, "flat offset follows the hide multiplier");
        Check((float)all[8]!["quantity"]!["offset"]! == 3f, "flat offset follows allMultiplier");

        Check(DropScaling.ScaleExisting(source, cfg)[7]!.ToString(Formatting.None)
              == source[7]!.ToString(Formatting.None),
            "entry without a quantity is passed through unchanged");
        Check((float)source[0]!["quantity"]!["avg"]! == 3f && (float)source[2]!["quantity"]!["avg"]! == 40f,
            "source array is not mutated");

        // The system always re-scales its saved baseline, so a reload must land on the same values.
        var baseline = ModdedDrops();
        Check(JToken.DeepEquals(
                DropScaling.ScaleExisting(baseline, cfg),
                DropScaling.ScaleExisting(baseline, cfg)),
            "scaling from the baseline is repeatable");
    }

    private static JArray ModdedDrops() => JArray.Parse(@"[
        { ""type"": ""item"", ""code"": ""hide-raw-huge"", ""quantity"": { ""avg"": 3, ""var"": 1 } },
        { ""type"": ""item"", ""code"": ""bovinae:pelt-aurochs"", ""quantity"": { ""avg"": 2 } },
        { ""type"": ""item"", ""code"": ""redmeat-raw"", ""quantity"": { ""avg"": 40, ""var"": 6 } },
        { ""type"": ""item"", ""code"": ""bushmeat-raw"", ""quantity"": { ""avg"": 10, ""var"": 2 } },
        { ""type"": ""item"", ""code"": ""fat"", ""quantity"": { ""avg"": 8 } },
        { ""type"": ""item"", ""code"": ""bone"", ""quantity"": { ""avg"": 1 } },
        { ""type"": ""item"", ""code"": ""game:hide-raw-large"", ""quantity"": { ""min"": 2, ""max"": 4 } },
        { ""type"": ""item"", ""code"": ""sinew"" },
        { ""type"": ""item"", ""code"": ""bovinae:pelt-bull"", ""quantity"": { ""offset"": 1, ""avg"": 0, ""var"": 0 } }
    ]");

    private static void Section(string name)
    {
        Console.WriteLine();
        Console.WriteLine($"== {name} ==");
    }

    private static void Check(bool ok, string what)
    {
        checks++;
        if (!ok)
        {
            failures++;
            Console.WriteLine($"  FAIL: {what}");
        }
    }

    private static void Fail(string what)
    {
        checks++;
        failures++;
        Console.WriteLine($"  FAIL: {what}");
    }
}
