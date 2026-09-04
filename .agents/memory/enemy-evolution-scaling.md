---
name: Enemy evolution scaling
description: Keep opponent evolution-stage pressure aligned with run difficulty without overpowering BST selection.
---

Opponent evolution stage should be a mild multiplier layered on top of the existing BST selection weight. Round and skill adjustment increase the relative chance of later stages, while early runs retain a broad first-stage mix.

**Why:** A strong stage multiplier can make a lower-BST evolved species outrank the intended strongest candidate, causing difficulty to jump in a way unrelated to the run's normal stat curve.

**How to apply:** When tuning opponent selection, compare early and late stage ratios separately from absolute weights, and preserve the existing highest-BST ordering as a regression expectation.