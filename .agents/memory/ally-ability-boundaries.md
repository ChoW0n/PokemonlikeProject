---
name: Ally ability boundaries
description: How partner-oriented abilities should behave while the battle engine remains 1v1.
---

Partner-oriented abilities must receive an explicit living ally before applying partner bonuses or protection. The opposing Pokémon is never an implicit ally in the 1v1 engine. Effects that explicitly protect or boost their own holder may still apply without a partner.

**Why:** The battle model currently exposes one active Pokémon per side, so treating the opponent as a teammate silently produces incorrect boosts, damage reduction, immunity, or item transfer.

**How to apply:** Keep partner inputs optional and empty in 1v1 callers. Add double-battle side context before enabling Telepathy, Healer, Friend Guard, Symbiosis, or ally-targeted portions of Plus/Minus, Flower Veil, Aroma Veil, and Sweet Veil.