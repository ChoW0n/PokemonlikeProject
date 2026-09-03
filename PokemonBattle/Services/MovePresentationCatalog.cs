using PokemonBattle.Models;

namespace PokemonBattle.Services;

/// <summary>
/// Presentation-only move catalog. Rules stay in the engine; this keeps the
/// visual identity of well-known moves while every generated move has a safe
/// category/type fallback.
/// </summary>
public static class MovePresentationCatalog
{
    private static readonly Dictionary<string, string> Specific = new()
    {
        ["tackle"] = "impact", ["scratch"] = "slash", ["quick-attack"] = "dash",
        ["aqua-jet"] = "dash", ["extreme-speed"] = "dash", ["mach-punch"] = "dash",
        ["bullet-punch"] = "dash", ["fake-out"] = "impact",
        ["thunderbolt"] = "beam", ["thunder"] = "beam", ["charge-beam"] = "beam",
        ["flamethrower"] = "beam", ["fire-blast"] = "burst", ["ice-beam"] = "beam",
        ["psychic"] = "wave", ["psybeam"] = "beam", ["shadow-ball"] = "orb",
        ["water-pulse"] = "wave", ["surf"] = "wave", ["hydro-pump"] = "beam",
        ["energy-ball"] = "orb", ["sludge-bomb"] = "orb", ["dark-pulse"] = "wave",
        ["aerial-ace"] = "slash", ["air-slash"] = "slash", ["night-slash"] = "slash",
        ["cross-chop"] = "slash", ["sacred-sword"] = "slash", ["psycho-cut"] = "slash",
        ["earthquake"] = "quake", ["rock-slide"] = "quake", ["stone-edge"] = "slash",
        ["razor-leaf"] = "slash", ["leaf-blade"] = "slash", ["pin-missile"] = "multi",
        ["bullet-seed"] = "multi", ["fury-swipes"] = "multi", ["double-slap"] = "multi",
        ["recover"] = "heal", ["roost"] = "heal", ["rest"] = "heal", ["soft-boiled"] = "heal",
        ["protect"] = "shield", ["detect"] = "shield", ["endure"] = "shield",
        ["kings-shield"] = "shield", ["baneful-bunker"] = "shield",
        ["spiky-shield"] = "shield", ["obstruct"] = "shield",
        ["solar-beam"] = "charge", ["skull-bash"] = "charge", ["fly"] = "charge",
        ["bounce"] = "charge", ["future-sight"] = "delayed", ["doom-desire"] = "delayed",
        ["self-destruct"] = "recoil", ["explosion"] = "recoil", ["memento"] = "recoil"
    };

    public static string Resolve(string moveKey, Move move)
    {
        if (Specific.TryGetValue(moveKey, out var presentation)) return presentation;
        if (move.HealingPercent > 0) return "heal";
        if (move.IsStatus) return "status";
        if (move.MaxHits > 1) return "multi";
        if (MoveRuleMetadata.GetRule(moveKey, move).Kind is MoveRuleKind.Charge) return "charge";
        return move.IsSpecial ? "beam" : TypeColors.GetEffectKind(move.Type, false);
    }
}