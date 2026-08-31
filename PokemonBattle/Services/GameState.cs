using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class GameState
{
    private readonly IScoreStore _scoreStore;
    private readonly IPresetStore _presetStore;
    private readonly UnlockService _unlocks;
    private readonly RunStore _runStore;
    private readonly CurrentUserService _currentUser;
    private readonly SkillRatingService _skillRatings;

    public GameScreen CurrentScreen { get; private set; } = GameScreen.Start;
    public int CurrentScore { get; private set; }
    public int CurrentRunLevel => Math.Max(1, CurrentScore + 1);
    public int CurrentRunDifficultyAdjustment { get; private set; }
    public double SkillRating { get; private set; } = SkillRatingCalculator.DefaultRating;
    public double ResultSkillRating { get; private set; } = SkillRatingCalculator.DefaultRating;
    public double LastSkillRatingChange { get; private set; }
    public bool HasPendingSkillRatingUpdate => LastBattleWon && _roundPerformances.Count > 0;
    public int NextRunDifficultyAdjustment =>
        SkillRatingCalculator.CalculateDifficultyAdjustment(ResultSkillRating);
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
    private readonly List<RunRoundPerformance> _roundPerformances = new();

    public event Action? OnChange;

    public GameState(
        IScoreStore scoreStore,
        IPresetStore presetStore,
        UnlockService unlocks,
        RunStore runStore,
        CurrentUserService currentUser,
        SkillRatingService skillRatings)
    {
        _scoreStore = scoreStore;
        _presetStore = presetStore;
        _unlocks = unlocks;
        _runStore = runStore;
        _currentUser = currentUser;
        _skillRatings = skillRatings;
    }

    public IReadOnlyList<LegendaryEncounterHistoryEntry> LegendaryEncounterHistory =>
        _legendaryEncounterHistory;

    private readonly List<LegendaryEncounterHistoryEntry> _legendaryEncounterHistory = new();

    public async Task LoadRunForCurrentUser()
    {
        if (_runLoaded || !_currentUser.IsLoggedIn) return;

        var (
            score,
            highScore,
            loadouts,
            legendaryProgressPercent,
            legendaryEncounterHistory,
            difficultyAdjustment,
            roundPerformances) =
            await _runStore.Load(_currentUser.Username!);
        _legendaryEncounterHistory.Clear();
        _legendaryEncounterHistory.AddRange(legendaryEncounterHistory);
        _roundPerformances.Clear();
        _roundPerformances.AddRange(roundPerformances);
        var storedRating = await _skillRatings.GetOrCreateAsync(_currentUser.Username!);
        SkillRating = storedRating.Rating;
        ResultSkillRating = SkillRating;
        LastSkillRatingChange = 0;
        CurrentRunDifficultyAdjustment = Math.Clamp(
            difficultyAdjustment,
            SkillRatingCalculator.MinimumDifficultyAdjustment,
            SkillRatingCalculator.MaximumDifficultyAdjustment);

        //방어 코드: 도감에 없는 포켓몬(예: 크래시로 깨진 PokemonId=0)이 하나라도 섞여있으면
        //전체 데이터를 신뢰할 수 없다고 보고 진행 상황을 완전히 초기화함
        bool hasCorruptedEntry = loadouts.Any(l => !PokemonDatabase.All.ContainsKey(l.PokemonId));
        bool hadDuplicateItems = false;

        if (hasCorruptedEntry)
        {
            CurrentScore = 0;
            PlayerLoadouts = new List<PokemonLoadout>();
            PlayerTeamIds = new List<int>();
            _roundPerformances.Clear();
        }
        else
        {
            CurrentScore = score;
            hadDuplicateItems = TeamLoadoutRules.HasDuplicateItems(loadouts);
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

        bool isNewRun = CurrentScore == 0
            && PlayerLoadouts.Count == 0
            && _roundPerformances.Count == 0;
        if (hasCorruptedEntry || isNewRun)
        {
            CurrentRunDifficultyAdjustment = SkillRatingCalculator.CalculateDifficultyAdjustment(SkillRating);
            await PersistRun(); //깨끗해진 상태와 현재 런의 고정 보정을 DB에도 즉시 반영
        }
        else if (hadDuplicateItems)
        {
            await PersistRun();
        }

        _runLoaded = true;
    }

    public async Task ReloadRunForCurrentUser()
    {
        _runLoaded = false;
        await LoadRunForCurrentUser();
    }


    private async Task PersistRun()
    {
        if (!_currentUser.IsLoggedIn) return;
        await _runStore.Save(
            _currentUser.Username!,
            CurrentScore,
            HighScore,
            PlayerLoadouts,
            LegendaryProgressPercent,
            _legendaryEncounterHistory,
            CurrentRunDifficultyAdjustment,
            _roundPerformances);
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
            _legendaryEncounterHistory.Add(new LegendaryEncounterHistoryEntry
            {
                CycleNumber = _legendaryEncounterHistory.Count + 1,
                Stage = Math.Max(1, CurrentScore + 1),
                PokemonIds = EnemyTeamIds
                    .Where(EnemyTeamProvider.IsLegendary)
                    .Distinct()
                    .ToList(),
                EncounteredAtUtc = DateTimeOffset.UtcNow
            });
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

    public async Task WinRound(int turns = 0, IEnumerable<Pokemon>? playerTeam = null)
    {
        RecordRoundPerformance(turns, playerTeam, cleared: true);
        ResultSkillRating = SkillRatingCalculator.PreviewRating(
            SkillRating,
            _roundPerformances,
            won: true);
        LastSkillRatingChange = ResultSkillRating - SkillRating;
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

    public async Task LoseBattle(int turns = 0, IEnumerable<Pokemon>? playerTeam = null)
    {
        RecordRoundPerformance(turns, playerTeam, cleared: false);
        await UpdateSkillRatingForCurrentRun();
        _scoreStore.SaveIfHigher(CurrentScore);
        HighScore = Math.Max(HighScore, _scoreStore.GetHighScore());
        LastBattleWon = false;
        LastLegendaryProgressReward = 0;
        CurrentScore = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        _roundPerformances.Clear();
        await PersistRun();
        CurrentScreen = GameScreen.Result;
        NotifyChange();
    }

    public async Task ResetForNewRun()
    {
        //승리한 런을 사용자가 새 런으로 넘길 때도 같은 집계 경로로 평점을 갱신한다.
        if (_roundPerformances.Count > 0)
        {
            await UpdateSkillRatingForCurrentRun(won: true);
        }

        CurrentScore = 0;
        LastLegendaryProgressReward = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        EnemyLoadouts = new List<PokemonLoadout>();
        EnemyTeamIds = new List<int>();
        LegendaryEncounterConsumed = false;
        CurrentRunDifficultyAdjustment =
            SkillRatingCalculator.CalculateDifficultyAdjustment(SkillRating);
        _roundPerformances.Clear();
        await PersistRun();
        CurrentScreen = GameScreen.StarterSelect;
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

    private void RecordRoundPerformance(
        int turns,
        IEnumerable<Pokemon>? playerTeam,
        bool cleared)
    {
        var team = playerTeam?.ToList() ?? new List<Pokemon>();
        double hpRatio = team.Count == 0
            ? (cleared ? 1 : 0)
            : team.Average(pokemon => pokemon.MaxHp <= 0
                ? 0
                : Math.Clamp((double)pokemon.CurrentHp / pokemon.MaxHp, 0, 1));

        _roundPerformances.Add(new RunRoundPerformance
        {
            Cleared = cleared,
            PlayerHpRatio = hpRatio,
            Turns = Math.Max(0, turns)
        });
    }

    private async Task UpdateSkillRatingForCurrentRun(bool won = false)
    {
        if (!_currentUser.IsLoggedIn || _roundPerformances.Count == 0) return;

        var summary = SkillRatingCalculator.SummarizeRun(_roundPerformances, won);
        double previousRating = SkillRating;
        SkillRating = await _skillRatings.UpdateForRunAsync(
            _currentUser.Username!,
            summary);
        ResultSkillRating = SkillRating;
        LastSkillRatingChange = SkillRating - previousRating;
        _roundPerformances.Clear();
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
