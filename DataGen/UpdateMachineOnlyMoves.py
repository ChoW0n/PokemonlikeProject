#!/usr/bin/env python3
"""Add PokeAPI machine-only move metadata to the generated Pokémon database."""

from concurrent.futures import ThreadPoolExecutor, as_completed
import json
from pathlib import Path
import re
import sys
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parent.parent
DATABASE_PATH = ROOT / "PokemonBattle" / "Models" / "PokemonDatabase.cs"
MOVE_DATABASE_PATH = ROOT / "PokemonBattle" / "Models" / "MoveDatabase.cs"
USER_AGENT = "PokemonBattle-MachineOnlyMoves/1.0 (https://github.com/ChoW0n/PokemonlikeProject)"


def fetch_moves(pokemon_id: int) -> tuple[int, list[str], str | None]:
    request = Request(
        f"https://pokeapi.co/api/v2/pokemon/{pokemon_id}",
        headers={"User-Agent": USER_AGENT},
    )
    try:
        with urlopen(request, timeout=30) as response:
            payload = json.load(response)
    except (HTTPError, URLError, TimeoutError, json.JSONDecodeError) as exception:
        return pokemon_id, [], str(exception)

    machine_only: list[str] = []
    for move_slot in payload.get("moves", []):
        move_key = move_slot.get("move", {}).get("name")
        details = move_slot.get("version_group_details") or []
        methods = {
            detail.get("move_learn_method", {}).get("name")
            for detail in details
        }
        if move_key and details and methods == {"machine"}:
            machine_only.append(move_key)
    return pokemon_id, sorted(set(machine_only)), None


def main() -> int:
    source = DATABASE_PATH.read_text()
    if any(line.count("new[] {") >= 4 for line in source.splitlines()):
        raise RuntimeError(
            "PokemonDatabase.cs already has machine-only metadata; regenerate it from a clean source."
        )

    supported_moves = set(
        re.findall(r'All\["([^"]+)"\]', MOVE_DATABASE_PATH.read_text())
    )
    lines = source.splitlines(keepends=True)
    results: dict[int, list[str]] = {}
    failures: dict[int, str] = {}

    with ThreadPoolExecutor(max_workers=16) as executor:
        futures = [executor.submit(fetch_moves, pokemon_id) for pokemon_id in range(1, 722)]
        for future in as_completed(futures):
            pokemon_id, machine_only, error = future.result()
            results[pokemon_id] = [
                move_key for move_key in machine_only if move_key in supported_moves
            ]
            if error:
                failures[pokemon_id] = error

    if failures:
        for pokemon_id, error in sorted(failures.items()):
            print(f"[{pokemon_id}] PokeAPI 조회 실패: {error}", file=sys.stderr)

    output: list[str] = []
    seen = set()
    line_pattern = re.compile(
        r'(All\[(\d+)\] = new PokemonData\(.*?, new\[\] \{ )'
        r'([^}]*)'
        r'( \}, new\[\] \{)'
    )
    for line in lines:
        match = line_pattern.search(line)
        if not match:
            output.append(line)
            continue

        pokemon_id = int(match.group(2))
        machine_only = results.get(pokemon_id, [])
        existing_moves = re.findall(r'"([^"]+)"', match.group(3))
        all_moves = existing_moves + [
            move_key for move_key in machine_only if move_key not in existing_moves
        ]
        move_array = ", ".join(f'"{move_key}"' for move_key in all_moves)
        line = (
            line[: match.start(3)]
            + move_array
            + line[match.end(3) :]
        )
        if line.rstrip().endswith(");"):
            suffix = (
                ", "
                + (
                    "Array.Empty<string>()"
                    if not machine_only
                    else "new[] { " + ", ".join(
                        f'"{move_key}"' for move_key in machine_only
                    ) + " }"
                )
            )
            line = line.rstrip()[:-2] + suffix + ");\n"
        output.append(line)
        seen.add(pokemon_id)

    if len(seen) != 721:
        raise RuntimeError(f"Expected 721 generated Pokémon entries, found {len(seen)}.")

    DATABASE_PATH.write_text("".join(output))
    print(
        f"완료: 포켓몬 {len(seen)}종, "
        f"TM 전용 기술 {sum(len(results.get(i, [])) for i in seen)}개 반영"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())