---
name: Sprite ground measurement
description: Rules for keeping animated and community Pokémon sprites aligned to their platform.
---

DataGen must measure the first frame of each sprite independently for front and back views. The bottom transparent-space ratio is based on pixels with alpha greater than zero; missing or failed measurements become zero and remain listed in generated diagnostics.

**Why:** Fixed per-species pixel offsets do not survive responsive sprite sizes and cannot account for different front/back canvases or community GIF overrides.

**How to apply:** Resolve the exact effective URL, including community overrides, before measuring. Keep scale and grounding as separate CSS properties; apply the side-specific ratio only to the fighter sprite, while the platform shadow stays fixed.