using UnityEngine;

public enum PlayerCharacterType
{
    BoyLaser = 0,
    GirlKitty = 1
}

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class PixelHeroAnimator2D : MonoBehaviour
{
    public PlayerCharacterType characterType = PlayerCharacterType.BoyLaser;
    public float walkFrameRate = 12f;

    private SpriteRenderer spriteRenderer;

    private Sprite boyIdleSprite;
    private Sprite[] boyWalkSprites;
    private Sprite boyJumpSprite;

    private Sprite kittyIdleSprite;
    private Sprite[] kittyWalkSprites;
    private Sprite kittyJumpSprite;

    private static Texture2D cachedBoyPreviewTex;
    private static Texture2D cachedKittyPreviewTex;

    private float animTimer;
    private int currentWalkFrame;
    private int facingDirection = 1; // 1 = Direita, -1 = Esquerda
    private float shootRecoilTimer;

    // ================== SPRITES DO BONECO MENINO (CAMISETA AMARELA + LASER) ==================
    private static readonly string[] BoyIdleGrid =
    {
        "....HHHHHH......",
        "...HHHHHHHHH....",
        "...HHHSSSSSH....",
        "...HHSSSSESSS...",
        "....HSSSSSSSS...",
        ".....SSSSSSS....",
        "....YYYYYYYY....",
        "...yYYYYYYYYGGGG",
        "...SYYYYYYYYgGGL",
        "...SYYYYYYYYg...",
        "....BBBBBBBB....",
        "....BBBBBBBB....",
        "....BBB..BBB....",
        "....BBB..BBB....",
        "....BBB..BBB....",
        "....KKKK.KKKK..."
    };

    private static readonly string[] BoyWalk1Grid =
    {
        "....HHHHHH......",
        "...HHHHHHHHH....",
        "...HHHSSSSSH....",
        "...HHSSSSESSS...",
        "....HSSSSSSSS...",
        ".....SSSSSSS....",
        "...yYYYYYYYY....",
        "..SSYYYYYYYYGGGG",
        "....YYYYYYYYgGGL",
        "....YYYYYYYYg...",
        "....BBBBBBBB....",
        "...bbbb..BBBB...",
        "..bbbb....BBBB..",
        "..bbb......BBB..",
        ".KKKK......KKKK.",
        "................"
    };

    private static readonly string[] BoyWalk2Grid =
    {
        "................",
        "....HHHHHH......",
        "...HHHHHHHHH....",
        "...HHHSSSSSH....",
        "...HHSSSSESSS...",
        "....HSSSSSSSS...",
        ".....SSSSSSS....",
        "....YYYYYYYYGGGG",
        "...yYYYYYYYYgGGL",
        "...SYYYYYYYYg...",
        "....BBBBBBBB....",
        ".....bbbbBBBB...",
        ".....bbb.BBBB...",
        ".....bbb..BBB...",
        "....KKKK..BBB...",
        "..........KKKK.."
    };

    private static readonly string[] BoyWalk3Grid =
    {
        "....HHHHHH......",
        "...HHHHHHHHH....",
        "...HHHSSSSSH....",
        "...HHSSSSESSS...",
        "....HSSSSSSSS...",
        ".....SSSSSSS....",
        "....YYYYYYYY....",
        "....YSYYYYYYGGGG",
        ".....SSYYYYYgGGL",
        "....YYYYYYYYg...",
        "....BBBBBBBB....",
        "...BBBB..bbbb...",
        "..BBBB....bbbb..",
        "..BBB......bbb..",
        ".KKKK......KKKK.",
        "................"
    };

    private static readonly string[] BoyJumpGrid =
    {
        "..SS.HHHHHH.....",
        "..SSHHHHHHHHH...",
        "..yYHHHSSSSSH...",
        "..yYHHSSSSESSS..",
        "...yYHSSSSSSSS..",
        "....YYYYSSSSS...",
        "....YYYYYYYY....",
        "....YYYYYYYYGGGG",
        "....YYYYYYYYgGGL",
        "....BBBBBBBBg...",
        "...bbbbBBBBBB...",
        "..bbbb....BBBB..",
        ".bbbb.....BBBB..",
        ".KKKK.....KKKK..",
        "................",
        "................"
    };

    // ================== SPRITES DA BONECA MENINA (ESTILO HELLO KITTY) ==================
    private static readonly string[] KittyIdleGrid =
    {
        "..OO.......OO...",
        ".OWWO....RRRWWO.",
        ".OWWOOOOORrRRWO.",
        ".OWWWWWWWWRRRWO.",
        "OOWWWWWWWWWWWWOO",
        ".OWWWOWWWWOWWWO.",
        "OOWWWWWNNWWWWWOO",
        ".OWWWWWWWWWWWWO.",
        "..OOOOOOOOOOOO..",
        "...OPPPPPPPPO...",
        "..WWPBBBBBBPWW..",
        "..WWBBBBBBBBWW..",
        "....BBBBBBBB....",
        "....BBB..BBB....",
        "....WWW..WWW....",
        "....OOO..OOO...."
    };

    private static readonly string[] KittyWalk1Grid =
    {
        "..OO.......OO...",
        ".OWWO....RRRWWO.",
        ".OWWOOOOORrRRWO.",
        ".OWWWWWWWWRRRWO.",
        "OOWWWWWWWWWWWWOO",
        ".OWWWOWWWWOWWWO.",
        "OOWWWWWNNWWWWWOO",
        ".OWWWWWWWWWWWWO.",
        "..OOOOOOOOOOOO..",
        "..WWPPPPPPPPWW..",
        "...OPBBBBBBPO...",
        "....BBBBBBBB....",
        "...bbbb..BBBB...",
        "..WWWW....WWWW..",
        "..OOOO....OOOO..",
        "................"
    };

    private static readonly string[] KittyWalk2Grid =
    {
        "................",
        "..OO.......OO...",
        ".OWWO....RRRWWO.",
        ".OWWOOOOORrRRWO.",
        ".OWWWWWWWWRRRWO.",
        "OOWWWWWWWWWWWWOO",
        ".OWWWOWWWWOWWWO.",
        "OOWWWWWNNWWWWWOO",
        ".OWWWWWWWWWWWWO.",
        "..OOOOOOOOOOOO..",
        "...OPPPPPPPPO...",
        "..WWPBBBBBBPWW..",
        "....bbbbBBBB....",
        "....bbb..BBB....",
        "...WWWW..WWWW...",
        "...OOOO..OOOO..."
    };

    private static readonly string[] KittyWalk3Grid =
    {
        "..OO.......OO...",
        ".OWWO....RRRWWO.",
        ".OWWOOOOORrRRWO.",
        ".OWWWWWWWWRRRWO.",
        "OOWWWWWWWWWWWWOO",
        ".OWWWOWWWWOWWWO.",
        "OOWWWWWNNWWWWWOO",
        ".OWWWWWWWWWWWWO.",
        "..OOOOOOOOOOOO..",
        "...OPPPPPPPPWW..",
        "..WWPBBBBBBPO...",
        "....BBBBBBBB....",
        "...BBBB..bbbb...",
        "..WWWW....WWWW..",
        "..OOOO....OOOO..",
        "................"
    };

    private static readonly string[] KittyJumpGrid =
    {
        "..OO.......OO...",
        ".OWWO....RRRWWO.",
        ".OWWOOOOORrRRWO.",
        ".OWWWWWWWWRRRWO.",
        "OOWWWWWWWWWWWWOO",
        ".OWWWOWWWWOWWWO.",
        "OOWWWWWNNWWWWWOO",
        ".OWWWWWWWWWWWWO.",
        "WWOOOOOOOOOOOOWW",
        "WW.OPPPPPPPPO.WW",
        "....PBBBBBBP....",
        "...bbbbBBBBBB...",
        "..WWWW....WWWW..",
        "..OOOO....OOOO..",
        "................",
        "................"
    };

    public int FacingDirection => facingDirection;

    private void OnEnable()
    {
        EnsureSprites();
        ApplyFrame(GetCurrentIdleSprite());
    }

    private void OnValidate()
    {
        EnsureSprites();
        if (!Application.isPlaying)
        {
            ApplyFrame(GetCurrentIdleSprite());
        }
    }

    public void SetCharacterType(PlayerCharacterType type)
    {
        characterType = type;
        EnsureSprites();
        ApplyFrame(GetCurrentIdleSprite());
    }

    public void EnsureSprites()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        ColoredBox2D oldBox = GetComponent<ColoredBox2D>();
        if (oldBox != null)
        {
            oldBox.enabled = false;
        }

        if (boyIdleSprite == null)
        {
            boyIdleSprite = BuildSpriteFromGrid(BoyIdleGrid, "HeroBoy_Idle");
        }
        if (boyWalkSprites == null || boyWalkSprites.Length != 4 || boyWalkSprites[0] == null)
        {
            Sprite w1 = BuildSpriteFromGrid(BoyWalk1Grid, "HeroBoy_Walk1");
            Sprite w2 = BuildSpriteFromGrid(BoyWalk2Grid, "HeroBoy_Walk2");
            Sprite w3 = BuildSpriteFromGrid(BoyWalk3Grid, "HeroBoy_Walk3");
            boyWalkSprites = new[] { w1, w2, w3, w2 };
        }
        if (boyJumpSprite == null)
        {
            boyJumpSprite = BuildSpriteFromGrid(BoyJumpGrid, "HeroBoy_Jump");
        }

        if (kittyIdleSprite == null)
        {
            kittyIdleSprite = BuildSpriteFromGrid(KittyIdleGrid, "HeroKitty_Idle");
        }
        if (kittyWalkSprites == null || kittyWalkSprites.Length != 4 || kittyWalkSprites[0] == null)
        {
            Sprite kw1 = BuildSpriteFromGrid(KittyWalk1Grid, "HeroKitty_Walk1");
            Sprite kw2 = BuildSpriteFromGrid(KittyWalk2Grid, "HeroKitty_Walk2");
            Sprite kw3 = BuildSpriteFromGrid(KittyWalk3Grid, "HeroKitty_Walk3");
            kittyWalkSprites = new[] { kw1, kw2, kw3, kw2 };
        }
        if (kittyJumpSprite == null)
        {
            kittyJumpSprite = BuildSpriteFromGrid(KittyJumpGrid, "HeroKitty_Jump");
        }

        spriteRenderer.color = Color.white;
    }

    public void UpdateAnimation(float horizontalInput, bool isGrounded, float deltaTime)
    {
        EnsureSprites();

        if (horizontalInput > 0.01f)
        {
            facingDirection = 1;
        }
        else if (horizontalInput < -0.01f)
        {
            facingDirection = -1;
        }

        spriteRenderer.flipX = (facingDirection < 0);

        if (shootRecoilTimer > 0f)
        {
            shootRecoilTimer -= deltaTime;
        }

        Sprite idle = GetCurrentIdleSprite();
        Sprite jump = (characterType == PlayerCharacterType.GirlKitty) ? kittyJumpSprite : boyJumpSprite;
        Sprite[] walk = (characterType == PlayerCharacterType.GirlKitty) ? kittyWalkSprites : boyWalkSprites;

        if (!isGrounded)
        {
            ApplyFrame(jump);
            animTimer = 0f;
            return;
        }

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            animTimer += deltaTime * walkFrameRate;
            if (animTimer >= 1f)
            {
                animTimer -= 1f;
                currentWalkFrame = (currentWalkFrame + 1) % walk.Length;
            }
            ApplyFrame(walk[currentWalkFrame]);
        }
        else
        {
            animTimer = 0f;
            currentWalkFrame = 0;
            ApplyFrame(idle);
        }
    }

    public void TriggerShootEffect()
    {
        shootRecoilTimer = 0.08f;
    }

    public Vector3 GetGunMuzzleWorldPosition()
    {
        return transform.position + new Vector3(facingDirection * 0.52f, 0.03f, 0f);
    }

    public static Texture2D GetBoyPreviewTexture()
    {
        if (cachedBoyPreviewTex == null)
        {
            cachedBoyPreviewTex = BuildTextureFromGrid(BoyIdleGrid);
        }
        return cachedBoyPreviewTex;
    }

    public static Texture2D GetKittyPreviewTexture()
    {
        if (cachedKittyPreviewTex == null)
        {
            cachedKittyPreviewTex = BuildTextureFromGrid(KittyIdleGrid);
        }
        return cachedKittyPreviewTex;
    }

    private Sprite GetCurrentIdleSprite()
    {
        return (characterType == PlayerCharacterType.GirlKitty) ? kittyIdleSprite : boyIdleSprite;
    }

    private void ApplyFrame(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    private static Sprite BuildSpriteFromGrid(string[] grid, string spriteName)
    {
        Texture2D tex = BuildTextureFromGrid(grid);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        s.name = spriteName;
        return s;
    }

    private static Texture2D BuildTextureFromGrid(string[] grid)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int row = 0; row < size; row++)
        {
            string line = grid[row];
            int y = size - 1 - row;
            for (int x = 0; x < size; x++)
            {
                char c = x < line.Length ? line[x] : '.';
                tex.SetPixel(x, y, CharToColor(c));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Color CharToColor(char c)
    {
        switch (c)
        {
            // Menino
            case 'H': return new Color(0.23f, 0.14f, 0.08f, 1f); // Cabelo castanho escuro
            case 'S': return new Color(1.00f, 0.80f, 0.60f, 1f); // Pele
            case 'E': return new Color(0.10f, 0.10f, 0.15f, 1f); // Olho
            case 'Y': return new Color(1.00f, 0.88f, 0.11f, 1f); // Camiseta amarela
            case 'y': return new Color(0.85f, 0.68f, 0.02f, 1f); // Sombra da camiseta amarela
            case 'B': return new Color(0.32f, 0.73f, 1.00f, 1f); // Calça / macacão azul claro
            case 'b': return new Color(0.18f, 0.54f, 0.88f, 1f); // Sombra azul claro
            case 'K': return new Color(0.18f, 0.20f, 0.25f, 1f); // Sapatos escuros
            case 'G': return new Color(0.52f, 0.58f, 0.68f, 1f); // Corpo da arma de laser
            case 'g': return new Color(0.25f, 0.29f, 0.36f, 1f); // Punho da arma de laser
            case 'L': return new Color(0.00f, 0.96f, 1.00f, 1f); // Ponta energética do laser

            // Menina (Estilo Hello Kitty)
            case 'O': return new Color(0.12f, 0.12f, 0.16f, 1f); // Contorno, olhos e bigodes
            case 'W': return new Color(0.99f, 0.99f, 1.00f, 1f); // Pelagem branca
            case 'R': return new Color(1.00f, 0.18f, 0.38f, 1f); // Laço vermelho/rosa vibrante
            case 'r': return new Color(0.78f, 0.06f, 0.24f, 1f); // Centro do laço
            case 'N': return new Color(1.00f, 0.84f, 0.12f, 1f); // Narizinho amarelo
            case 'P': return new Color(1.00f, 0.48f, 0.72f, 1f); // Blusinha rosa
            default:  return new Color(0f, 0f, 0f, 0f);          // Transparente
        }
    }
}
