using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("PokemonBattle-DataGen/1.0 (https://github.com/ChoW0n/PokemonlikeProject)");
var pokemonSb = new StringBuilder();
var moveSb = new StringBuilder();
var abilitySb = new StringBuilder();

var typeMap = new Dictionary<string, string>
{
    ["normal"] = "Normal", ["fire"] = "Fire", ["water"] = "Water", ["electric"] = "Electric",
    ["grass"] = "Grass", ["ice"] = "Ice", ["fighting"] = "Fighting", ["poison"] = "Poison",
    ["ground"] = "Ground", ["flying"] = "Flying", ["psychic"] = "Psychic", ["bug"] = "Bug",
    ["rock"] = "Rock", ["ghost"] = "Ghost", ["dragon"] = "Dragon", ["dark"] = "Dark",
    ["steel"] = "Steel", ["fairy"] = "Fairy"
};

var abilityCache = new Dictionary<string, string>();
var abilityWritten = new HashSet<string>();
var moveCache = new HashSet<string>();
var unsupportedMoveCache = new HashSet<string>();
var evolutionChainCache = new Dictionary<string, PokeApiEvolutionChain?>();
var nameToId = new Dictionary<string, int>();

string CleanText(string raw)
{
    string s = raw.Replace("\n", " ").Replace("\f", " ").Replace("\r", " ")
        .Replace("\"", "'")
        .Replace("“", "'").Replace("”", "'")
        .Replace("‘", "'").Replace("’", "'")
        .Replace("\\", "/");
    return Regex.Replace(s, @"\s+", " ").Trim();
}

moveSb.AppendLine("namespace PokemonBattle.Models;");
moveSb.AppendLine();
moveSb.AppendLine("public static class MoveDatabase");
moveSb.AppendLine("{");
moveSb.AppendLine("    // MoveRuleMetadata owns runtime-only move behavior and is intentionally not generated.");
moveSb.AppendLine("    public static Dictionary<string, Move> All = new Dictionary<string, Move>();");
moveSb.AppendLine();
moveSb.AppendLine("    static MoveDatabase()");
moveSb.AppendLine("    {");

abilitySb.AppendLine("namespace PokemonBattle.Models;");
abilitySb.AppendLine();
abilitySb.AppendLine("public static partial class AbilityDatabase");
abilitySb.AppendLine("{");
abilitySb.AppendLine("    public static Dictionary<string, AbilityInfo> All = new Dictionary<string, AbilityInfo>();");
abilitySb.AppendLine();
abilitySb.AppendLine("    static AbilityDatabase()");
abilitySb.AppendLine("    {");

pokemonSb.AppendLine("namespace PokemonBattle.Models;");
pokemonSb.AppendLine();
pokemonSb.AppendLine("public static class PokemonDatabase");
pokemonSb.AppendLine("{");
pokemonSb.AppendLine("    public static Dictionary<int, PokemonData> All = new Dictionary<int, PokemonData>();");
pokemonSb.AppendLine();
pokemonSb.AppendLine("    static PokemonDatabase()");
pokemonSb.AppendLine("    {");

for (int id = 1; id <= 721; id++)
{
    try
    {
        var basic = await http.GetFromJsonAsync<PokeApiPokemon>($"https://pokeapi.co/api/v2/pokemon/{id}");
        if (basic != null) nameToId[basic.name] = id;
    }
    catch { }
}
Console.WriteLine("1차 패스(이름-번호 매핑) 완료");

for (int id = 1; id <= 721; id++)
{
    try
    {
        var pokemon = await http.GetFromJsonAsync<PokeApiPokemon>($"https://pokeapi.co/api/v2/pokemon/{id}");
        var species = await http.GetFromJsonAsync<PokeApiSpeciesFull>($"https://pokeapi.co/api/v2/pokemon-species/{id}");
        if (pokemon == null || species == null) continue;

        string korName = species.names.FirstOrDefault(n => n.language.name == "ko")?.name ?? pokemon.name;
        string type1 = typeMap[pokemon.types[0].type.name];
        string type2Line = pokemon.types.Count > 1 ? $"PokemonType.{typeMap[pokemon.types[1].type.name]}" : "null";

        int hp = pokemon.stats.First(s => s.stat.name == "hp").base_stat;
        int atk = pokemon.stats.First(s => s.stat.name == "attack").base_stat;
        int def = pokemon.stats.First(s => s.stat.name == "defense").base_stat;
        int spAtk = pokemon.stats.First(s => s.stat.name == "special-attack").base_stat;
        int spDef = pokemon.stats.First(s => s.stat.name == "special-defense").base_stat;
        int spd = pokemon.stats.First(s => s.stat.name == "speed").base_stat;

        var apiLearnableMoveKeys = pokemon.moves
            .Where(moveSlot => moveSlot.version_group_details.Any(v =>
                v.move_learn_method.name is "level-up" or "egg" or "tutor" or "machine"))
            .Select(moveSlot => moveSlot.move.name)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var generatedMoveKeys = pokemon.moves
            .Where(moveSlot => moveSlot.version_group_details.Any(v =>
                v.move_learn_method.name is "level-up" or "machine"))
            .Select(moveSlot => moveSlot.move.name)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var learnableMoveKeys = new List<string>();
        foreach (var slug in generatedMoveKeys)
        {
            if (!moveCache.Contains(slug) && !unsupportedMoveCache.Contains(slug))
            {
                var moveDetail = await http.GetFromJsonAsync<PokeApiMoveDetail>($"https://pokeapi.co/api/v2/move/{slug}");
                if (moveDetail == null || !typeMap.ContainsKey(moveDetail.type.name))
                {
                    unsupportedMoveCache.Add(slug);
                    continue;
                }

                string ailmentRaw = moveDetail.meta?.ailment?.name ?? "none";
                bool hasDamage = moveDetail.power != null && moveDetail.power > 0;
                bool hasAilment = ailmentRaw != "none" && ailmentRaw != "unknown" && ailmentRaw != "flinch";
                bool hasStatChange = moveDetail.stat_changes.Count > 0;
                bool hasFlinch = (moveDetail.meta?.flinch_chance ?? 0) > 0 || ailmentRaw == "flinch";
                bool hasHealing = (moveDetail.meta?.healing ?? 0) != 0;
                bool hasDrain = (moveDetail.meta?.drain ?? 0) != 0;

                if (!hasDamage && !hasAilment && !hasStatChange && !hasFlinch && !hasHealing && !hasDrain)
                {
                    unsupportedMoveCache.Add(slug);
                    continue;
                }

                bool isStatus = moveDetail.damage_class.name == "status";
                bool isSpecial = moveDetail.damage_class.name == "special";
                bool alwaysHits = moveDetail.accuracy == null;
                int accuracy = moveDetail.accuracy ?? 100;
                int power = moveDetail.power ?? 0;
                int priority = moveDetail.priority;
                int healingPercent = moveDetail.meta?.healing ?? 0;
                int drainPercent = moveDetail.meta?.drain ?? 0;
                int minHits = moveDetail.meta?.min_hits ?? 1;
                int maxHits = moveDetail.meta?.max_hits ?? 1;

                string ailmentName2 = hasAilment ? ailmentRaw : "none";
                int ailmentChance = moveDetail.meta?.ailment_chance ?? 0;
                if (ailmentName2 != "none" && ailmentChance == 0) ailmentChance = 100;

                int rawFlinch = moveDetail.meta?.flinch_chance ?? 0;
                int flinchChance = ailmentRaw == "flinch"
                    ? ((moveDetail.meta?.ailment_chance ?? 0) == 0 ? 100 : moveDetail.meta!.ailment_chance)
                    : rawFlinch;

                bool targetsSelf = moveDetail.target.name == "user";
                var statChangeParts = moveDetail.stat_changes
                    .Where(sc => sc.stat.name is "attack" or "defense" or "special-attack" or "special-defense" or "speed")
                    .Select(sc => $"new StatChangeEntry {{ Stat = \"{sc.stat.name}\", Change = {sc.change}, TargetsSelf = {(targetsSelf ? "true" : "false")} }}")
                    .ToList();

                int statChance = moveDetail.meta?.stat_chance ?? 0;
                if (statChangeParts.Count > 0 && statChance == 0) statChance = 100;

                string statChangesCode = statChangeParts.Count > 0
                    ? "new List<StatChangeEntry> { " + string.Join(", ", statChangeParts) + " }"
                    : "new List<StatChangeEntry>()";

                string korMoveName = moveDetail.names.FirstOrDefault(n => n.language.name == "ko")?.name ?? moveDetail.name;
                string koFlavor = moveDetail.flavor_text_entries.FirstOrDefault(f => f.language.name == "ko")?.flavor_text ?? "";
                string moveDesc = CleanText(koFlavor);

                moveSb.AppendLine($"        All[\"{slug}\"] = new Move(\"{korMoveName}\", {power}, PokemonType.{typeMap[moveDetail.type.name]}, {moveDetail.pp ?? 10}, {accuracy}, {(alwaysHits ? "true" : "false")}, {priority}, {(isStatus ? "true" : "false")}, {(isSpecial ? "true" : "false")}, \"{ailmentName2}\", {ailmentChance}, {flinchChance}, {statChangesCode}, {statChance}, \"{moveDesc}\", {healingPercent}, {drainPercent}, {minHits}, {maxHits});");

                moveCache.Add(slug);
            }

            if (moveCache.Contains(slug)) learnableMoveKeys.Add(slug);
        }

        if (learnableMoveKeys.Count == 0) learnableMoveKeys.Add("tackle");

        var machineOnlyMoveKeys = pokemon.moves
            .Where(moveSlot => learnableMoveKeys.Contains(moveSlot.move.name, StringComparer.Ordinal))
            .Where(moveSlot => moveSlot.version_group_details.Count > 0
                && moveSlot.version_group_details.All(v => v.move_learn_method.name == "machine"))
            .Select(moveSlot => moveSlot.move.name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        string moveList = string.Join(", ", learnableMoveKeys.Distinct().Select(m => $"\"{m}\""));
        string apiLearnableMoveList = string.Join(", ", apiLearnableMoveKeys.Select(m => $"\"{m}\""));
        string machineOnlyMoveList = machineOnlyMoveKeys.Count == 0
            ? "Array.Empty<string>()"
            : $"new[] {{ {string.Join(", ", machineOnlyMoveKeys.Select(m => $"\"{m}\""))} }}";

        var abilityNames = new List<string>();
        foreach (var slot in pokemon.abilities.Take(2))
        {
            string aslug = slot.ability.name;
            if (!abilityCache.TryGetValue(aslug, out var korAbility))
            {
                var abilityDetail = await http.GetFromJsonAsync<PokeApiAbilityDetail>($"https://pokeapi.co/api/v2/ability/{aslug}");
                korAbility = abilityDetail?.names.FirstOrDefault(n => n.language.name == "ko")?.name ?? aslug;
                abilityCache[aslug] = korAbility;

                string koAbilityFlavor = abilityDetail?.flavor_text_entries.FirstOrDefault(f => f.language.name == "ko")?.flavor_text ?? "";
                string abilityDesc = CleanText(koAbilityFlavor);

                if (!abilityWritten.Contains(korAbility))
                {
                    abilitySb.AppendLine($"        All[\"{korAbility}\"] = new AbilityInfo(\"{korAbility}\", \"{abilityDesc}\");");
                    abilityWritten.Add(korAbility);
                }
            }
            abilityNames.Add(korAbility);
        }
        string abilityList = string.Join(", ", abilityNames.Select(a => $"\"{a}\""));

        string imageUrl = pokemon.sprites.other?.showdown?.front_default ?? pokemon.sprites.front_default ?? "";
        string backImageUrl = pokemon.sprites.other?.showdown?.back_default ?? imageUrl;

        string? nextEvoName = null;
        if (species.evolution_chain != null)
        {
            string chainUrl = species.evolution_chain.url;
            if (!evolutionChainCache.TryGetValue(chainUrl, out var chain))
            {
                chain = await http.GetFromJsonAsync<PokeApiEvolutionChain>(chainUrl);
                evolutionChainCache[chainUrl] = chain;
            }
            if (chain != null) nextEvoName = FindNextEvolution(chain.chain, pokemon.name);
        }

        string evolvesToLine = "null";
        if (nextEvoName != null && nameToId.TryGetValue(nextEvoName, out int nextId))
        {
            evolvesToLine = nextId.ToString();
        }

        pokemonSb.AppendLine($"        All[{id}] = new PokemonData(\"{korName}\", \"{pokemon.name}\", PokemonType.{type1}, {type2Line}, {hp}, {atk}, {def}, {spAtk}, {spDef}, {spd}, new[] {{ {moveList} }}, new[] {{ {abilityList} }}, \"{imageUrl}\", \"{backImageUrl}\", {evolvesToLine}, 5, new[] {{ {apiLearnableMoveList} }}, {machineOnlyMoveList});");

        Console.WriteLine($"[{id}/721] {korName} 완료");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{id}] 실패: {ex.Message}");
    }
}

if (!moveCache.Contains("tackle"))
{
    moveSb.AppendLine("        All[\"tackle\"] = new Move(\"몸통박치기\", 40, PokemonType.Normal, 35, 100, false, 0, false, false, \"none\", 0, 0, new List<StatChangeEntry>(), 0, \"\", 0, 0, 1, 1);");
}

moveSb.AppendLine("    }");
moveSb.AppendLine("}");
abilitySb.AppendLine("    }");
abilitySb.AppendLine("}");
pokemonSb.AppendLine("    }");
pokemonSb.AppendLine("}");

File.WriteAllText("../PokemonBattle/Models/MoveDatabase.cs", moveSb.ToString());
File.WriteAllText("../PokemonBattle/Models/AbilityDatabase.cs", abilitySb.ToString());
File.WriteAllText("../PokemonBattle/Models/PokemonDatabase.cs", pokemonSb.ToString());
Console.WriteLine($"완료! 기술 {moveCache.Count}개, 특성 {abilityWritten.Count}개, 포켓몬 721마리 생성됨");

string? FindNextEvolution(PokeApiChainLink node, string currentName)
{
    if (node.species.name == currentName)
        return node.evolves_to.Count > 0 ? node.evolves_to[0].species.name : null;
    foreach (var child in node.evolves_to)
    {
        var result = FindNextEvolution(child, currentName);
        if (result != null) return result;
    }
    return null;
}

public class PokeApiPokemon
{
    public string name { get; set; } = "";
    public List<PokeApiTypeSlot> types { get; set; } = new();
    public List<PokeApiStat> stats { get; set; } = new();
    public List<PokeApiAbilitySlot> abilities { get; set; } = new();
    public List<PokeApiMoveSlot> moves { get; set; } = new();
    public PokeApiSprites sprites { get; set; } = new();
}
public class PokeApiTypeSlot { public PokeApiTypeInfo type { get; set; } = new(); }
public class PokeApiTypeInfo { public string name { get; set; } = ""; }
public class PokeApiStat { public int base_stat { get; set; } public PokeApiStatInfo stat { get; set; } = new(); }
public class PokeApiStatInfo { public string name { get; set; } = ""; }
public class PokeApiAbilitySlot { public PokeApiAbilityRef ability { get; set; } = new(); }
public class PokeApiAbilityRef { public string name { get; set; } = ""; }

public class PokeApiAbilityDetail
{
    public string name { get; set; } = "";
    public List<PokeApiName> names { get; set; } = new();
    public List<PokeApiAbilityFlavorText> flavor_text_entries { get; set; } = new();
}
public class PokeApiAbilityFlavorText
{
    public string flavor_text { get; set; } = "";
    public PokeApiLanguage language { get; set; } = new();
}

public class PokeApiMoveSlot
{
    public PokeApiMoveRef move { get; set; } = new();
    public List<PokeApiVersionGroupDetail> version_group_details { get; set; } = new();
}
public class PokeApiMoveRef { public string name { get; set; } = ""; }
public class PokeApiVersionGroupDetail { public PokeApiLearnMethod move_learn_method { get; set; } = new(); }
public class PokeApiLearnMethod { public string name { get; set; } = ""; }

public class PokeApiMoveDetail
{
    public string name { get; set; } = "";
    public int? power { get; set; }
    public int? accuracy { get; set; }
    public int? pp { get; set; }
    public int priority { get; set; }
    public PokeApiTypeInfo type { get; set; } = new();
    public PokeApiDamageClass damage_class { get; set; } = new();
    public PokeApiMoveMeta? meta { get; set; }
    public List<PokeApiStatChangeEntry> stat_changes { get; set; } = new();
    public PokeApiTargetRef target { get; set; } = new();
    public List<PokeApiName> names { get; set; } = new();
    public List<PokeApiMoveFlavorText> flavor_text_entries { get; set; } = new();
}
public class PokeApiMoveFlavorText
{
    public string flavor_text { get; set; } = "";
    public PokeApiLanguage language { get; set; } = new();
}
public class PokeApiDamageClass { public string name { get; set; } = ""; }
public class PokeApiMoveMeta
{
    public PokeApiAilmentRef ailment { get; set; } = new();
    public int ailment_chance { get; set; }
    public int flinch_chance { get; set; }
    public int stat_chance { get; set; }
    public int healing { get; set; }
    public int drain { get; set; }
    public int? min_hits { get; set; }
    public int? max_hits { get; set; }
}
public class PokeApiAilmentRef { public string name { get; set; } = "none"; }
public class PokeApiStatChangeEntry { public int change { get; set; } public PokeApiStatRef stat { get; set; } = new(); }
public class PokeApiStatRef { public string name { get; set; } = ""; }
public class PokeApiTargetRef { public string name { get; set; } = ""; }

public class PokeApiSprites
{
    public string? front_default { get; set; }
    public PokeApiOtherSprites? other { get; set; }
}
public class PokeApiOtherSprites { public PokeApiShowdownSprites? showdown { get; set; } }
public class PokeApiShowdownSprites { public string? front_default { get; set; } public string? back_default { get; set; } }

public class PokeApiSpeciesFull
{
    public List<PokeApiName> names { get; set; } = new();
    public PokeApiEvolutionChainRef? evolution_chain { get; set; }
}
public class PokeApiEvolutionChainRef { public string url { get; set; } = ""; }
public class PokeApiEvolutionChain { public PokeApiChainLink chain { get; set; } = new(); }
public class PokeApiChainLink
{
    public PokeApiSpeciesRef species { get; set; } = new();
    public List<PokeApiChainLink> evolves_to { get; set; } = new();
}
public class PokeApiSpeciesRef { public string name { get; set; } = ""; }

public class PokeApiName { public string name { get; set; } = ""; public PokeApiLanguage language { get; set; } = new(); }
public class PokeApiLanguage { public string name { get; set; } = ""; }
