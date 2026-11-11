#!/usr/bin/env python3
"""
Validador offline de mapas ASCII para o platformer 2D (Unity).
Lê o mapa Stage1AsciiMap diretamente de LevelBuilder2D.cs (ou testa um mapa quebrado com --test-broken)
e valida com base nas métricas físicas medidas do jogo:
  - Altura máxima do pulo: ~2.68 u (margem segura de degrau: <= 2.5 u)
  - Alcance horizontal máximo: ~5.5 u (margem segura de vão: <= 5.0 u)
  - Espaçamento máximo entre checkpoints (P -> C -> ... -> G): <= 25.0 u
"""

import argparse
import pathlib
import re
import sys
from collections import deque
from typing import List, Tuple

MAX_SAFE_STEP_HEIGHT = 2.5
MAX_SAFE_GAP_WIDTH = 5.0
MAX_CHECKPOINT_SPACING = 25.0


def extract_ascii_map_from_cs(cs_path: pathlib.Path, array_name: str = "Stage1AsciiMap") -> List[str]:
    text = cs_path.read_text(encoding="utf-8")
    pattern = rf"{re.escape(array_name)}\s*=\s*\{{(.*?)\}};"
    match = re.search(pattern, text, re.DOTALL)
    if not match:
        raise ValueError(f"Array '{array_name}' não encontrado em {cs_path}")
    block = match.group(1)
    rows = re.findall(r'"([^"]+)"', block)
    if not rows:
        raise ValueError(f"Nenhuma linha de mapa encontrada em '{array_name}'")
    return rows


def validate_map(rows: List[str]) -> Tuple[bool, List[str], List[str]]:
    errors: List[str] = []
    info: List[str] = []

    if not rows or len(rows) < 2:
        return False, ["Mapa vazio ou com menos de 2 linhas."], info

    width = len(rows[0])
    height = len(rows)
    for idx, r in enumerate(rows):
        if len(r) != width:
            errors.append(f"Linha {idx} tem comprimento {len(r)}, esperado {width}.")

    if errors:
        return False, errors, info

    ground_row = height - 1
    player_positions: List[Tuple[int, int]] = []
    goal_positions: List[Tuple[int, int]] = []
    checkpoint_cols: List[int] = []

    # (d) Verificar se algum perigo (^) ou robô (R) nasce dentro de sólido ou sem chão/plataforma sob os pés
    for r in range(height):
        for c in range(width):
            ch = rows[r][c]
            if ch == "P":
                player_positions.append((c, r))
            elif ch == "G":
                goal_positions.append((c, r))
            elif ch == "C":
                checkpoint_cols.append(c)

            if ch in ("^", "R", "J", "T", "S", "D"):
                if r == ground_row:
                    errors.append(f"[Regra D] '{ch}' na coluna {c}, linha {r} nasce na linha de chão sólido.")
                else:
                    below = rows[r + 1][c]
                    if below not in ("#", "=", "~", "B", "F"):
                        errors.append(
                            f"[Regra D] '{ch}' na coluna {c}, linha {r} nasce no vazio (abaixo: '{below}')."
                        )
                    if r > 0 and rows[r - 1][c] in ("#", "=", "~", "B", "F"):
                        errors.append(
                            f"[Regra D] '{ch}' na coluna {c}, linha {r} está soterrado sob bloco sólido."
                        )
            elif ch == "V":
                if r == ground_row:
                    errors.append(f"[Regra D] Robô voador 'V' na coluna {c}, linha {r} nasce dentro do chão sólido.")

    if len(player_positions) != 1:
        errors.append(f"Esperado exatamente 1 início 'P', encontrado(s): {len(player_positions)}.")
    if len(goal_positions) != 1:
        errors.append(f"Esperado exatamente 1 portal 'G', encontrado(s): {len(goal_positions)}.")

    if not player_positions or not goal_positions:
        return False, errors, info

    p_col, p_row = player_positions[0]
    g_col, g_row = goal_positions[0]

    # (b) Verificar vãos no chão e alturas de degraus/plataformas/obstáculos
    bottom = rows[ground_row]
    col = 0
    max_gap_seen = 0.0
    while col < width:
        if bottom[col] != "#":
            start_gap = col
            while col < width and bottom[col] != "#":
                col += 1
            gap_len = float(col - start_gap)
            # Verifica se há plataforma '=' ou '~' cobrindo o vão
            plat_cols = [
                x
                for x in range(start_gap, col)
                if any(rows[r][x] in ("=", "~") for r in range(ground_row))
            ]
            effective_gap = gap_len if not plat_cols else gap_len * 0.5
            max_gap_seen = max(max_gap_seen, effective_gap)
            if effective_gap > MAX_SAFE_GAP_WIDTH:
                errors.append(
                    f"[Regra B] Vão excessivo de {effective_gap:.1f}u nas colunas {start_gap}..{col - 1} "
                    f"(limite seguro: {MAX_SAFE_GAP_WIDTH:.1f}u)."
                )
        else:
            col += 1

    c = 0
    surface_row = ground_row - 1
    while c < width:
        if rows[surface_row][c] == "^":
            start_obs = c
            while c < width and rows[surface_row][c] == "^":
                c += 1
            obs_width = float(c - start_obs)
            if obs_width > MAX_SAFE_GAP_WIDTH:
                errors.append(
                    f"[Regra B] Sequência contínua de perigos '^' com largura {obs_width:.1f}u nas colunas "
                    f"{start_obs}..{c - 1} excede o alcance horizontal seguro ({MAX_SAFE_GAP_WIDTH:.1f}u)."
                )
        else:
            c += 1

    # (c) Verificar espaçamento de checkpoints ao longo da progressão P -> C... -> G
    waypoints = sorted(set([p_col] + [x for x in checkpoint_cols if p_col <= x <= g_col] + [g_col]))
    max_cp_dist = 0.0
    for i in range(len(waypoints) - 1):
        span = float(waypoints[i + 1] - waypoints[i])
        max_cp_dist = max(max_cp_dist, span)
        if span > MAX_CHECKPOINT_SPACING:
            errors.append(
                f"[Regra C] Distância de {span:.1f}u sem checkpoint entre col {waypoints[i]} e col {waypoints[i + 1]} "
                f"(limite: {MAX_CHECKPOINT_SPACING:.1f}u)."
            )

    # (a) Verificar conectividade (BFS) de P até G usando a física de salto
    # Um nó válido (c, y_level) é uma coluna onde há piso sólido abaixo e não há '#' bloqueando.
    standable_nodes = set()
    for c in range(width):
        if bottom[c] == "#" and rows[surface_row][c] != "^":
            standable_nodes.add((c, 0.0))
        for r in range(ground_row):
            ch = rows[r][c]
            if ch in ("=", "~"):
                elev = float(max(1, (ground_row - 1) - r))
                standable_nodes.add((c, elev))
            elif ch in ("B", "F"):
                elev = float((ground_row - 1) - r + 1)
                standable_nodes.add((c, elev))

    start_node = (p_col, 0.0)
    goal_node = (g_col, 0.0)
    standable_nodes.add(start_node)
    standable_nodes.add(goal_node)

    visited = {start_node}
    queue = deque([start_node])
    reached_goal = False

    while queue:
        curr_c, curr_y = queue.popleft()
        if (curr_c, curr_y) == goal_node:
            reached_goal = True

        for next_c, next_y in standable_nodes:
            if (next_c, next_y) in visited:
                continue
            dx = abs(next_c - curr_c)
            dy = next_y - curr_y
            # Caminhada adjacente ou salto dentro do envelope seguro (dx <= 5.0, subida dy <= 2.5)
            if dx <= MAX_SAFE_GAP_WIDTH and dy <= MAX_SAFE_STEP_HEIGHT:
                visited.add((next_c, next_y))
                queue.append((next_c, next_y))

    if not reached_goal:
        errors.append("[Regra A] Não existe caminho alcançável do início 'P' até a meta 'G'.")

    for r in range(ground_row):
        for col_i in range(width):
            if rows[r][col_i] == "=":
                elev = float(max(1, (ground_row - 1) - r))
                if (col_i, elev) not in visited:
                    errors.append(
                        f"[Regra B] Plataforma '=' na coluna {col_i}, linha {r} (altura {elev:.0f}u) é inalcançável por degraus <= {MAX_SAFE_STEP_HEIGHT:.1f}u."
                    )

    info.append(f"Dimensões: {width}x{height} | Início: col {p_col} | Meta: col {g_col}")
    info.append(
        f"Checkpoints: {len(checkpoint_cols)} (maior distância: {max_cp_dist:.1f}u <= {MAX_CHECKPOINT_SPACING:.1f}u)"
    )
    info.append(f"Maior vão: {max_gap_seen:.1f}u <= {MAX_SAFE_GAP_WIDTH:.1f}u | Caminho P->G: {'SIM' if reached_goal else 'NÃO'}")

    return len(errors) == 0, errors, info


def run_broken_map_self_test() -> bool:
    broken_map = [
        "........................................",
        "..P..........^.................G........",
        "######.......#############.....#########"
    ]
    ok, errs, _ = validate_map(broken_map)
    if ok:
        print("[FALHA NO SELF-TEST] O mapa quebrado passou indevidamente!")
        return False
    print("[OK] Self-test de mapa quebrado falhou como esperado com os erros:")
    for e in errs:
        print(f"  - {e}")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description="Valida o mapa ASCII da fase usando as métricas físicas do jogo.")
    parser.add_argument(
        "--cs-file",
        default="My project/Assets/Scripts/LevelBuilder2D.cs",
        help="Caminho para LevelBuilder2D.cs contendo Stage1AsciiMap",
    )
    parser.add_argument(
        "--test-broken",
        action="store_true",
        help="Executa um teste em um mapa propositalmente quebrado para confirmar que o validador detecta falhas.",
    )
    args = parser.parse_args()

    if args.test_broken:
        return 0 if run_broken_map_self_test() else 1

    cs_path = pathlib.Path(args.cs_file)
    if not cs_path.exists():
        print(f"Erro: arquivo não encontrado: {cs_path}", file=sys.stderr)
        return 1

    rows = extract_ascii_map_from_cs(cs_path)
    ok, errors, info = validate_map(rows)

    print("=== Validador Offline de Fase (validate_level.py) ===")
    for line in info:
        print(f"  [INFO] {line}")

    if not ok:
        print("\n[FALHOU] Problemas encontrados no mapa:")
        for err in errors:
            print(f"  [ERRO] {err}")
        return 1

    print("\n[SUCESSO] Fase v1 validada com sucesso em todas as regras (A, B, C, D)!")
    return 0


if __name__ == "__main__":
    sys.exit(main())
