using UnityEngine;

/// <summary>
/// Interruptor Rúnico ('S' no mapa ASCII) — Fase 3 (Diferenciação de Personagens).
/// Soluções por personagem:
///   - Boneco Menino: aciona à distância disparando o Laser contra o cristal do interruptor.
///   - Boneca Kitty: aciona pisando/saltando sobre o interruptor (ou usando o Arranhão na Fase 2).
/// Ao ser acionado, abre todas as portas rúnicas (SwitchGate2D) do mesmo grupo (ou a mais próxima)
/// e concede bônus de pontuação (+150 PTS).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Switch2D : MonoBehaviour
{
    [Header("Configuração do Interruptor")]
    public int targetGateGroupId = 1;
    public int activationScoreBonus = 150;
    public bool isActivated;

    private SpriteRenderer sr;
    private BoxCollider2D boxCollider;
    private float pulseTimer;

    private static Sprite cachedInactiveSprite;
    private static Sprite cachedActiveSprite;

    private static readonly string[] SwitchInactiveGrid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "............OOOOOOOO............",
        "..........OORRRRRRRROO..........",
        ".........ORRLLLLLLLLRRO.........",
        "........ORRLLWWWWWWLLRRO........",
        "........ORRLLWWWWWWLLRRO........",
        "........OORRRRRRRRRRRROO........",
        "..........OOrrrrrrrrOO..........",
        "........OOOOrrrrrrrrOOOO........",
        ".......OmmmmOOOOOOOOmmmmO.......",
        "......OmmMMMMMMMMMMMMMMmmO......",
        ".....OmmMMddddddddddddMMmmO.....",
        ".....OmmMMdRRRRRRRRRRdMMmmO.....",
        "....OmmMMddRRRRRRRRRRddMMmmO....",
        "....OmmMMMMMMMMMMMMMMMMMMmmO....",
        "...OddddddddddddddddddddddddO...",
        "...OddddddddddddddddddddddddO...",
        "..OOVVVVVVVVVVVVVVVVVVVVVVVVOO..",
        "..OOVVVVVVVVVVVVVVVVVVVVVVVVOO..",
        "..OOOOOOOOOOOOOOOOOOOOOOOOOOOO..",
        "................................"
    };

    private static readonly string[] SwitchActiveGrid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "............OOOOOOOO............",
        "..........OOGGGGGGGGOO..........",
        ".........OGGCCCCCCCCGGO.........",
        "........OGGCCWWWWWWCCGGO........",
        "........OOGGGGGGGGGGGGOO........",
        "........OOOOggggggggOOOO........",
        ".......OmmmmOOOOOOOOmmmmO.......",
        "......OmmMMMMMMMMMMMMMMmmO......",
        ".....OmmMMddddddddddddMMmmO.....",
        ".....OmmMMdCCCCCCCCCCdMMmmO.....",
        "....OmmMMddGGGGGGGGGGddMMmmO....",
        "....OmmMMMMMMMMMMMMMMMMMMmmO....",
        "...OddddddddddddddddddddddddO...",
        "...OddddddddddddddddddddddddO...",
        "..OOVVVVVVVVVVVVVVVVVVVVVVVVOO..",
        "..OOVVVVVVVVVVVVVVVVVVVVVVVVOO..",
        "..OOOOOOOOOOOOOOOOOOOOOOOOOOOO.."
    };

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        EnsureSprites();

        if (sr != null)
        {
            sr.sprite = isActivated ? cachedActiveSprite : cachedInactiveSprite;
            sr.sortingOrder = 4;
            sr.color = Color.white;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(0.92f, 0.72f);
            boxCollider.offset = new Vector2(0f, -0.12f);
        }
    }

    public void ActivateSwitch(BoxPlayer2D player = null)
    {
        if (isActivated) return;

        isActivated = true;
        pulseTimer = 0.35f;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            EnsureSprites();
            sr.sprite = cachedActiveSprite;
        }

        // Abre as portas rúnicas (SwitchGate2D) do mesmo grupo (ou todas se houver apenas 1 grupo)
        SwitchGate2D[] gates = FindObjectsByType<SwitchGate2D>();
        bool openedAny = false;
        foreach (SwitchGate2D gate in gates)
        {
            if (gate != null && !gate.IsOpen && gate.gateGroupId == targetGateGroupId)
            {
                gate.OpenGate();
                openedAny = true;
            }
        }

        if (!openedAny)
        {
            foreach (SwitchGate2D gate in gates)
            {
                if (gate != null && !gate.IsOpen)
                {
                    gate.OpenGate();
                }
            }
        }

        if (player == null)
        {
            player = FindAnyObjectByType<BoxPlayer2D>();
        }
        if (player != null)
        {
            player.AddScore(activationScoreBonus);
        }

        LaserProjectile2D.SpawnImpactSpark(transform.position + new Vector3(0f, 0.1f, 0f), 0.16f, new Color(0.30f, 1.0f, 0.70f, 1f));
        GameAudio2D.PlaySwitch();
        CameraFollow2D.TriggerShake(0.18f);
        GameInput2D.TriggerRumble(0.16f, 0.32f, 0.07f);
    }

    public void ResetSwitch()
    {
        isActivated = false;
        pulseTimer = 0f;
        transform.localScale = Vector3.one;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            EnsureSprites();
            sr.sprite = cachedInactiveSprite;
            sr.color = Color.white;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.deltaTime;
            float t = pulseTimer / 0.35f;
            transform.localScale = new Vector3(1f + 0.22f * t, 1f - 0.18f * t, 1f);
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    private static void EnsureSprites()
    {
        if (cachedInactiveSprite != null && cachedActiveSprite != null) return;

        cachedInactiveSprite = PixelArtBuilder.CreateSpriteFromGrid(
            "Switch2D_Inactive",
            SwitchInactiveGrid,
            SwitchCharToColor,
            32f,
            32,
            32,
            alignBottom: true
        );
        cachedActiveSprite = PixelArtBuilder.CreateSpriteFromGrid(
            "Switch2D_Active",
            SwitchActiveGrid,
            SwitchCharToColor,
            32f,
            32,
            32,
            alignBottom: true
        );
    }

    private static Color SwitchCharToColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color(0.08f, 0.09f, 0.14f, 1f);
            case 'V': return new Color(0.20f, 0.22f, 0.30f, 1f);
            case 'd': return new Color(0.30f, 0.34f, 0.45f, 1f);
            case 'M': return new Color(0.46f, 0.51f, 0.65f, 1f);
            case 'm': return new Color(0.64f, 0.70f, 0.84f, 1f);
            case 'r': return new Color(0.68f, 0.12f, 0.24f, 1f);
            case 'R': return new Color(0.96f, 0.22f, 0.35f, 1f);
            case 'L': return new Color(1.00f, 0.58f, 0.42f, 1f);
            case 'g': return new Color(0.10f, 0.62f, 0.42f, 1f);
            case 'G': return new Color(0.20f, 0.92f, 0.58f, 1f);
            case 'C': return new Color(0.55f, 1.00f, 0.82f, 1f);
            case 'W': return new Color(0.96f, 1.00f, 0.98f, 1f);
            default:  return Color.clear;
        }
    }
}
