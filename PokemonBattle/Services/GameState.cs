using PokemonBattle.Models;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public class GameState
{
    private readonly IScoreStore _scoreStore;
    private readonly IPresetStore _presetStore;
    private readonly UnlockService _unlocks;
    private readonly RunStore _runStore;
    private readonly CurrentUserService _currentUser;
    private readonly SkillRatingService _skillRatings;
    private readonly PlayerProgressionStore? _progression;
    private readonly PokemonMasteryStore? _mastery;
    private readonly SemaphoreSlim _persistGate = new(1, 1);
    private readonly SemaphoreSlim _outcomeGate = new(1, 1);

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
    public int CompletedBattles { get; private set; }
    public bool RivalPending { get; private set; }
    public bool IsRivalBattle { get; private set; }
    public string? RivalUsername { get; private set; }
    public List<MailboxMessage> MailboxMessages { get; private set; } = new();
    public List<TechnicalMachineInventory> TechnicalMachines { get; private set; } = new();
    public IReadOnlyDictionary<int, int> PokemonMasteryWins => _masteryWins;

    public int SelectedPokemonId { get; private set; } = 1;
    public int EnemyPokemonId { get; private set; } = 4;
    public bool LastBattleWon { get; private set; }
    public List<string> EvolutionMessages { get; private set; } = new();
    public string CovenantRewardMessage { get; private set; } = "";

    public List<int> PlayerTeamIds { get; private set; } = new();
    public List<int> EnemyTeamIds { get; private set; } = new();
    public List<PokemonLoadout> PlayerLoadouts { get; private set; } = new();
    public List<PokemonLoadout> EnemyLoadouts { get; private set; } = new(); //상대 팀의 확정된 기술/특성/도구 (미리보기와 전투가 항상 일치하도록)
    public RunMetaState RunMeta { get; private set; } = new();
    public BattlefieldImprintDefinition? CurrentBattlefieldImprint =>
        RunMetaCatalog.Battlefield(RunMeta.BattlefieldImprintId);
    public IReadOnlyList<RunLegacyDefinition> ActiveLegacies =>
        RunMeta.LegacyIds
            .Select(RunMetaCatalog.Legacy)
            .Where(legacy => legacy != null)
            .Cast<RunLegacyDefinition>()
            .ToList();
    public IReadOnlyList<string> PendingLegacyChoices => RunMeta.PendingLegacyChoices;
    public int LegacyClaimsRemaining => RunMeta.LegacyClaimsRemaining;
    public IReadOnlyList<StolenMoveOption> PendingStolenMoveChoices =>
        RunMeta.PendingStolenMoveChoices;
    public bool CanChooseRiskCovenant =>
        RunMeta.RiskCovenantStage == CurrentRunLevel
        && !RunMeta.RiskCovenantDecisionMade;
    public RiskCovenantDefinition? CurrentRiskCovenant =>
        RunMetaCatalog.Covenant(RunMeta.RiskCovenantId)
        ?? RunMetaCatalog.RiskCovenants.FirstOrDefault();

    private bool _runLoaded;
    private readonly object _loadSync = new();
    private Task? _loadTask;
    private bool _battleOutcomeProcessed;
    private readonly List<RunRoundPerformance> _roundPerformances = new();
    private readonly List<StolenMoveOption> _battleUsedEnemyMoves = new();
    private readonly Dictionary<int, int> _masteryWins = new();
    private readonly Random _metaRandom = new();

    private static bool EnsureLoadoutGenders(IEnumerable<PokemonLoadout> loadouts)
    {
        bool changed = false;
        foreach (var loadout in loadouts)
        {
            if (loadout.Gender.HasValue
                || !PokemonDatabase.All.TryGetValue(loadout.PokemonId, out var data))
            {
                continue;
            }

            // 성별이 없는 기존 저장 데이터는 처음 읽을 때 한 번만 정한다.
            loadout.Gender = Pokemon.InferGender(data);
            changed = true;
        }

        return changed;
    }

    public event Action? OnChange;

    public GameState(
        IScoreStore scoreStore,
        IPresetStore presetStore,
        UnlockService unlocks,
        RunStore runStore,
        CurrentUserService currentUser,
        SkillRatingService skillRatings,
        PlayerProgressionStore? progression = null,
        PokemonMasteryStore? mastery = null)
    {
        _scoreStore = scoreStore;
        _presetStore = presetStore;
        _unlocks = unlocks;
        _runStore = runStore;
        _currentUser = currentUser;
        _skillRatings = skillRatings;
        _progression = progression;
        _mastery = mastery;
    }

    public IReadOnlyList<LegendaryEncounterHistoryEntry> LegendaryEncounterHistory =>
        _legendaryEncounterHistory;

    private readonly List<LegendaryEncounterHistoryEntry> _legendaryEncounterHistory = new();

    public Task LoadRunForCurrentUser()
    {
        if (_runLoaded || !_currentUser.IsLoggedIn) return Task.CompletedTask;

        lock (_loadSync)
        {
            if (_runLoaded || !_currentUser.IsLoggedIn) return Task.CompletedTask;
            return _loadTask ??= LoadRunForCurrentUserCore();
        }
    }

    private async Task LoadRunForCurrentUserCore()
    {
        try
        {
            var (
                score,
                highScore,
                loadouts,
                legendaryProgressPercent,
                legendaryEncounterHistory,
                difficultyAdjustment,
                roundPerformances,
                metaState) =
                await _runStore.Load(_currentUser.Username!);
        RunMeta = RunMetaCatalog.Normalize(metaState);
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

        if (_progression != null)
        {
            try
            {
                // 로그인 전에 기술머신 재지급을 시도한다.
                await _progression.RunTechnicalMachineRegrantOnceAsync(_currentUser.Username!);
            }
            catch
            {
                // 재지급 실패가 로그인을 막지 않게 한다.
            }

            var accountProgress = await _progression.LoadAsync(_currentUser.Username!);
            CompletedBattles = accountProgress.completedBattles;
            RivalPending = accountProgress.rivalPending;
            MailboxMessages = accountProgress.messages;
            TechnicalMachines = accountProgress.machines;
        }
        _masteryWins.Clear();
        if (_mastery != null)
        {
            foreach (var entry in await _mastery.LoadAsync(_currentUser.Username!))
            {
                _masteryWins[entry.Key] = entry.Value;
            }
        }

        //방어 코드: 도감에 없는 포켓몬(예: 크래시로 깨진 PokemonId=0)이 하나라도 섞여있으면
        //전체 데이터를 신뢰할 수 없다고 보고 진행 상황을 완전히 초기화함
        bool hasCorruptedEntry = loadouts.Any(l => !PokemonDatabase.All.ContainsKey(l.PokemonId));
        bool hadDuplicateItems = false;
        bool hadMissingGenders = false;

        if (hasCorruptedEntry)
        {
            CurrentScore = 0;
            PlayerLoadouts = new List<PokemonLoadout>();
            PlayerTeamIds = new List<int>();
            _roundPerformances.Clear();
            RunMeta = new RunMetaState();
        }
        else
        {
            CurrentScore = score;
            hadDuplicateItems = TeamLoadoutRules.HasDuplicateItems(loadouts);
            PlayerLoadouts = TeamLoadoutRules.NormalizeUniqueItems(loadouts);
            hadMissingGenders = EnsureLoadoutGenders(PlayerLoadouts);
            PlayerTeamIds = PlayerLoadouts.Select(l => l.PokemonId).ToList();
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
        else if (hadDuplicateItems || hadMissingGenders)
        {
            await PersistRun();
        }

            _runLoaded = true;
        }
        finally
        {
            lock (_loadSync)
            {
                _loadTask = null;
            }
        }
    }

    public async Task ReloadRunForCurrentUser()
    {
        lock (_loadSync)
        {
            _runLoaded = false;
        }
        await LoadRunForCurrentUser();
    }


    private async Task PersistRun()
    {
        if (!_currentUser.IsLoggedIn) return;

        await _persistGate.WaitAsync();
        try
        {
            // Snapshot before the database await so a later UI event cannot
            // mutate the JSON payload halfway through a save.
            var loadouts = PlayerLoadouts.Select(loadout => loadout.Clone()).ToList();
            var history = _legendaryEncounterHistory.ToList();
            var performances = _roundPerformances.ToList();
            var metaState = RunMeta.Clone();
            await _runStore.Save(
                _currentUser.Username!,
                CurrentScore,
                HighScore,
                loadouts,
                LegendaryProgressPercent,
                history,
                CurrentRunDifficultyAdjustment,
                performances,
                metaState);
        }
        finally
        {
            _persistGate.Release();
        }
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
        EnsureLoadoutGenders(normalizedLoadouts);
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
        EnsureLoadoutGenders(PlayerLoadouts);
        PlayerTeamIds = PlayerLoadouts.Select(l => l.PokemonId).ToList();
        await PersistRun();
        if (_progression != null && _currentUser.IsLoggedIn)
        {
            await _progression.SaveLatestLoadoutsAsync(_currentUser.Username!, PlayerLoadouts);
            await _progression.RecordTeamSelectionsAsync(_currentUser.Username!, PlayerLoadouts);
        }
        NotifyChange();
    }

    public async Task<List<PokemonLoadout>?> GetPendingRivalLoadoutsAsync()
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return null;
        var rival = await _progression.GetPendingRivalAsync(_currentUser.Username!);
        if (rival == null) return null;
        IsRivalBattle = true;
        RivalPending = true;
        RivalUsername = rival.Username;
        return rival.Loadouts;
    }

    public void MarkNormalBattle()
    {
        IsRivalBattle = false;
        RivalPending = false;
        RivalUsername = null;
    }

    public void BeginBattle(bool? rival = null)
    {
        if (rival.HasValue) IsRivalBattle = rival.Value;
        PlayerLoadouts = TeamLoadoutRules.NormalizeUniqueItems(PlayerLoadouts);
        PlayerTeamIds = PlayerLoadouts.Select(loadout => loadout.PokemonId).ToList();
        EnemyLoadouts = TeamLoadoutRules.NormalizeUniqueItems(EnemyLoadouts);
        EnemyTeamIds = EnemyLoadouts.Select(loadout => loadout.PokemonId).ToList();
        _battleOutcomeProcessed = false;
        _battleUsedEnemyMoves.Clear();
        CovenantRewardMessage = "";
    }

    public async Task EnsureStageMetaAsync()
    {
        int stage = CurrentRunLevel;
        bool changed = false;

        if (RunMeta.BattlefieldImprintStage != stage
            || RunMetaCatalog.Battlefield(RunMeta.BattlefieldImprintId) == null)
        {
            var selected = RunMetaCatalog.BattlefieldImprints
                .OrderBy(_ => _metaRandom.Next())
                .First();
            RunMeta.BattlefieldImprintId = selected.Id;
            RunMeta.BattlefieldImprintStage = stage;
            changed = true;
        }

        if (RunMeta.RiskCovenantStage != stage)
        {
            string? previousCovenantId = RunMeta.RiskCovenantId;
            var selectedCovenant = RunMetaCatalog.RiskCovenants
                .Where(covenant => covenant.Id != previousCovenantId)
                .OrderBy(_ => _metaRandom.Next())
                .FirstOrDefault();
            RunMeta.RiskCovenantStage = stage;
            RunMeta.RiskCovenantId = selectedCovenant?.Id;
            RunMeta.RiskCovenantDecisionMade = false;
            RunMeta.RiskCovenantAccepted = false;
            RunMeta.BonusLegacyClaims = 0;
            RunMeta.BonusTechnicalMachineRewards = 0;
            changed = true;
        }

        if (changed)
        {
            await PersistRun();
            NotifyChange();
        }
    }

    public async Task<bool> ChooseRiskCovenantAsync(bool accept)
    {
        var covenant = CurrentRiskCovenant;
        if (!CanChooseRiskCovenant || covenant == null) return false;

        RunMeta.RiskCovenantDecisionMade = true;
        RunMeta.RiskCovenantAccepted = accept;
        RunMeta.RiskCovenantId = covenant.Id;
        RunMeta.BonusLegacyClaims = accept ? covenant.BonusLegacyClaims : 0;
        RunMeta.BonusTechnicalMachineRewards =
            accept ? covenant.BonusTechnicalMachineRewards : 0;

        if (accept)
        {
            foreach (var loadout in EnemyLoadouts)
            {
                loadout.Level += covenant.EnemyLevelBonus;
            }

            RunMeta.MaxHpPenaltyPercent = Math.Max(
                RunMeta.MaxHpPenaltyPercent,
                covenant.MaxHpPenaltyPercent);

            if (covenant.GrantsImmediateLegacy)
            {
                var grantedLegacy = RunMetaCatalog.Legacies
                    .Where(legacy => !RunMeta.LegacyIds.Contains(legacy.Id))
                    .OrderBy(_ => _metaRandom.Next())
                    .FirstOrDefault();
                if (grantedLegacy != null)
                {
                    RunMeta.LegacyIds.Add(grantedLegacy.Id);
                    CovenantRewardMessage =
                        $"폭주의 저주 보상으로 {grantedLegacy.Name} 유산을 즉시 획득했습니다.";
                }
            }
        }

        await PersistRun();
        NotifyChange();
        return true;
    }

    public void RecordEnemyMoveUsed(int sourcePokemonId, string moveKey)
    {
        if (!RunMetaCatalog.IsStolenMoveEligible(moveKey, sourcePokemonId)
            || _battleUsedEnemyMoves.Any(option =>
                option.SourcePokemonId == sourcePokemonId && option.MoveKey == moveKey))
        {
            return;
        }

        _battleUsedEnemyMoves.Add(new StolenMoveOption
        {
            SourcePokemonId = sourcePokemonId,
            MoveKey = moveKey
        });
    }

    public IReadOnlyList<string> RunStolenMoveKeysFor(int pokemonId) =>
        RunMeta.StolenMoves
            .Where(move => move.PokemonId == pokemonId)
            .Select(move => move.MoveKey)
            .ToList();

    public bool IsRunStolenMove(int pokemonId, string moveKey) =>
        RunMeta.StolenMoves.Any(move =>
            move.PokemonId == pokemonId && move.MoveKey == moveKey);

    public async Task<bool> ClaimLegacyAsync(string legacyId)
    {
        if (RunMeta.LegacyClaimsRemaining <= 0
            || !RunMeta.PendingLegacyChoices.Contains(legacyId)
            || RunMeta.LegacyIds.Contains(legacyId))
        {
            return false;
        }

        RunMeta.LegacyIds.Add(legacyId);
        RunMeta.PendingLegacyChoices.Remove(legacyId);
        RunMeta.LegacyClaimsRemaining--;
        await PersistRun();
        NotifyChange();
        return true;
    }

    public async Task<bool> ClaimStolenMoveAsync(
        int pokemonId,
        string moveKey,
        string? replaceMoveKey = null)
    {
        var option = RunMeta.PendingStolenMoveChoices.FirstOrDefault(candidate =>
            candidate.MoveKey == moveKey);
        var loadout = PlayerLoadouts.FirstOrDefault(candidate =>
            candidate.PokemonId == pokemonId);
        if (option == null || loadout == null
            || !RunMetaCatalog.IsStolenMoveEligible(moveKey, option.SourcePokemonId)
            || !RunMetaCatalog.TryApplyStolenMove(loadout, moveKey, replaceMoveKey))
        {
            return false;
        }

        RunMeta.StolenMoves.Add(new StolenMoveRecord
        {
            PokemonId = pokemonId,
            MoveKey = moveKey
        });
        RunMeta.PendingStolenMoveChoices.Remove(option);
        await PersistRun();
        NotifyChange();
        return true;
    }

    public async Task ClearPendingRunRewardsAsync()
    {
        if (RunMeta.PendingLegacyChoices.Count == 0
            && RunMeta.PendingStolenMoveChoices.Count == 0
            && RunMeta.LegacyClaimsRemaining == 0)
        {
            return;
        }

        RunMeta.PendingLegacyChoices.Clear();
        RunMeta.PendingStolenMoveChoices.Clear();
        RunMeta.LegacyClaimsRemaining = 0;
        await PersistRun();
        NotifyChange();
    }

    public async Task RecordMoveSelectionAsync(string moveKey)
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return;
        await _progression.RecordMoveSelectionAsync(_currentUser.Username!, moveKey);
    }

    public IReadOnlyList<string> TechnicalMachineMovesFor(int pokemonId)
    {
        if (!PokemonDatabase.All.TryGetValue(pokemonId, out var data)) return Array.Empty<string>();
        var compatible = data.MoveNames
            .Concat(data.MachineOnlyMoveNames)
            .ToHashSet(StringComparer.Ordinal);
        return TechnicalMachines
            .Where(machine => machine.Quantity > 0 && MoveDatabase.All.ContainsKey(machine.MoveKey))
            .Where(machine => compatible.Contains(machine.MoveKey))
            .Select(machine => machine.MoveKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<bool> TryLearnTechnicalMachineAsync(string moveKey)
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return false;
        bool consumed = await _progression.TryConsumeTechnicalMachineAsync(
            _currentUser.Username!, moveKey);
        if (consumed)
        {
            var machine = TechnicalMachines.FirstOrDefault(item => item.MoveKey == moveKey);
            if (machine != null) machine.Quantity--;
            NotifyChange();
        }
        return consumed;
    }

    public async Task MarkMailboxMessageReadAsync(int messageId)
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return;
        if (await _progression.MarkMessageReadAsync(_currentUser.Username!, messageId))
        {
            var message = MailboxMessages.FirstOrDefault(item => item.Id == messageId);
            if (message != null) message.IsRead = true;
            NotifyChange();
        }
    }

    public async Task MarkAllMailboxMessagesReadAsync()
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return;
        int markedCount = await _progression.MarkAllMessagesReadAsync(_currentUser.Username!);
        if (markedCount == 0) return;

        foreach (var message in MailboxMessages)
        {
            message.IsRead = true;
        }
        NotifyChange();
    }

    public int UnreadMailboxCount => MailboxMessages.Count(message => !message.IsRead);

    private async Task RefreshAccountProgress()
    {
        if (_progression == null || !_currentUser.IsLoggedIn) return;
        var accountProgress = await _progression.LoadAsync(_currentUser.Username!);
        CompletedBattles = accountProgress.completedBattles;
        RivalPending = accountProgress.rivalPending;
        MailboxMessages = accountProgress.messages;
        TechnicalMachines = accountProgress.machines;
    }

    public async Task WinRound(
        int turns = 0,
        IEnumerable<Pokemon>? playerTeam = null,
        IEnumerable<Pokemon>? enemyTeam = null)
    {
        await _outcomeGate.WaitAsync();
        try
        {
            await WinRoundCore(turns, playerTeam, enemyTeam);
        }
        finally
        {
            _outcomeGate.Release();
        }
    }

    private async Task WinRoundCore(
        int turns,
        IEnumerable<Pokemon>? playerTeam,
        IEnumerable<Pokemon>? enemyTeam)
    {
        if (_battleOutcomeProcessed) return;
        _battleOutcomeProcessed = true;
        int battleRound = CurrentRunLevel;
        int battleDifficultyAdjustment = CurrentRunDifficultyAdjustment;
        double battleSkillRating = SkillRating;
        bool isLegendaryBattle = EnemyTeamProvider.ContainsLegendary(EnemyTeamIds);
        double playerHpRatio = RecordRoundPerformance(turns, playerTeam, cleared: true);
        double enemyHpRatio = CalculateTeamHpRatio(enemyTeam);
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
        HighScore = Math.Max(HighScore, CurrentScore);
        _scoreStore.SaveIfHigher(CurrentScore);
        LastBattleWon = true;
        EvolutionMessages = new List<string>();
        PrepareVictoryRewards();
        if (RunMeta.BonusTechnicalMachineRewards > 0)
        {
            if (_progression != null && _currentUser.IsLoggedIn)
            {
                var rewards = new List<string>();
                for (int i = 0; i < RunMeta.BonusTechnicalMachineRewards; i++)
                {
                    string? rewardName = await _progression.GrantTechnicalMachineRewardAsync(
                        _currentUser.Username!,
                        PlayerLoadouts);
                    if (rewardName != null) rewards.Add(rewardName);
                }

                if (rewards.Count > 0)
                {
                    CovenantRewardMessage =
                        $"어둠의 서약 보상: {string.Join(", ", rewards)} 기술머신을 획득했습니다.";
                }
            }
            RunMeta.BonusTechnicalMachineRewards = 0;
        }

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
        if (_progression != null && _currentUser.IsLoggedIn)
        {
            await _progression.CompleteBattleAsync(
                _currentUser.Username!,
                PlayerLoadouts,
                IsRivalBattle,
                won: true,
                round: battleRound,
                turns: turns,
                playerHpRatio: playerHpRatio,
                enemyHpRatio: enemyHpRatio,
                isLegendaryBattle: isLegendaryBattle,
                difficultyAdjustment: battleDifficultyAdjustment,
                skillRating: battleSkillRating);
            await RefreshAccountProgress();
        }
        if (_mastery != null && _currentUser.IsLoggedIn)
        {
            var masteryIds = PlayerLoadouts.Select(loadout => loadout.PokemonId).ToList();
            await _mastery.RecordVictoryContributionsAsync(
                _currentUser.Username!, masteryIds);
            PokemonMasteryStore.ApplyVictoryContributions(_masteryWins, masteryIds);
        }
        CurrentScreen = GameScreen.Result;
        NotifyChange();
    }

    public int GetPokemonMasteryBonusPercent(int pokemonId) =>
        PokemonMasteryRules.GetBonusPercent(_masteryWins.GetValueOrDefault(pokemonId));

    public async Task LoseBattle(
        int turns = 0,
        IEnumerable<Pokemon>? playerTeam = null,
        IEnumerable<Pokemon>? enemyTeam = null)
    {
        await _outcomeGate.WaitAsync();
        try
        {
            await LoseBattleCore(turns, playerTeam, enemyTeam);
        }
        finally
        {
            _outcomeGate.Release();
        }
    }

    private async Task LoseBattleCore(
        int turns,
        IEnumerable<Pokemon>? playerTeam,
        IEnumerable<Pokemon>? enemyTeam)
    {
        if (_battleOutcomeProcessed) return;
        _battleOutcomeProcessed = true;
        int battleRound = CurrentRunLevel;
        int battleDifficultyAdjustment = CurrentRunDifficultyAdjustment;
        double battleSkillRating = SkillRating;
        bool isLegendaryBattle = EnemyTeamProvider.ContainsLegendary(EnemyTeamIds);
        var latestLoadouts = PlayerLoadouts.Select(loadout => loadout.Clone()).ToList();
        double playerHpRatio = RecordRoundPerformance(turns, playerTeam, cleared: false);
        double enemyHpRatio = CalculateTeamHpRatio(enemyTeam);
        await UpdateSkillRatingForCurrentRun(peakRound: CurrentRunLevel);
        _scoreStore.SaveIfHigher(CurrentScore);
        HighScore = Math.Max(HighScore, _scoreStore.GetHighScore());
        LastBattleWon = false;
        LastLegendaryProgressReward = 0;
        CurrentScore = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        RunMeta = new RunMetaState();
        _battleUsedEnemyMoves.Clear();
        if (_progression != null && _currentUser.IsLoggedIn)
        {
            await _progression.CompleteBattleAsync(
                _currentUser.Username!,
                latestLoadouts,
                IsRivalBattle,
                won: false,
                round: battleRound,
                turns: turns,
                playerHpRatio: playerHpRatio,
                enemyHpRatio: enemyHpRatio,
                isLegendaryBattle: isLegendaryBattle,
                difficultyAdjustment: battleDifficultyAdjustment,
                skillRating: battleSkillRating);
            await RefreshAccountProgress();
        }
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
            await UpdateSkillRatingForCurrentRun(
                won: true,
                peakRound: Math.Max(1, CurrentScore));
        }

        CurrentScore = 0;
        LastLegendaryProgressReward = 0;
        PlayerLoadouts = new List<PokemonLoadout>();
        PlayerTeamIds = new List<int>();
        EnemyLoadouts = new List<PokemonLoadout>();
        EnemyTeamIds = new List<int>();
        IsRivalBattle = false;
        RivalUsername = null;
        RunMeta = new RunMetaState();
        _battleUsedEnemyMoves.Clear();
        _battleOutcomeProcessed = false;
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

    private double RecordRoundPerformance(
        int turns,
        IEnumerable<Pokemon>? playerTeam,
        bool cleared)
    {
        var team = playerTeam?.ToList() ?? new List<Pokemon>();
        double hpRatio = team.Count == 0
            ? (cleared ? 1 : 0)
            : CalculateTeamHpRatio(team);

        _roundPerformances.Add(new RunRoundPerformance
        {
            Cleared = cleared,
            PlayerHpRatio = hpRatio,
            Turns = Math.Max(0, turns)
        });
        return hpRatio;
    }

    private static double CalculateTeamHpRatio(IEnumerable<Pokemon>? team)
    {
        var members = team?.ToList() ?? new List<Pokemon>();
        return members.Count == 0
            ? 0
            : members.Average(pokemon => pokemon.MaxHp <= 0
                ? 0
                : Math.Clamp((double)pokemon.CurrentHp / pokemon.MaxHp, 0, 1));
    }

    private async Task UpdateSkillRatingForCurrentRun(
        bool won = false,
        int? peakRound = null)
    {
        if (!_currentUser.IsLoggedIn || _roundPerformances.Count == 0) return;

        var summary = SkillRatingCalculator.SummarizeRun(_roundPerformances, won);
        double previousRating = SkillRating;
        SkillRating = await _skillRatings.UpdateForRunAsync(
            _currentUser.Username!,
            summary,
            peakRound ?? CurrentRunLevel);
        ResultSkillRating = SkillRating;
        LastSkillRatingChange = SkillRating - previousRating;
        _roundPerformances.Clear();
    }

    private void PrepareVictoryRewards()
    {
        var availableLegacies = RunMetaCatalog.Legacies
            .Where(legacy => !RunMeta.LegacyIds.Contains(legacy.Id))
            .OrderBy(_ => _metaRandom.Next())
            .Take(3)
            .Select(legacy => legacy.Id)
            .ToList();
        int scheduledClaims = RunMetaCatalog.ScheduledLegacyClaimsForWin(CurrentScore);
        RunMeta.LegacyClaimsRemaining = Math.Min(
            scheduledClaims + RunMeta.BonusLegacyClaims,
            availableLegacies.Count);
        RunMeta.PendingLegacyChoices = RunMeta.LegacyClaimsRemaining > 0
            ? availableLegacies
            : new List<string>();
        RunMeta.BonusLegacyClaims = 0;

        RunMeta.PendingStolenMoveChoices = _battleUsedEnemyMoves
            .Where(option => !RunMeta.StolenMoves.Any(stolen =>
                stolen.MoveKey == option.MoveKey))
            .OrderBy(_ => _metaRandom.Next())
            .Take(6)
            .Select(option => option.Clone())
            .ToList();
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
