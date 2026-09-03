---
name: Battle environment state
description: Weather and terrain are shared battle state used by model calculations and effect handlers.
---

Weather, terrain, Trick Room, and Gravity are session state owned by BattleEngine and activated at each public rule-operation boundary. Static model APIs resolve the active engine environment for calculations and retain a fallback for legacy direct callers.

**Why:** Blazor event callbacks can resume under different async execution contexts, so an AsyncLocal-only state can disappear between actions even though the scoped BattleEngine is still alive.

**How to apply:** Keep environment-mutating tests serialized and reset weather/field state in cleanup. Route user-visible environment labels through BattleEngine.CurrentWeather/CurrentField rather than reading static state during rendering.