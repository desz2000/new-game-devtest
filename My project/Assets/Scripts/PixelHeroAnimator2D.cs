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
    public float walkFrameRate = 11f;
    public float idleFrameRate = 2.6f;

    private SpriteRenderer spriteRenderer;

    private static Sprite[] boyIdleSprites;
    private static Sprite[] boyWalkSprites;
    private static Sprite boyJumpSprite;
    private static Sprite boyShootSprite;
    private static Sprite[] boyStompSprites;

    private static Sprite[] kittyIdleSprites;
    private static Sprite[] kittyWalkSprites;
    private static Sprite kittyJumpSprite;
    private static Sprite[] kittyScratchSprites;
    private static bool heroSpritesReady;

    private static Texture2D cachedBoyPreviewTex;
    private static Texture2D cachedKittyPreviewTex;
    private static Texture2D[] cachedBoyStompPreviewTexs;
    private static Texture2D[] cachedKittyScratchPreviewTexs;

    private Animator unityAnimator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int HasPowerHash = Animator.StringToHash("HasStage2Power");
    private static readonly int ScratchTriggerHash = Animator.StringToHash("Scratch");
    private static readonly int StompTriggerHash = Animator.StringToHash("Stomp");
    private static readonly int ShootTriggerHash = Animator.StringToHash("Shoot");

    private float animTimer;
    private float idleTimer;
    private int currentWalkFrame;
    private int currentIdleFrame;
    private int facingDirection = 1; // 1 = Direita, -1 = Esquerda
    private float shootRecoilTimer;
    private float scratchAnimTimer;
    private float stompEffectTimer;
    private const float ScratchAnimDuration = 0.24f;
    private const float StompEffectDuration = 0.28f;

    public int FacingDirection => facingDirection;
    public bool IsScratching => scratchAnimTimer > 0f;

    private void OnEnable()
    {
        EnsureSprites();
        ApplyFrame(GetCurrentIdleSprite(0));
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            EnsureSprites();
            ApplyFrame(GetCurrentIdleSprite(0));
        };
#endif
    }

    public void SetCharacterType(PlayerCharacterType type)
    {
        characterType = type;
        EnsureSprites();
        ApplyFrame(GetCurrentIdleSprite(currentIdleFrame));
    }

    public void EnsureSprites()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ColoredBox2D oldBox = GetComponent<ColoredBox2D>();
            if (oldBox != null)
            {
                oldBox.enabled = false;
            }
        }
        if (unityAnimator == null)
        {
            unityAnimator = GetComponent<Animator>();
        }

        if (heroSpritesReady && boyIdleSprites != null && boyIdleSprites[0] != null && kittyIdleSprites != null && kittyIdleSprites[0] != null)
        {
            return;
        }

        // Salto geracional 64-Bit (64x64 @ 42 PPU = ~1.40u de altura) carregados de Resources/Sprites64/
        boyIdleSprites = new[]
        {
            PixelArt64Generator.GetBoySprite(0, "HeroBoy64_Idle1"),
            PixelArt64Generator.GetBoySprite(1, "HeroBoy64_Idle2")
        };

        Sprite bw1 = PixelArt64Generator.GetBoySprite(2, "HeroBoy64_Walk1");
        Sprite bw2 = PixelArt64Generator.GetBoySprite(3, "HeroBoy64_Walk2");
        Sprite bw3 = PixelArt64Generator.GetBoySprite(4, "HeroBoy64_Walk3");
        boyWalkSprites = new[] { bw1, bw2, bw3, bw2 };

        boyJumpSprite = PixelArt64Generator.GetBoySprite(5, "HeroBoy64_Jump");
        boyShootSprite = PixelArt64Generator.GetBoySprite(6, "HeroBoy64_Shoot");

        boyStompSprites = new[]
        {
            PixelArt64Generator.GetBoySprite(7, "HeroBoy64_StompPrep"),
            PixelArt64Generator.GetBoySprite(8, "HeroBoy64_StompDive"),
            PixelArt64Generator.GetBoySprite(9, "HeroBoy64_StompBounce")
        };

        kittyIdleSprites = new[]
        {
            PixelArt64Generator.GetKittySprite(0, "HeroKitty64_Idle1"),
            PixelArt64Generator.GetKittySprite(1, "HeroKitty64_Idle2")
        };

        Sprite kw1 = PixelArt64Generator.GetKittySprite(2, "HeroKitty64_Walk1");
        Sprite kw2 = PixelArt64Generator.GetKittySprite(3, "HeroKitty64_Walk2");
        Sprite kw3 = PixelArt64Generator.GetKittySprite(4, "HeroKitty64_Walk3");
        kittyWalkSprites = new[] { kw1, kw2, kw3, kw2 };

        kittyJumpSprite = PixelArt64Generator.GetKittySprite(5, "HeroKitty64_Jump");

        kittyScratchSprites = new[]
        {
            PixelArt64Generator.GetKittySprite(6, "HeroKitty64_Scratch1"),
            PixelArt64Generator.GetKittySprite(7, "HeroKitty64_Scratch2"),
            PixelArt64Generator.GetKittySprite(8, "HeroKitty64_Scratch3")
        };

        heroSpritesReady = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void UpdateAnimation(float horizontalInput, bool isGrounded, float deltaTime, float verticalVelocity = 0f, bool hasStage2Power = false)
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

        if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
        {
            unityAnimator.SetFloat(SpeedHash, Mathf.Abs(horizontalInput), 0.08f, deltaTime);
            unityAnimator.SetBool(IsGroundedHash, isGrounded);
            unityAnimator.SetFloat(VerticalVelHash, verticalVelocity);
            unityAnimator.SetBool(HasPowerHash, hasStage2Power);
        }

        // 1. Animação de Arranhão da Boneca Kitty (Novo Poder da Fase 2)
        if (scratchAnimTimer > 0f)
        {
            scratchAnimTimer -= deltaTime;
            if (characterType == PlayerCharacterType.GirlKitty && kittyScratchSprites != null && kittyScratchSprites.Length == 3)
            {
                float progress = Mathf.Clamp01(1f - (scratchAnimTimer / ScratchAnimDuration));
                int frameIdx = Mathf.Clamp(Mathf.FloorToInt(progress * 3f), 0, 2);
                ApplyFrame(kittyScratchSprites[frameIdx]);
                return;
            }
        }

        // 2. Animação de Rebote do Pulo Esmagador (Stomp Bounce)
        if (stompEffectTimer > 0f)
        {
            stompEffectTimer -= deltaTime;
            if (characterType == PlayerCharacterType.BoyLaser && boyStompSprites != null && boyStompSprites.Length >= 3)
            {
                ApplyFrame(boyStompSprites[2]); // BoyStompBounce
                return;
            }
        }

        // 3. Recuo do Disparo de Laser do Menino
        if (shootRecoilTimer > 0f)
        {
            shootRecoilTimer -= deltaTime;
            if (characterType == PlayerCharacterType.BoyLaser && boyShootSprite != null)
            {
                ApplyFrame(boyShootSprite);
                return;
            }
        }

        Sprite jump = (characterType == PlayerCharacterType.GirlKitty) ? kittyJumpSprite : boyJumpSprite;
        Sprite[] walk = (characterType == PlayerCharacterType.GirlKitty) ? kittyWalkSprites : boyWalkSprites;

        if (!isGrounded)
        {
            if (characterType == PlayerCharacterType.BoyLaser && hasStage2Power && boyStompSprites != null && boyStompSprites.Length >= 2)
            {
                if (verticalVelocity < -1.5f)
                {
                    ApplyFrame(boyStompSprites[1]); // BoyStompDive
                    animTimer = 0f;
                    return;
                }
                else if (verticalVelocity < 2.2f)
                {
                    ApplyFrame(boyStompSprites[0]); // BoyStompPrep
                    animTimer = 0f;
                    return;
                }
            }

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

            idleTimer += deltaTime * idleFrameRate;
            if (idleTimer >= 1f)
            {
                idleTimer -= 1f;
                currentIdleFrame = 1 - currentIdleFrame;
            }
            ApplyFrame(GetCurrentIdleSprite(currentIdleFrame));
        }
    }

    public void TriggerShootEffect()
    {
        shootRecoilTimer = 0.14f;
        if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
        {
            unityAnimator.SetTrigger(ShootTriggerHash);
        }
    }

    public void TriggerScratchAttack()
    {
        scratchAnimTimer = ScratchAnimDuration;
        if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
        {
            unityAnimator.SetTrigger(ScratchTriggerHash);
        }
    }

    public void TriggerStompEffect()
    {
        stompEffectTimer = StompEffectDuration;
        if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
        {
            unityAnimator.SetTrigger(StompTriggerHash);
        }
    }

    public Vector3 GetGunMuzzleWorldPosition()
    {
        // Cano ciano da pistola na sprite 64x64 @ 42 PPU
        return transform.position + new Vector3(facingDirection * 0.58f, 0.34f, 0f);
    }

    public Vector3 GetScratchSpawnWorldPosition()
    {
        // Arco do arranhão diante da pata da Kitty na sprite 64x64 @ 42 PPU
        return transform.position + new Vector3(facingDirection * 0.68f, 0.16f, 0f);
    }

    public static Texture2D GetBoyPreviewTexture()
    {
        if (cachedBoyPreviewTex == null)
        {
            cachedBoyPreviewTex = PixelArt64Generator.GetBoyTexture(0, "HeroBoy64_Idle1");
        }
        return cachedBoyPreviewTex;
    }

    public static Texture2D GetKittyPreviewTexture()
    {
        if (cachedKittyPreviewTex == null)
        {
            cachedKittyPreviewTex = PixelArt64Generator.GetKittyTexture(0, "HeroKitty64_Idle1");
        }
        return cachedKittyPreviewTex;
    }

    public static Texture2D[] GetBoyStompPreviewTextures()
    {
        if (cachedBoyStompPreviewTexs == null || cachedBoyStompPreviewTexs.Length != 3 || cachedBoyStompPreviewTexs[0] == null)
        {
            cachedBoyStompPreviewTexs = new[]
            {
                PixelArt64Generator.GetBoyTexture(7, "HeroBoy64_StompPrep"),
                PixelArt64Generator.GetBoyTexture(8, "HeroBoy64_StompDive"),
                PixelArt64Generator.GetBoyTexture(9, "HeroBoy64_StompBounce")
            };
        }
        return cachedBoyStompPreviewTexs;
    }

    public static Texture2D[] GetKittyScratchPreviewTextures()
    {
        if (cachedKittyScratchPreviewTexs == null || cachedKittyScratchPreviewTexs.Length != 3 || cachedKittyScratchPreviewTexs[0] == null)
        {
            cachedKittyScratchPreviewTexs = new[]
            {
                PixelArt64Generator.GetKittyTexture(6, "HeroKitty64_Scratch1"),
                PixelArt64Generator.GetKittyTexture(7, "HeroKitty64_Scratch2"),
                PixelArt64Generator.GetKittyTexture(8, "HeroKitty64_Scratch3")
            };
        }
        return cachedKittyScratchPreviewTexs;
    }

    private Sprite GetCurrentIdleSprite(int frame)
    {
        Sprite[] list = (characterType == PlayerCharacterType.GirlKitty) ? kittyIdleSprites : boyIdleSprites;
        if (list == null || list.Length == 0) return null;
        return list[Mathf.Clamp(frame, 0, list.Length - 1)];
    }

    private void ApplyFrame(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
