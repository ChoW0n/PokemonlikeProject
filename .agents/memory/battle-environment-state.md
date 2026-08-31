---
name: Battle environment state
description: Weather and terrain are shared battle state used by model calculations and effect handlers.
---

Weather and terrain state is intentionally shared by Pokémon stat/status calculations and battle effect handlers, so tests that change either state must reset it and run without parallelism.

**Why:** A concurrent test can leave a terrain active while another test expects only weather effects, producing valid-looking but incorrect HP or damage assertions.

**How to apply:** Reset both environment states in test cleanup and keep environment-mutating battle tests serialized.