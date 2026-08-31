using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class GameState
{
    private readonly IScoreStore _scoreStore;
    private readonly IPresetStore _presetStore;
    private readonly UnlockService _unlocks;
    private readonly RunStore _runStore;
    private readonly CurrentUserService _currentUser;

    public GameScreen CurrentScreen { get; private set; } = GameScreen.Start;
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int LegendaryProgressPercent { get; private set; }
    public int LastLegendaryProgressReward { get; private set; }
    public bool LegendaryUnlocked => LegendaryProgression.IsUnlocked(LegendaryProgressPercent);
    public bool LegendaryEncounterConsumed { get; private set; }

    public int SelectedPokemonId { get; private set; } = 1;
    public int EnemyPokemonId { get; private set; } = 4;
    public bool LastBattleWon { get; private set; }
    public List<string> EvolutionMessages { get; private set; } = new();

    public List<int> PlayerTeamIds { get; private set; } = new();
    public List<int> EnemyTeamIds { get; private set; } = new();
    public List<PokemonLoadout> PlayerLoadouts { get; private set; } = new();
    public List<PokemonLoadout> EnemyLoadouts { get; private set; } = new(); //상대 팀의 확정된 기술/특성/도구 (미리보기와 전투가 항상 일치하도록)

    private bool _runLoaded;

    public event Action? OnChange;

    public GameState(IScoreStore scoreStore, IPresetStore presetStore, UnlockService unlocks, RunStore runStore, CurrentUserService currentUser)
    {
        _scoreStore = scoreStore;
        _presetStore = presetStore;
        _unlocks = unlocks;
        _runStore = runStore;
        _currentUser = currentUser;
    }

    public async Task LoadRunForCurrentUser()
    {
        if (_runLoaded || !_currentUser.IsLoggedIn) return;

        var (score, highScore, loadouts, legendaryProgressPercent) = await _runStore.Load(_currentUser.Username!);

        //방어 코드: 도감에 없는 포켓몬(예: 크래시로 깨진 PokemonId=0)이 하나라도 섞여있으면
        //전체 데이터를 신뢰할 수 없다고 보고 진행 상황을 완전히 초기화함
        bool hasCorruptedEntry = loadouts.Any(l => !PokemonDatabase.All.ContainsKey(l.PokemonId));

        if (hasCorruptedEntry)
        {
            CurrentScore = 0;
            PlayerLoadouts = new List<PokemonLoadout>();
            PlayerTeamIds = new List<int>();
            await PersistRun(); //깨끗해진 상태를 DB에도 즉시 반영해서 다음부턴 안 재발하게 함
        }
        else
        {
            CurrentScore = score;
            bool hadDuplicateItems = TeamLoadoutRules.HasDuplicateItems(loadouts);
            PlayerLoadouts = TeamLoadoutRules.NormalizeUniqueItems(loadouts);
            PlayerTeamIds = PlayerLoadouts.Select(l => l.PokemonId).ToList();
            if (hadDuplicateItems)
            {
                await PersistRun();
            }
        }

        HighScore = highScore;
        LegendaryProgressPercent = Math.Clamp(legendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent);
        LastLegendaryProgressReward = 0;
        LegendaryEncounterConsumed = false;
        _runLoaded = true;
    }


    private async Task PersistRun()
    {
        if (!_currentUser.IsLoggedIn) return;
        await _runStore.Save(_currentUser.Username!, CurrentScore, HighScore, PlayerLoadouts, LegendaryProgressPercent);
    }

    public void GoTo(GameScreen screen)
    {
        CurrentScreen = screen;
        NotifyChange();
    }

    public void SelectPokemon(int pokemonId)
    {
        SelectedPokemonId = pokemonId;
        NotifyChange();
    }

    public void SetEnemy(int pokemonId)
    {
        EnemyPokemonId = pokemonId;
        NotifyChange();
    }

    public void SetPlayerTeam(List<int> ids)
    {
        PlayerTeamIds = ids;
        NotifyChange();
    }

    public void SetEnemyTeam(List<int> ids)
    {
        EnemyTeamIds = ids;
        NotifyChange();
    }

    public async Task SetEnemyLoadouts(List<PokemonLoadout> loadouts) //상대 미리보기에서 확정된 라인업 저장 및 전설 출현 소비
    {
        var normalizedLoadouts = TeamLoadoutRules.NormalizeUniqueItems(loadouts);
        if (!HaveSameLoadouts(EnemyLoadouts, normalizedLoadouts))
        {
            LegendaryEncounterConsumed = false;
        }

        EnemyLoadouts = normalizedLoadouts;
        EnemyTeamIds = EnemyLoadouts.Select(l => l.PokemonId).ToList();

        var consumption = LegendaryProgression.ConsumeEncounter(
            LegendaryProgressPercent,
            EnemyTeamProvider.ContainsLegendary(EnemyTeamIds),
            LegendaryEncounterConsumed);

        if (consumption.WasConsumed)
        {
            LegendaryProgressPercent = consumption.ProgressPercent;
            LegendaryEncounterConsumed = true;
            await PersistRun();
        }

        NotifyChange();
    }

    public async Task SetPlayerLoadouts(List<PokemonLoadout> loadouts)
    {
        PlayerLoadouts = TeamLoadoutRules.NormalizeUniqueItems(loadouts);
        PlayerTeamIds = PlayerLoadouts.Select(l => l.PokemonId).ToList();
        await PersistRun();
        NotifyChange();
    }

    public async Task WinRound()
    {
        int progressBefore = LegendaryProgressPercent;
        int progressReward = LegendaryEncounterConsumed
            ? 0
            : LegendaryProgression.CalculateReward(CurrentScore + 1, EnemyLoadouts);
        LegendaryProgressPercent = LegendaryProgression.AddProgress(progressBefore, progressReward);
        LastLegendaryProgressReward = LegendaryProgressPercent - progressBefore;
        CurrentScore++;
        LastBattleWon = true;
        EvolutionMessages = new List<string>();

        foreach (var loadout in PlayerLoadouts)
        {
            loadout.Level++;

            var data = PokemonDatabase.All[loadout.PokemonId];
            if (data.EvolvesToId != null && loadout.Level >= data.EvolveLevel)
            {
                await _unlocks.Unlock(data.EvolvesToId.Value);
                EvolutionMessages.Add(PokemonDatabase.All[data.EvolvesToId.Value].Name);
            }
        }

        await PersistRun();
        CurrentScreen = GameScreen.Result;
        NotifyChange();
    }

    public async Task LoseBattle()
    {
        _scoreStore.SaveIfHigher(CurrentScore);
        HighScore = Math.Max(HighScore, _scoreStore.GetHighScore());
        LastBattleWon = false;
        LastLegendaryProgressReward = 0;
        CurrentScore = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        await PersistRun();
        CurrentScreen = GameScreen.Result;
        NotifyChange();
    }

    public async Task ResetForNewRun()
    {
        CurrentScore = 0;
        LastLegendaryProgressReward = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        EnemyLoadouts = new List<PokemonLoadout>();
        EnemyTeamIds = new List<int>();
        LegendaryEncounterConsumed = false;
        await PersistRun();
        CurrentScreen = GameScreen.Start;
        NotifyChange();
    }

    public async Task<bool> SavePreset(string name, List<PokemonLoadout> team)
    {
        if (!_currentUser.IsLoggedIn || string.IsNullOrWhiteSpace(name)) return false;

        await _presetStore.SaveAsync(name, TeamLoadoutRules.NormalizeUniqueItems(team));
        return true;
    }

    public async Task<List<PokemonLoadout>?> LoadPreset(
        string name,
        IEnumerable<PokemonLoadout>? currentRun = null)
    {
        if (!_currentUser.IsLoggedIn || string.IsNullOrWhiteSpace(name)) return null;

        var preset = await _presetStore.LoadAsync(name);
        return preset == null
            ? null
            : PresetLoadoutMapper.ApplyCurrentRunLevels(
                TeamLoadoutRules.NormalizeUniqueItems(preset),
                currentRun ?? Enumerable.Empty<PokemonLoadout>());
    }

    public Task<List<string>> ListPresetNames() =>
        _currentUser.IsLoggedIn
            ? _presetStore.ListNamesAsync()
            : Task.FromResult(new List<string>());

    public async Task<bool> DeletePreset(string name)
    {
        if (!_currentUser.IsLoggedIn || string.IsNullOrWhiteSpace(name)) return false;
        return await _presetStore.DeleteAsync(name);
    }

    private void NotifyChange()
    {
        OnChange?.Invoke();
    }

    private static bool HaveSameLoadouts(
        IReadOnlyList<PokemonLoadout> first,
        IReadOnlyList<PokemonLoadout> second)
    {
        if (first.Count != second.Count) return false;

        for (int i = 0; i < first.Count; i++)
        {
            var left = first[i];
            var right = second[i];
            if (left.PokemonId != right.PokemonId
                || left.ChosenAbility != right.ChosenAbility
                || left.ChosenItem != right.ChosenItem
                || left.Level != right.Level
                || !left.ChosenMoveNames.SequenceEqual(right.ChosenMoveNames))
            {
                return false;
            }
        }

        return true;
    }
}
