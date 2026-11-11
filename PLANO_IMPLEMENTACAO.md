# Plano de implementação: mecânicas novas e level design v2

Documento para o agente game-dev. Leia antes de qualquer tarefa e siga as skills em `.claude/skills/` (`unity-conventions`, `game-feel`, `gdd-sync`).

## 1. Estado atual do jogo (lido do código)

Platformer 2D em Unity 2022.3, tudo gerado por código (sem arte nem fase em arquivo).

| Peça | Arquivo | Resumo |
|---|---|---|
| Jogador | `BoxPlayer2D.cs` (778 linhas) | Movimento A/D, pulo (Espaço/W) com coyote time e buffer, pulo curto ao soltar, queda rápida (S), vida em 3 corações, knockback, invencibilidade, squash/stretch, **e também** HUD, tela de seleção, telas de vitória/derrota |
| Personagens | `PixelHeroAnimator2D.cs` | Menino: laser (Q), não pisa em robô. Menina Kitty: pisa em robô, sem arma |
| Inimigo | `RobotEnemy2D.cs` | Patrulha tipo Goomba, 2 de vida, morre por 2 tiros ou por pisão |
| Projétil | `LaserProjectile2D.cs` | Reto, só horizontal, some ao bater em sólido |
| Fase | `LevelBuilder2D.cs` | Uma fase linear de ~83 unidades, chão em y = -2, 1 buraco, 1 plataforma, 11 obstáculos vermelhos, 6 robôs, portal no fim |
| Editor | `LevelSceneSetup.cs` | Reconstrói e **salva a cena automaticamente** ao abrir a Unity |

### Física medida (use como régua em todo design de fase)
- Altura máxima do pulo: **~2,68** (v²/(2·g·gravityScale), com v = 14,5; g·escala = 39,24). Pulo curto: ~0,8.
- Tempo no ar (chão plano): ~0,74 s. Alcance horizontal máximo: **~5,5** (a 7,5 u/s).
- Pisão da Kitty: impulso de 0,85 × pulo, ou seja, ~1,9 de altura.
- Margens de design (80% do máximo): altura confortável ≤ 2,0, difícil até 2,5; vão confortável ≤ 4,0, difícil até 5,0.

### Diagnóstico do level design ("cru")
1. Os obstáculos têm 0,95 a 1,4 de altura e o pulo vai a 2,68: nenhum exige habilidade. O único vão (3,2) usa só 58% do alcance.
2. A fase é um corredor plano. O eixo vertical quase não é usado (uma plataforma a 1,1 do chão).
3. Todos os perigos são iguais (bloco vermelho no chão). Não há ensino, variação nem clímax.
4. Sem checkpoint de verdade: cair no vão leva ao ponto fixo `(44.5, 0)` escrito no código. Morrer reinicia tudo.
5. Sem recompensa, segredo ou escolha. Os dois personagens têm a mesma fase, então suas mecânicas pouco mudam o caminho.
6. Sem cenário, parallax, música ou som.

### Dívidas técnicas que travam o crescimento
- Regras por **nome de objeto** (`StartsWith("Obstacle")`, `"Goal"`), frágil.
- `BoxPlayer2D` mistura input, física, estado de jogo e UI (IMGUI).
- Posições da fase e do respawn **hardcoded**; sem formato de dados, então cada ajuste de fase é edição de C#.
- Input antigo (`Input.GetKey`), sem remapeamento nem gamepad.
- O agente não abre a Unity, então toda validação precisa ser possível **fora** dela quando der.

## 2. Regras de trabalho do agente
- Uma tarefa = um commit pequeno, na branch `feature/<nome>`. Todo push em `feature/**` dispara build Windows no CI.
- Não alterar comportamento durante refatoração (Fase 0). Mudança de balanceamento vai em commit separado.
- Ao fim de cada tarefa, entregar: o que mudou, o que **não** foi testado e um roteiro de **teste manual em Play Mode** (3 a 6 passos).
- Valores de design vêm de campos `public` com `[Header]`; nada de número mágico novo.
- Atualizar `GDD.md` (skill `gdd-sync`) ao concluir cada fase.
- Dúvida de design: apresentar 2 a 3 opções ao usuário e esperar a escolha.

## 3. Roadmap

### Fase 0: Fundação (sem mudar o jogo)
Objetivo: permitir iterar em fases e mecânicas sem editar C# a cada vez.

- **0.1 Marcadores por componente.** Criar `Hazard2D` (dano, flag `stompable`), `Goal2D`, `Checkpoint2D`. Trocar todos os `StartsWith(...)` por `GetComponent`. *Pronto quando:* nenhuma checagem por nome resta e o jogo se comporta igual.
- **0.2 Separar UI e estado.** Extrair `GameState` (Seleção, Jogando, Vitória, Derrota) e `GameUI2D` (HUD, telas) do `BoxPlayer2D`. *Pronto quando:* `BoxPlayer2D` só trata movimento, dano e habilidades.
- **0.3 Fase em dados (ASCII).** `LevelBuilder2D` passa a ler um mapa em `string[]`, no mesmo estilo das grades de sprite já usadas. 1 caractere = 1 unidade. Legenda sugerida: `#` chão, `=` plataforma, `^` perigo, `R` robô, `C` checkpoint, `G` meta, `P` início, `o` moeda, `.` vazio. A fase atual é reproduzida primeiro, fiel ao original. *Pronto quando:* a fase v1 construída pelo mapa é equivalente à atual.
- **0.4 Validador de fase offline.** `tools/validate_level.py` lê o mesmo mapa e, usando a física da seção 1, verifica: (a) existe caminho do início à meta; (b) nenhum vão ou degrau passa de 5,0 / 2,5; (c) há checkpoint ao menos a cada ~25 unidades; (d) nenhum perigo ou robô nasce dentro de sólido. Roda no sandbox, sem Unity. *Pronto quando:* falha em um mapa quebrado de propósito e passa na fase v1.
- **0.5 Respawn por checkpoint.** Substituir o `(44.5, 0)` fixo pelo último `Checkpoint2D` tocado. Morte total volta ao último checkpoint, não ao início, a menos que o usuário prefira o contrário (**decisão do usuário**).

### Fase 1: Level design v2 da Fase 1
Estrutura em 5 seções, reaproveitando o princípio ensinar, testar, complicar, aliviar:

1. **Ensino (0 a 15).** Um perigo baixo, um vão curto (≤ 2,5), um robô isolado. Sem dano possível por erro de leitura.
2. **Verticalidade (15 a 35).** Plataformas em duas alturas (1,0 e 2,0). Caminho alto, seguro e com moedas; caminho baixo, com robô.
3. **Checkpoint + descanso (35 a 40).** Espaço aberto, sem ameaça.
4. **Combinação (40 a 65).** Vão de ~4,0 com plataforma intermediária, robô patrulhando uma plataforma, perigos em sequência com ritmo (pular, pausa, pular, pausa).
5. **Clímax (65 a 85).** Vão de ~4,8 seguido de arena curta com 2 a 3 robôs, depois o portal.

Regras de design: nunca dois desafios novos ao mesmo tempo; todo perigo novo é apresentado em local seguro; toda zona de risco alto tem checkpoint antes; sempre ao menos uma moeda ou rota opcional por seção. Rodar `validate_level.py` a cada edição do mapa.

### Fase 2: Mecânicas de fase (valem para os dois personagens)
Ordem sugerida, da mais barata e valiosa para a mais cara:

1. **Moedas e pontuação.** Coletável, contador na HUD, total por tentativa. Base para recompensa e rotas opcionais.
2. **Plataforma de mão única (one-way).** Atravessa por baixo, pousa por cima. Libera a verticalidade da seção 2. Usar `PlatformEffector2D`.
3. **Mola (spring pad).** Impulso fixo (~3,5 de altura). Cria segredos e rotas altas.
4. **Plataforma móvel.** Vai e volta entre dois pontos; o jogador herda a velocidade. Cuidado com `isGrounded` e jitter.
5. **Espinhos vs. bloco.** Separar visual e regra: espinho dá dano, bloco sólido não. Hoje o "obstáculo" é um bloco que machuca.

### Fase 3: Mecânicas que diferenciam os personagens
Princípio: o mesmo obstáculo tem duas soluções, uma por personagem. Isso dá rejogabilidade sem criar fases duplicadas.

- **Interruptor.** Menino aciona com laser; Kitty pisando. Abre uma porta ou ponte.
- **Bloco frágil.** Menino quebra com laser; Kitty com pisão-martelo (S no ar sobre o bloco). Esconde rota ou moedas.
- **Pisão da Kitty com escalada.** Pisões encadeados em robôs dão altura crescente (combo), para alcançar plataformas altas.
- **Laser com alcance de design.** Laser atravessa vidro ou ricocheteia em espelho (opcional, só se o usuário aprovar).

### Fase 4: Inimigos novos
Cada inimigo ensina algo e tem contra-jogada clara. Reaproveitar `RobotEnemy2D` como base (herança ou componente `Enemy2D`).

- **Robô saltador.** Pula em intervalo fixo; força o jogador a ler o tempo.
- **Torreta.** Parada, dispara em linha reta com telegraphing (0,5 s de aviso). Só o laser do Menino alcança de longe; a Kitty precisa se aproximar.
- **Robô voador.** Senoide horizontal; não pode ser pisado, só atingido por laser (o desafio da Kitty é desviar).

### Fase 5: Polimento e "juice"
- Parallax de fundo (2 a 3 camadas geradas por código), partículas de poeira no pouso e de moeda.
- Som: pulo, laser, dano, moeda, vitória. Música em loop. Volume ajustável.
- Migrar para o **Input System** com remapeamento e gamepad (tarefa isolada; é invasiva).
- Tela de título e pausa.

### Fase 6: Progressão
- Seletor de fases com 3 a 5 mapas ASCII. Cada fase introduz **uma** ideia nova.
- Estrelas ou notas por moedas e vidas restantes. Persistência com `PlayerPrefs`.

## 4. Backlog de decisões que cabem ao usuário
- Morte total: volta ao último checkpoint ou ao início da fase?
- Quer vidas (limite de tentativas) ou tentativas infinitas, como hoje?
- A arte continua 100% gerada por código ou vai passar a usar sprites/tilemaps importados?
- Tom da fase 2: mais difícil, mais curta, ou tema novo?
- Gamepad e remapeamento entram no escopo?

## 5. Riscos e cuidados
- `LevelSceneSetup` salva a cena sozinho ao abrir a Unity. Mudar o formato da fase pode regenerar a cena e gerar diff grande em `SampleScene.unity`; avisar antes.
- Mudar `jumpForce`, `gravityScale` ou `moveSpeed` invalida as margens da seção 1 e a fase inteira. Qualquer mudança nesses valores exige reexecutar o validador e atualizar esta tabela.
- Sem a Unity no sandbox, nada aqui foi compilado. Todo código novo precisa passar por uma abertura do projeto na Unity antes do merge.
