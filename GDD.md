# Game Design Document (GDD) — Ruínas do Monte & Pradaria Solar

## 1. Visão Geral
Platformer 2D em Pixel Art 32x32 gerado por código na Unity, com dois protagonistas assimétricos (**Boneco Menino** e **Boneca Kitty**), combate contra robôs patrulheiros estilo Clank, sistema de corações, moedas rúnicas, pontuação, molas, plataformas one-way, plataformas móveis, interruptores com portas rúnicas e blocos frágeis, áudio procedural completo e partículas reativas, com progressão entre **Fase 1 (Ruínas da Noite — Level Design v2 em mapa ASCII)** e **Fase 2 (Pradaria Solar — procedural)**.

## 2. Métricas Físicas (Régua de Level Design)
- **Velocidade horizontal (`moveSpeed`):** $7{,}5\text{ u/s}$
- **Força do pulo (`jumpForce`):** $14{,}5$ | **Escala de gravidade (`gravityScale`):** $4{,}0$ ($g \cdot \text{escala} = 39{,}24\text{ u/s}^2$)
- **Altura máxima do pulo:** $\approx 2{,}68\text{ u}$ (pulo curto ao soltar cedo: $\approx 0{,}8\text{ u}$)
- **Alcance horizontal máximo (chão plano):** $\approx 5{,}5\text{ u}$ ($\approx 0{,}74\text{ s}$ no ar)
- **Combo de Pisão nos robôs (`BoxPlayer2D`):**
  - 1º pisão: $0{,}86 \times \text{jumpForce}$ ($\approx 1{,}9\text{ u}$)
  - 2º pisão seguido sem tocar no chão: $1{,}04 \times \text{jumpForce}$ ($\approx 2{,}9\text{ u}$) + bônus de combo
  - 3º+ pisão seguido sem tocar no chão: $1{,}16 \times \text{jumpForce}$ ($\approx 3{,}6\text{ u}$) + bônus de combo
- **Impulso da Mola (`SpringPad2D`):** $v_y = 16{,}6\text{ u/s}$ ($\approx 3{,}5\text{ u}$ de altura)
- **Margens de segurança para fases (80% do limite):**
  - Degrau confortável $\le 2{,}0\text{ u}$ | Limite máximo $\le 2{,}5\text{ u}$
  - Vão confortável $\le 4{,}0\text{ u}$ | Limite máximo $\le 5{,}0\text{ u}$
  - Distância máxima entre checkpoints (`C`) $\le 25{,}0\text{ u}$

## 3. Personagens, Regras de Vida e Diferenciação (Fase 3)
| Personagem | Habilidade Inicial (Fase 1) | Interação com Obstáculos (Fase 3) | Novo Poder Desbloqueado (Fase 2) |
|---|---|---|---|
| **1. Boneco Menino** | Pistola de Laser (`[Q / X / RT]`), destrói robôs à distância com 2 disparos. | Aciona **Interruptores (`S`)** e quebra **Blocos Frágeis (`F`)** à distância com o Laser. | **Botas de Pulo Esmagador:** passa a esmagar robôs e usar Pisão-Martelo (`[S]`). |
| **2. Boneca Kitty** | **Pulo Esmagador:** derrota robôs ao pular sobre eles, com **Combo de Pisão** de altura crescente. | Aciona **Interruptores (`S`)** pisando neles e quebra **Blocos Frágeis (`F`)** com **Pisão-Martelo (`[S / Baixo]`)**. | **Garras de Arranhão (`[Q / X / RT]`):** causa 1 de dano aos robôs, aciona interruptores e quebra blocos frágeis. |

- **Regra de Respawn e Morte Total:**
  - Cair em um abismo enquanto ainda restam corações desconta 1 coração e retorna o jogador ao último **Checkpoint (`Checkpoint2D`)** ativado.
  - Perder todos os 3 corações (**Fim de Jogo**) e reiniciar a tentativa **sempre retorna o jogador ao início da fase** (`respawnAtCheckpointOnFullDeath = false`), resetando checkpoints, inimigos, plataformas móveis, interruptores, portas, blocos frágeis, moedas e pontuação.

## 4. Mecânicas de Fase e Diferenciação (Fases 2 e 3 Concluídas)
1. **Moedas (`o` / `Coin2D`) e Pontuação (`Score`):** Cada moeda concede $+100\text{ PTS}$ e cada robô derrotado concede $+250\text{ PTS}$.
2. **Plataforma de Mão Única (`=` / `PlatformEffector2D`):** Atravessa de baixo para cima no salto e pousa por cima (`surfaceArc = 170°`).
3. **Mola de Impulso (`M` / `SpringPad2D`):** Lança o jogador a $\approx 3{,}5\text{ u}$ de altura para rotas altas e segredos.
4. **Plataforma Móvel (`~` / `MovingPlatform2D`):** Plataforma cinemática one-way que oscila suavemente entre dois pontos em `FixedUpdate`; o jogador herda sua velocidade horizontal.
5. **Espinhos (`^`) vs. Bloco Sólido (`B`):** Bloco sólido (`B`) é seguro para subir; espinhos (`^`) causam 1 coração de dano.
6. **Interruptor (`S` / `Switch2D`) + Porta Rúnica (`D` / `SwitchGate2D`):** O Menino aciona com o Laser; a Kitty aciona pisando sobre o botão. Ao ativar, o cristal muda de rubi para esmeralda (`+150 PTS`) e recolhe a `SwitchGate2D` correspondente.
7. **Bloco Frágil (`F` / `BreakableBlock2D`):** Pedra rachada com núcleo dourado. O Menino quebra com o Laser; a Kitty quebra com o **Pisão-Martelo** (segurando `S / Seta Baixo` sobre o bloco). Concede $+1\text{ Moeda}$ e $+100\text{ PTS}$.
8. **Combo de Pisão da Kitty (`stompComboCount`):** Pisões encadeados em robôs sem tocar no chão concedem impulso vertical crescente ($1{,}9\text{ u} \to 2{,}9\text{ u} \to 3{,}6\text{ u}$) e bônus de pontuação.

## 5. Bestiário de Inimigos e Física de Abate no Ar (Fase 4 Concluída — `RobotEnemy2D`)
1. **Robô Patrulheiro (`R` / `EnemyBehaviorType.PatrolWalker`):** Patrulha horizontalmente com detecção de borda e parede. Derrotado por 2 tiros de Laser ou 1 Pisão.
2. **Robô Saltador (`J` / `EnemyBehaviorType.Jumper`):** Robô com molas douradas nas pernas que salta em intervalo fixo ($1{,}65\text{ s}$) com *telegraphing* de agachamento ($0{,}28\text{ s}$ antes do pulo).
3. **Torreta Fixa (`T` / `EnemyBehaviorType.Turret`):** Fixa no solo, mira no jogador e dispara orbes de plasma (`TurretProjectile2D`) em linha reta após **$0{,}5\text{ s}$ de *telegraphing*** luminoso.
4. **Robô Voador / Drone (`V` / `EnemyBehaviorType.FlyingDrone`):** Voa em senoide horizontal com espinhos elétricos no topo (**não pode ser pisado por cima**). O Menino o abate com 1 tiro de laser, e a **Kitty (ou o Menino) pode derrubá-lo saltando por baixo de sua barriga** (`CheckHazardObject`).
5. **Física de Queda para Corpos no Ar:** Qualquer robô abatido enquanto estiver no ar (`FlyingDrone` ou `Jumper` durante o salto) sofre aceleração gravitacional ($38\text{ u/s}^2$), quica uma vez e repousa sobre o chão ou plataforma logo abaixo (`FindSupportGroundY`).

## 6. Polimento, Áudio Procedural e UI Ultra-Minimalista (Fase 5 Concluída)
1. **HUD Ultra-Minimalista (`GameUI2D`):** Sem caixas ou molduras de fundo durante a partida — apenas os 3 corações no canto superior esquerdo acompanhados de `Moedas/Total • Pontos` com sombra suave de 1px.
2. **Menu de Pausa (`GameState.Paused` / `[Esc]`):** Pausa a partida com controle de Volume (`- / +` salvo em `PlayerPrefs`), Continuar, Reiniciar Fase, Trocar Personagem e Sair.
3. **Áudio Procedural (`GameAudio2D`):** Efeitos sonoros sintetizados por código (pulo, pouso, laser, pisão, moeda, interruptor, mola, bloco quebrando, dano e vitória) + trilha ambiente contínua por tema (noturna na Fase 1, diurna na Fase 2).
4. **Partículas de "Juice" (`PlayerDustEffect2D`):** Poeira nos pés ao saltar e pousar, além de brilho dourado (`SpawnCoinSparkles`) ao coletar moedas.

## 7. Fase 3 e Geração Procedural v2 (BuildProceduralStageGeometry)
1. **Progressão:** Fase 1 (ASCII) → Fase 2 (Pradaria Solar, diurna) → **Fase 3 (Abismo Cristalino, noturna, mais difícil)** → Vitória. Cada fase procedural gera uma nova seed a cada tentativa.
2. **Mais aleatoriedade:** número de ilhas (F2: 3–6 abismos | F3: 4–7), comprimento das ilhas (14–28 u), **degraus de altura entre ilhas** (-0,5 a +1,5 u em relação ao chão base), travessias variadas (salto simples, plataforma móvel ou 1–3 pedras flutuantes) e pesos dos padrões sorteados por seed.
3. **10 padrões de encontro:** mola + plataforma alta, espinhos duplos, inimigo variado/drone + bloco, espinho + robô, escadaria de blocos, estrada alta com robô embaixo, torreta com cobertura, dois saltadores, corredor de drones e arco de moedas de descanso. Ilha inicial é "gentil" (sem torreta, saltadores e drones).
4. **Fase 3:** inimigos mais velozes (+12%), mais Saltadores/Torretas/Drones, cristais e tochas como cenário, música noturna.
5. **Câmera:** zoom aproximado (orthographicSize 5,6 → 4,4) e seguimento vertical mais responsivo.

## 8. Verticalidade e Identidade da Fase 3 (Montanhas Nevadas)
1. **Verticalidade (Fase 1 + fases procedurais):** `Stage1AsciiMap` expandido para 8 linhas (`100x8`), com torres de plataformas até `+5u` acima do solo, plataformas aéreas sobre abismos e inimigos no alto. Fases 2 e 3 procedurais mantêm degraus de `-2,0` a `+2,0u`, Torres de Plataformas e Pontes Aéreas.
2. **Identidade própria da Fase 3 (`StageVisualTheme.CrystalAbyss`):** montanhas de neve ao fundo em parallax (céu noturno com aurora boreal, picos alpinos nevados, pinheiros com neve e neblina glaciar), chão e plataformas cobertos de neve (`snowGroundTopSprite`, `snowPlatformSlabSprite`), neve caindo em partículas e pinheiros nevados + cristais apoiados diretamente no solo (sem elementos flutuando). Música noturna.

## 9. Salto Geracional 64-Bit (`64x64` + Sprites Pré-Assados via MCP)
1. **Banco de Sprites Nativos 64x64 (`Assets/Resources/Sprites64/`):**
   - **Protagonistas maiores (`42 PPU` ≈ `1,38u–1,43u` de altura no mundo):** `HeroBoy64_*` gerado a partir da referência enviada pelo usuário via `sprite-ai` MCP (`Idle`, `Walk`, `Jump`, `Shoot` com pistola laser ciano e variantes `StompPrep`/`StompDive`/`StompBounce`) e `HeroKitty64_*` gerada em estilo coeso 64-bit (`Idle`, `Walk`, `Jump` e `Scratch1–3`).
   - **Inimigos 64x64 (`46 PPU` ≈ `1,04u–1,24u`):** os 4 arquétipos de `RobotEnemy2D` (`PatrolWalker`, `Jumper`, `Turret`, `FlyingDrone`) gerados em alta definição com iluminação direcional e sombreamento multi-tom, além de sprites derrotados distintos por arquétipo (`Robot64_DeadWalker`, `Robot64_DeadJumper`, `Robot64_DeadTurret`, `Robot64_DeadDrone`).
2. **Upscaling 64-Bit para Blocos e Itens (`PixelArtBuilder`):** moedas, molas, interruptores, checkpoints, blocos quebráveis, portões rúnicos e tiles de terreno/plataformas escalonados para `64 PPU` com suavização diagonal Scale2x, bevel topo-esquerda, oclusão de ambiente e contorno seletivo colorido (Sel-Out).
3. **Performance:** carregamento direto via `Resources/Sprites64/*.png` com `FilterMode.Point` sem compressão (`Sprites64Importer`) e cache estático O(1) em memória em `PixelArt64Generator`, eliminando geração repetida na CPU em runtime.
