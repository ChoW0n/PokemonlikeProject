using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class PlayerProgressionStore
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    public PlayerProgressionStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(int completedBattles, bool rivalPending, List<MailboxMessage> messages,
        List<TechnicalMachineInventory> machines)> LoadAsync(string username)
    {
        var profile = await GetOrCreateAsync(username);
        var messages = await _db.MailboxMessages
            .AsNoTracking()
            .Where(message => message.Username == username)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ToListAsync();
        var machines = await _db.TechnicalMachines
            .AsNoTracking()
            .Where(machine => machine.Username == username && machine.Quantity > 0)
            .OrderBy(machine => machine.MoveKey)
            .ToListAsync();
        return (profile.CompletedBattles, profile.RivalPending, messages, machines);
    }

    public async Task SaveLatestLoadoutsAsync(string username, IEnumerable<PokemonLoadout> loadouts)
    {
        var profile = await GetOrCreateAsync(username);
        profile.LatestLoadoutsJson = SerializeLoadouts(loadouts);
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RecordMoveSelectionAsync(string username, string moveKey)
    {
        var profile = await GetOrCreateAsync(username);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        MovePreferenceRules.Record(preferences, moveKey);
        profile.MovePreferencesJson = JsonSerializer.Serialize(preferences, JsonOptions);
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RecordTeamSelectionsAsync(string username, IEnumerable<PokemonLoadout> loadouts)
    {
        var profile = await GetOrCreateAsync(username);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        foreach (var moveKey in loadouts.SelectMany(loadout => loadout.ChosenMoveNames))
        {
            MovePreferenceRules.Record(preferences, moveKey);
        }
        profile.MovePreferencesJson = JsonSerializer.Serialize(preferences, JsonOptions);
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<PokemonLoadout>?> GetPendingRivalAsync(string username)
    {
        var profile = await GetOrCreateAsync(username);
        if (!profile.RivalPending) return null;

        var latest = DeserializeLoadouts(profile.LatestLoadoutsJson);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        return BuildRivalLoadouts(latest, preferences);
    }

    public async Task CompleteBattleAsync(
        string username,
        IEnumerable<PokemonLoadout> latestLoadouts,
        bool isRivalBattle,
        bool won)
    {
        var profile = await GetOrCreateAsync(username);
        profile.LatestLoadoutsJson = SerializeLoadouts(latestLoadouts);

        if (isRivalBattle)
        {
            if (!profile.RivalPending) return;

            profile.RivalPending = false;
            string keyPrefix = $"rival-{profile.RivalNumber}";
            AddMessageIfMissing(
                username,
                $"{keyPrefix}-{(won ? "win" : "loss")}",
                won ? "라이벌전 승리" : "라이벌전 결과",
                won
                    ? "플레이 성향을 반영한 라이벌을 이겼습니다. 보관함에 기술머신 1개를 지급했습니다."
                    : "라이벌에게 패배했습니다. 다음 일반 전투에서 다시 도전할 수 있습니다.");

            if (won)
            {
                string rewardMoveKey = PickRewardMove(profile);
                AddTechnicalMachine(username, rewardMoveKey);
                AddMessageIfMissing(
                    username,
                    $"{keyPrefix}-reward",
                    "기술머신 보상",
                    $"{MoveDatabase.All[rewardMoveKey].Name} 기술머신을 1개 획득했습니다.");
            }
        }
        else
        {
            profile.CompletedBattles++;
            if (profile.CompletedBattles % 50 == 0 && latestLoadouts.Any())
            {
                profile.RivalPending = true;
                profile.RivalNumber = profile.CompletedBattles / 50;
                AddMessageIfMissing(
                    username,
                    $"rival-{profile.RivalNumber}-scheduled",
                    "라이벌전 예약",
                    $"일반 전투 {profile.CompletedBattles}회를 완료했습니다. 다음 상대는 라이벌입니다.");
            }
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> MarkMessageReadAsync(string username, int messageId)
    {
        var message = await _db.MailboxMessages
            .FirstOrDefaultAsync(item => item.Id == messageId && item.Username == username);
        if (message == null) return false;
        message.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TryConsumeTechnicalMachineAsync(string username, string moveKey)
    {
        if (!MoveDatabase.All.ContainsKey(moveKey)) return false;
        var machine = await _db.TechnicalMachines
            .FirstOrDefaultAsync(item => item.Username == username
                && item.MoveKey == moveKey
                && item.Quantity > 0);
        if (machine == null) return false;

        machine.Quantity--;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<PlayerProgression> GetOrCreateAsync(string username)
    {
        var profile = await _db.PlayerProgressions
            .FirstOrDefaultAsync(item => item.Username == username);
        if (profile != null) return profile;

        profile = new PlayerProgression { Username = username };
        _db.PlayerProgressions.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    private void AddMessageIfMissing(
        string username,
        string deduplicationKey,
        string title,
        string body)
    {
        bool exists = _db.MailboxMessages.Local.Any(message =>
            message.Username == username && message.DeduplicationKey == deduplicationKey)
            || _db.MailboxMessages.Any(message =>
                message.Username == username && message.DeduplicationKey == deduplicationKey);
        if (exists) return;

        _db.MailboxMessages.Add(new MailboxMessage
        {
            Username = username,
            DeduplicationKey = deduplicationKey,
            Title = title,
            Body = body
        });
    }

    private void AddTechnicalMachine(string username, string moveKey)
    {
        var machine = _db.TechnicalMachines.Local.FirstOrDefault(item =>
            item.Username == username && item.MoveKey == moveKey)
            ?? _db.TechnicalMachines.FirstOrDefault(item =>
                item.Username == username && item.MoveKey == moveKey);
        if (machine == null)
        {
            _db.TechnicalMachines.Add(new TechnicalMachineInventory
            {
                Username = username,
                MoveKey = moveKey,
                Quantity = 1
            });
        }
        else
        {
            machine.Quantity++;
        }
    }

    private static string PickRewardMove(PlayerProgression profile)
    {
        var latest = DeserializeLoadouts(profile.LatestLoadoutsJson);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        var alreadySelected = latest
            .SelectMany(loadout => loadout.ChosenMoveNames)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = latest
            .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
            .SelectMany(loadout => PokemonDatabase.All[loadout.PokemonId].MoveNames)
            .Distinct()
            .Where(moveKey => !alreadySelected.Contains(moveKey))
            .OrderByDescending(moveKey => MovePreferenceRules.CountFor(preferences, moveKey))
            .ThenBy(moveKey => moveKey, StringComparer.Ordinal)
            .ToList();
        return candidates.FirstOrDefault()
            ?? latest
                .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
                .SelectMany(loadout => PokemonDatabase.All[loadout.PokemonId].MoveNames)
                .FirstOrDefault()
            ?? "tackle";
    }

    private static List<PokemonLoadout> BuildRivalLoadouts(
        IEnumerable<PokemonLoadout> latest,
        MovePreferenceProfile preferences)
    {
        var rival = new List<PokemonLoadout>();
        foreach (var loadout in latest)
        {
            if (!PokemonDatabase.All.TryGetValue(loadout.PokemonId, out var data)) continue;

            var moves = data.MoveNames
                .Where(MoveDatabase.All.ContainsKey)
                .OrderByDescending(moveKey => MovePreferenceRules.CountFor(preferences, moveKey))
                .ThenBy(moveKey => moveKey, StringComparer.Ordinal)
                .Take(4)
                .ToList();
            if (moves.Count == 0) moves.Add(data.MoveNames.First());

            rival.Add(new PokemonLoadout
            {
                PokemonId = loadout.PokemonId,
                ChosenMoveNames = moves,
                ChosenAbility = data.AbilityNames.Contains(loadout.ChosenAbility)
                    ? loadout.ChosenAbility
                    : data.AbilityNames.FirstOrDefault() ?? "",
                ChosenItem = ItemDatabase.GetAvailableItems(data.Name)
                    .Any(item => item.Name == loadout.ChosenItem)
                    ? loadout.ChosenItem
                    : TeamLoadoutRules.NoItem,
                Level = Math.Max(1, loadout.Level)
            });
        }
        return TeamLoadoutRules.NormalizeUniqueItems(rival);
    }

    private static string SerializeLoadouts(IEnumerable<PokemonLoadout> loadouts) =>
        JsonSerializer.Serialize(loadouts.ToList(), JsonOptions);

    private static List<PokemonLoadout> DeserializeLoadouts(string json) =>
        JsonSerializer.Deserialize<List<PokemonLoadout>>(json, JsonOptions)
        ?? new List<PokemonLoadout>();

    private static MovePreferenceProfile DeserializePreferences(string json) =>
        JsonSerializer.Deserialize<MovePreferenceProfile>(json, JsonOptions)
        ?? new MovePreferenceProfile();
}