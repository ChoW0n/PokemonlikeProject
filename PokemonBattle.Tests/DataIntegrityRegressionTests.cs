using PokemonBattle.Models;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class DataIntegrityRegressionTests
{
    [Fact]
    public void Every_pokemon_move_reference_resolves_to_an_implemented_move()
    {
        var invalidReferences = PokemonDatabase.All
            .SelectMany(pair => pair.Value.MoveNames
                .Concat(pair.Value.MachineOnlyMoveNames)
                .Where(moveKey => !MoveDatabase.All.ContainsKey(moveKey))
                .Select(moveKey => $"#{pair.Key} {pair.Value.Name}: {moveKey}"))
            .ToArray();

        Assert.True(
            invalidReferences.Length == 0,
            $"구현되지 않은 기술 참조: {string.Join(", ", invalidReferences)}");
    }

    [Fact]
    public void Every_pokemon_ability_reference_resolves_to_a_known_ability()
    {
        var invalidReferences = PokemonDatabase.All
            .SelectMany(pair => pair.Value.AbilityNames
                .Where(abilityKey => !AbilityDatabase.All.ContainsKey(abilityKey))
                .Select(abilityKey => $"#{pair.Key} {pair.Value.Name}: {abilityKey}"))
            .ToArray();

        Assert.True(
            invalidReferences.Length == 0,
            $"등록되지 않은 특성 참조: {string.Join(", ", invalidReferences)}");
    }

    [Fact]
    public void Every_implemented_move_is_reachable_from_a_pokemon_or_explicitly_exempted()
    {
        var reachableMoves = PokemonDatabase.All.Values
            .SelectMany(data => data.MoveNames.Concat(data.MachineOnlyMoveNames))
            .ToHashSet(StringComparer.Ordinal);
        var legitimateExceptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "baneful-bunker",
            "obstruct"
        };

        var unreachableMoves = MoveDatabase.All.Keys
            .Where(moveKey => !reachableMoves.Contains(moveKey) && !legitimateExceptions.Contains(moveKey))
            .OrderBy(moveKey => moveKey)
            .ToArray();

        Assert.True(
            unreachableMoves.Length == 0,
            $"포켓몬 목록에서 도달할 수 없는 기술: {string.Join(", ", unreachableMoves)}");
    }

    [Fact]
    public void Move_ability_and_item_descriptions_are_not_blank()
    {
        var blankMoves = MoveDatabase.All
            .Where(pair => string.IsNullOrWhiteSpace(pair.Value.Description))
            .Select(pair => pair.Key)
            .OrderBy(key => key)
            .ToArray();
        var blankAbilities = AbilityDatabase.All
            .Where(pair => string.IsNullOrWhiteSpace(pair.Value.Description))
            .Select(pair => pair.Key)
            .OrderBy(key => key)
            .ToArray();
        var blankItems = ItemDatabase.GeneralItems
            .Concat(ItemDatabase.ExclusiveItems.Values)
            .Where(item => string.IsNullOrWhiteSpace(item.Description))
            .Select(item => item.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.True(blankMoves.Length == 0, $"설명이 비어 있는 기술: {string.Join(", ", blankMoves)}");
        Assert.True(blankAbilities.Length == 0, $"설명이 비어 있는 특성: {string.Join(", ", blankAbilities)}");
        Assert.True(blankItems.Length == 0, $"설명이 비어 있는 도구: {string.Join(", ", blankItems)}");
    }

    [Fact]
    public void Every_species_has_at_least_one_attack_move_except_for_whitelisted_species()
    {
        var legitimateExceptions = new HashSet<int>
        {
            11,  // 단데기: 진화 전 방어형 콘셉트
            14,  // 딱충이: 진화 전 방어형 콘셉트
            129, // 잉어킹: 초기 데이터가 튀어오르기뿐인 콘셉트
            132  // 메타몽: 변신만 가능한 콘셉트
        };

        var speciesWithoutAttack = PokemonDatabase.All
            .Where(pair => !pair.Value.MoveNames
                .Concat(pair.Value.MachineOnlyMoveNames)
                .Any(moveKey => MoveDatabase.All.TryGetValue(moveKey, out var move) && !move.IsStatus))
            .Where(pair => !legitimateExceptions.Contains(pair.Key))
            .Select(pair => $"#{pair.Key} {pair.Value.Name}")
            .OrderBy(name => name)
            .ToArray();

        Assert.True(
            speciesWithoutAttack.Length == 0,
            $"공격기가 없는 종: {string.Join(", ", speciesWithoutAttack)}");
    }

    [Fact]
    public void Learnable_implemented_moves_are_backfilled_into_species_move_lists()
    {
        var missingBackfills = PokemonDatabase.All
            .SelectMany(pair => pair.Value.LearnableMoveNames
                .Intersect(new[]
                {
                    "counter", "mirror-coat", "kings-shield", "spiky-shield", "sticky-web", "switcheroo"
                }, StringComparer.Ordinal)
                .Where(moveKey => !pair.Value.MoveNames.Contains(moveKey, StringComparer.Ordinal))
                .Select(moveKey => $"#{pair.Key} {pair.Value.Name}: {moveKey}"))
            .ToArray();

        Assert.True(
            missingBackfills.Length == 0,
            $"습득 가능 기술이 일반 목록에 없는 종: {string.Join(", ", missingBackfills)}");
        Assert.Contains("counter", PokemonDatabase.All[202].MoveNames);
        Assert.Contains("mirror-coat", PokemonDatabase.All[202].MoveNames);
        Assert.Contains("counter", PokemonDatabase.All[360].MoveNames);
        Assert.Contains("mirror-coat", PokemonDatabase.All[360].MoveNames);
    }

    [Fact]
    public void Removed_silk_guard_is_not_present_in_move_data_or_protection_rules()
    {
        Assert.DoesNotContain("silk-guard", MoveDatabase.All.Keys);
        Assert.False(MoveRuleMetadata.IsProtectionMove("silk-guard"));
    }
}