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

    private Sprite[] boyIdleSprites;
    private Sprite[] boyWalkSprites;
    private Sprite boyJumpSprite;
    private Sprite boyShootSprite;
    private Sprite[] boyStompSprites;

    private Sprite[] kittyIdleSprites;
    private Sprite[] kittyWalkSprites;
    private Sprite kittyJumpSprite;
    private Sprite[] kittyScratchSprites;

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

    // ================== SPRITES 32x32 DO BONECO MENINO (CAMISETA AMARELA + PISTOLA LASER) ==================
    // Legenda:
    // . = Transparente | O = Contorno escuro
    // H/h/l = Cabelo castanho (base/sombra/luz)
    // S/s/P = Pele (base/sombra/rubor) | W/E = Olho branco / pupila
    // Y/y/U = Camiseta amarela (base/sombra/brilho)
    // B/b/c = Calça jeans azul (base/sombra/brilho) | D/M = Cinto / fivela
    // K/k = Botas escuras/sola
    // G/g/L/C = Pistola Laser (metal claro/escuro/ciano brilhante/núcleo branco-ciano)

    private static readonly string[] BoyIdle1Grid =
    {
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO...........",
        ".....OOUYYYYYYYYYYyyO.OOOOOO....",
        "....OOYyyyyYYYYYYyyOOGGGGGGGGO..",
        "....OSSOYYyYYYYYYyOOGGGLLCCCLLO.",
        "....OSSOOOOYYYYYYyOOGgGGGGGGGO..",
        ".....OO...OYYYYYYyOOgGOOOOOOO...",
        "..........ODDDDDMDOOO...........",
        "..........OBBBBBBBBO............",
        ".........OBBcBBBBbBBO...........",
        ".........OBBcBBBBbBBO...........",
        ".........OBBBBOOOBBBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBbBO..OBbBO...........",
        "........OKKKKO..OKKKKO..........",
        "........OKkkKO..OKkkKO..........",
        "........OOOOOO..OOOOOO..........",
        "................................"
    };

    private static readonly string[] BoyIdle2Grid =
    {
        "................................",
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO.OOOOOO....",
        ".....OOUYYYYYYYYYYyyOGGGGGGGGO..",
        "....OOYyyyyYYYYYYyyOOGGLLLCLLLO.",
        "....OSSOYYyYYYYYYyOOGgGGGGGGGO..",
        ".....OO.OOOYYYYYYyOOgGOOOOOOO...",
        "..........ODDDDDMDOOO...........",
        "..........OBBBBBBBBO............",
        ".........OBBcBBBBbBBO...........",
        ".........OBBcBBBBbBBO...........",
        ".........OBBBBOOOBBBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBbBO..OBbBO...........",
        "........OKKKKO..OKKKKO..........",
        "........OKkkKO..OKkkKO..........",
        "........OOOOOO..OOOOOO..........",
        "................................"
    };

    private static readonly string[] BoyWalk1Grid =
    {
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        "......OOOYYYYYYYYOO.............",
        ".....OOUUYYYYYYYYYyOO...........",
        "....OOUYYYYYYYYYYYyyO.OOOOOO....",
        "...OSSOyyyyYYYYYYyyOOGGGGGGGGO..",
        "...OSSOOYYyYYYYYYyOOGGGLLCCCLLO.",
        "....OO..OOOYYYYYYyOOGgGGGGGGGO..",
        "..........OYYYYYYyOOgGOOOOOOO...",
        "..........ODDDDDMDOOO...........",
        ".........OOBBBBBBBBBO...........",
        "........OBBcBBBBBBbBBO..........",
        ".......OBBcBBOOOOBBbBBO.........",
        "......OBBbBO.....OBBbBO.........",
        ".....OBBbBO.......OBBbBO........",
        ".....OBbBO.........OBbBO........",
        "....OKKKKO.........OKKKKO.......",
        "....OKkkKO.........OKkkKO.......",
        "....OOOOOO.........OOOOOO.......",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] BoyWalk2Grid =
    {
        "................................",
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO.OOOOOO....",
        ".....OOUYYYYYYYYYYyyOGGGGGGGGO..",
        ".....OOyyyyYYYYYYyyOOGGLLCCCLLO.",
        "......OSSOYYYYYYYyOOGgGGGGGGGO..",
        ".......OO.OYYYYYYyOOgGOOOOOOO...",
        "..........ODDDDDMDOOO...........",
        "..........OBBBBBBBBO............",
        ".........OBBBBBBBbBBO...........",
        ".........OBBcBBBOBbBO...........",
        "..........OBBbBOOBbBO...........",
        "..........OBBbBO.OBbBO..........",
        "..........OBbBO..OBbBO..........",
        ".........OKKKKO..OBbBO..........",
        ".........OKkkKO.OKKKKO..........",
        ".........OOOOOO.OKkkKO..........",
        "................OOOOOO..........",
        "................................",
        "................................"
    };

    private static readonly string[] BoyWalk3Grid =
    {
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO...........",
        ".....OOUYYYYYYYYYYyyO..OOOOOO...",
        ".....OOyyyyYYYYYYyyOOOGGGGGGGGO.",
        "......OOYYyYYYYYYyOOOGGLLCCCLLO.",
        ".......OSSOYYYYYYyOOOGgGGGGGGGO.",
        "........OO.OYYYYYyOO.gGOOOOOOO..",
        "..........ODDDDDMDOOO...........",
        ".........OOBBBBBBBBBO...........",
        "........OBBbBBBBBBcBBO..........",
        ".......OBBbBBOOOOBBcBBO.........",
        "......OBBbBO.....OBBbBO.........",
        ".....OBbBBO.......OBBbBO........",
        ".....OBbBO.........OBbBO........",
        "....OKKKKO.........OKKKKO.......",
        "....OKkkKO.........OKkkKO.......",
        "....OOOOOO.........OOOOOO.......",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] BoyJumpGrid =
    {
        "....OO....OOOOOOOOOO............",
        "...OSSO.OOllHHHHHHhhOO..........",
        "...OSSOOllHHHHHHHHHhhhO.........",
        "....OYYOlHHHHHHHHHHHHhhhO.......",
        "....OYYOHHhhHHHHHHHHHHhhhhO.....",
        ".....OYOHhOOHHSSSSSShHOhhHO.....",
        ".....OOOHhOSSSSSSSSSSSSsOOhHO...",
        "......OOHOSSSSSWESSSSWESsOHO....",
        ".......OSSSSSSWEsSSSWESsOO......",
        ".......OSSSSSPSSSSSSPSSsO.......",
        "........OSSSSSSSSSSSSsOO........",
        ".........OOSSSSSSSSsOO..........",
        ".........OOYYYYYYYYOO...........",
        "........OOUYYYYYYYYYyOO.OOOOOO..",
        ".......OOUYYYYYYYYYYyyOGGGGGGGGO",
        ".......OOyyyyYYYYYYyyOOGGLLCCCLL",
        "........OOYYyYYYYYYyOOGgGGGGGGGO",
        "..........OYYYYYYYYyOOgGOOOOOOO.",
        "..........ODDDDDMDOOO...........",
        ".........OOBBBBBBBBBO...........",
        "........OBBcBBBBBBbBBO..........",
        ".......OBBcBBOOOOBBbBBO.........",
        "......OBBbBO.....OBBbBBO........",
        ".....OKKKKO.......OBBbBO........",
        ".....OKkkKO......OKKKKO.........",
        ".....OOOOOO......OKkkKO.........",
        ".................OOOOOO.........",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] BoyShootGrid =
    {
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO..OOOOOO...",
        ".....OOUYYYYYYYYYYyyO.OGGGGGGGGO",
        "....OOYyyyyYYYYYYyyOOOGGLLCCCCLC",
        "....OSSOYYyYYYYYYyOOOGGGGGCCCCLC",
        ".....OO.OOOYYYYYYyOOOGgGGGGGGGO.",
        "..........OYYYYYYyOO.gGOOOOOOO..",
        "..........ODDDDDMDOOO...........",
        "..........OBBBBBBBBO............",
        ".........OBBcBBBBbBBO...........",
        ".........OBBcBBBBbBBO...........",
        ".........OBBBBOOOBBBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBBbO..OBbBO...........",
        ".........OBbBO..OBbBO...........",
        "........OKKKKO..OKKKKO..........",
        "........OKkkKO..OKkkKO..........",
        "........OOOOOO..OOOOOO..........",
        "................................"
    };

    // ================== SPRITES 32x32 DA BONECA MENINA (ESTILO HELLO KITTY) ==================
    // Legenda adicional:
    // W/w/q = Pelagem branca (pura / sombra fria / sombra profunda)
    // R/r/a = Laço vermelho-rubi (base / sombra / brilho)
    // N/n = Nariz & botões dourados
    // P/p = Blusinha rosa / rubor
    // E = Olhos ovais escuros | O = Contorno / bigodes

    private static readonly string[] KittyIdle1Grid =
    {
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        "...OOwwwwWWWWWWWWwwwOOO.........",
        "....OOOOOOOOOOOOOOOOO...........",
        ".....OOpPPPPPPPPPPpOO...........",
        "....OWOpPPBBBBBBPPpOWO..........",
        "...OWWwOBNBBBBBBBNBOwWO.........",
        "...OWwOOBBBBBBBBBBBBOwWO........",
        "....OO.OBBcBBBBBBbBBOOO.........",
        ".......OBBcBBBBBBbBBO...........",
        ".......OBBBBBBBBBBBBO...........",
        ".......OBBBBBOOOBBBBO...........",
        ".......OBBbBO..OBbBBO...........",
        ".......OBbBBO..OBbBBO...........",
        ".......OWWWWO..OWWWWO...........",
        ".......OwwwWO..OwwwWO...........",
        ".......OKKKKO..OKKKKO...........",
        ".......OOOOOO..OOOOOO...........",
        "................................"
    };

    private static readonly string[] KittyIdle2Grid =
    {
        "................................",
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        "...OOwwwwWWWWWWWWwwwOOO.........",
        "....OOOOOOOOOOOOOOOOO...........",
        ".....OOpPPPPPPPPPPpOO...........",
        "....OWOpPPBBBBBBPPpOWO..........",
        "...OWWwOBNBBBBBBBNBOwWO.........",
        "...OWwOOBBBBBBBBBBBBOwWO........",
        "....OO.OBBcBBBBBBbBBOOO.........",
        ".......OBBcBBBBBBbBBO...........",
        ".......OBBBBBBBBBBBBO...........",
        ".......OBBBBBOOOBBBBO...........",
        ".......OBBbBO..OBbBBO...........",
        ".......OBbBBO..OBbBBO...........",
        ".......OWWWWO..OWWWWO...........",
        ".......OwwwWO..OwwwWO...........",
        ".......OKKKKO..OKKKKO...........",
        ".......OOOOOO..OOOOOO..........."
    };

    private static readonly string[] KittyWalk1Grid =
    {
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        "...OOwwwwWWWWWWWWwwwOOO.........",
        "....OOOOOOOOOOOOOOOOO...........",
        "...OWWOpPPPPPPPPPPpO............",
        "..OWWwOOBBBBBBBBBBpOWO..........",
        "...OOO.OBNBBBBBBBNBOwWO.........",
        ".......OBBBBBBBBBBBBOOO.........",
        "......OOBBcBBBBBBbBBOO..........",
        ".....OBBcBBBOOOOBBbBBO..........",
        "....OBBbBBO.....OBBbBO..........",
        "....OBbBBO.......OBbBBO.........",
        "....OWWWWO.......OWWWWO.........",
        "....OwwwWO.......OwwwWO.........",
        "....OKKKKO.......OKKKKO.........",
        "....OOOOOO.......OOOOOO.........",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] KittyWalk2Grid =
    {
        "................................",
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        "...OOwwwwWWWWWWWWwwwOOO.........",
        "....OOOOOOOOOOOOOOOOO...........",
        ".....OOpPPPPPPPPPPpOO...........",
        "....OWOpPPBBBBBBPPpOWO..........",
        "....OWOOBNBBBBBBBNBOWO..........",
        ".....OOOBBBBBBBBBBBBOO..........",
        ".......OBBcBBBOBBbBBO...........",
        "........OBBbBOOBbBBO............",
        "........OBbBBO.OBbBO............",
        "........OWWWWO.OBbBO............",
        "........OwwwWO.OWWWWO...........",
        "........OKKKKO.OwwwWO...........",
        "........OOOOOO.OKKKKO...........",
        "...............OOOOOO...........",
        "................................",
        "................................"
    };

    private static readonly string[] KittyWalk3Grid =
    {
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        "...OOwwwwWWWWWWWWwwwOOO.........",
        "....OOOOOOOOOOOOOOOOO...........",
        ".....OOpPPPPPPPPPPpOWWO.........",
        "....OWOpBBBBBBBBBBBOwWWO........",
        "....OWOOBNBBBBBBBNBOOOO.........",
        ".....OOOBBBBBBBBBBBBO...........",
        ".......OOBBcBBBBBBbBBOO.........",
        "......OBBbBBOOOOBBcBBBO.........",
        ".....OBBbBO.....OBBbBBO.........",
        "....OBbBBO.......OBbBBO.........",
        "....OWWWWO.......OWWWWO.........",
        "....OwwwWO.......OwwwWO.........",
        "....OKKKKO.......OKKKKO.........",
        "....OOOOOO.......OOOOOO.........",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] KittyJumpGrid =
    {
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO.......",
        "..OWWWWwWWWWWWWWWWWWWwOO........",
        ".OOwwwwwWWWWWWWWwwwwOOO.........",
        "OWWO.OOOOOOOOOOOOOOOO.OWWO......",
        "OWwO.OOpPPPPPPPPPPpOO.OwWO......",
        ".OOO.OOpPPBBBBBBPPpOO.OOO.......",
        ".......OBNBBBBBBBNBO............",
        ".......OBBBBBBBBBBBBO...........",
        "......OOBBcBBBBBBbBBOO..........",
        ".....OBBcBBBOOOOBBbBBBO.........",
        "....OWWWWO.......OBBbBO.........",
        "....OwwwWO......OWWWWO..........",
        "....OKKKKO......OwwwWO..........",
        "....OOOOOO......OKKKKO..........",
        "................OOOOOO..........",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    // ================== ANIMAÇÃO DO NOVO PODER (FASE 2) - BONECO MENINO: PULO ESMAGADOR ==================
    private static readonly string[] BoyStompPrepGrid =
    {
        "..........OOOOOOOOOO............",
        "........OOllHHHHHHhhOO..........",
        ".......OllHHHHHHHHHhhhO.........",
        "......OlHHHHHHHHHHHHhhhO........",
        ".....OHHhhHHHHHHHHHHhhhhO.......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        "....OOOOOOSSSSSSSSsOOOOOO.......",
        "...OOUUYYYYYYYYYYYYYYYyUOO......",
        "...OSSOYYYYYYYYYYYYYYyyOSSO.....",
        "....OO.OOUYYYYYYYYYYyyO.OO......",
        "........OOYYYYYYYYYYyOO.........",
        "..........ODDDDDMDOOO...........",
        "........OOOBBBBBBBBBOOO.........",
        ".......OBBcBBBBBBBBBbBBO........",
        "......OBBcBBOOOOOOOBBbBBO.......",
        "......OKKKKOO.....OOKKKKO.......",
        "......OKkkKO.......OKkkKO.......",
        "......OOOOOO.......OOOOOO.......",
        ".....OLLCCLLO.....OLLCCLLO......",
        "......OOUUOO.......OOUUOO.......",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] BoyStompDiveGrid =
    {
        "...OO.....OOOOOOOOOO.....OO.....",
        "..OSSO..OOllHHHHHHhhOO..OSSO....",
        "..OYYOOllHHHHHHHHHhhhOOYYO......",
        "...OYYOlHHHHHHHHHHHHhhhOYO......",
        "....OOOHHhhHHHHHHHHHHhhhhO......",
        ".....OHhOOHHSSSSSShHOhhHO.......",
        "....OHhOSSSSSSSSSSSSsOOhHO......",
        "....OHOSSSSSWESSSSWESsOHO.......",
        ".....OSSSSSSWEsSSSWESsOO........",
        ".....OSSSSSPSSSSSSPSSsO.........",
        "......OSSSSSSSSSSSSsOO..........",
        ".......OOSSSSSSSSsOO............",
        ".......OOYYYYYYYYOO.............",
        "......OOUYYYYYYYYYyOO.OOOOOO....",
        ".....OOUYYYYYYYYYYyyOGGGGGGGGO..",
        ".....OOyyyyYYYYYYyyOOGGLLCCCLLO.",
        ".......OOYYyYYYYYYyOOGgGGGGGGGO.",
        ".........OYYYYYYYYyOOgGOOOOOOO..",
        "..........ODDDDDMDOOO...........",
        ".........OBBBBBBBBBBBO..........",
        ".........OBBcBBBBBbBBO..........",
        ".........OBBcBBBBBbBBO..........",
        ".........OBBBBOOOOBBBO..........",
        ".........OBBbO....OBbBO.........",
        "........OKKKKO....OKKKKO........",
        "........OKkkKO....OKkkKO........",
        ".......OLLCCLLO..OLLCCLLO.......",
        "......OLCCCCCCLOOLCCCCCCLO......",
        ".......OOUUMMOO..OOUUMMOO.......",
        "........OOLLOO....OOLLOO........",
        "................................",
        "................................"
    };

    private static readonly string[] BoyStompBounceGrid =
    {
        "..OO......OOOOOOOOOO......OO....",
        ".OSSO...OOllHHHHHHhhOO...OSSO...",
        ".OSSOOOllHHHHHHHHHhhhOOOSSOO....",
        "..OYYUOlHHHHHHHHHHHHhhhUYYO.....",
        "...OYYOHHhhHHHHHHHHHHhhhhO......",
        "....OOOHhOOHHSSSSSShHOhhHO......",
        ".....OHhOSSSSSSSSSSSSsOOhHO.....",
        ".....OHOSSSSSWESSSSWESsOHO......",
        "......OSSSSSSWEsSSSWESsOO.......",
        "......OSSSSSPSSSSSSPSSsO........",
        ".......OSSSSSSSSSSSSsOO.........",
        "........OOSSSSSSSSsOO...........",
        "........OOYYYYYYYYOO............",
        ".......OOUYYYYYYYYYyOO..........",
        "......OOUYYYYYYYYYYyyO..........",
        "......OOyyyyYYYYYYyyOO..........",
        "........OOYYyYYYYYYyOO..........",
        "..........ODDDDDMDOOO...........",
        "........OOOBBBBBBBBBOOO.........",
        ".......OBBcBBBBBBBBBbBBO........",
        "......OBBcBBOOOOOOOBBbBBO.......",
        ".....OKKKKO.........OKKKKO......",
        ".....OKkkKO.........OKkkKO......",
        "....OLLCCLLO.......OLLCCLLO.....",
        "...OLCCCCCCLO.....OLCCCCCCLO....",
        "....OOUUMMOO.......OOUUMMOO.....",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    // ================== ANIMAÇÃO DO NOVO PODER (FASE 2) - BONECA KITTY: ARRANHÃO COM GARRAS ==================
    private static readonly string[] KittyScratch1Grid =
    {
        "....OOO...............OOO.......",
        "...OWWWO.............OrrRO......",
        "..OWWWWWO...........OrRRaRO.....",
        "..OWwWWwO..OOOOOOO..ORRRrRO.....",
        ".OWWWWWwOOOqqqqqqqOORRrrRRRO....",
        ".OWWWWWWWWWWWWWWWWOORRRrRRRRO...",
        ".OWWWWWWWWWWWWWWWWOORRrRRRrRO...",
        "OWWWWWWWWWWWWWWWWWWOOOrRRrOO....",
        "OWWWWWWWWWWWWWWWWWWWWWOOOO......",
        "OWWWWWWEWWWWWWWWWEWWWWWWwO......",
        "OOOWWWWEWWWWWWWWWEWWWWwOOO......",
        "OWWWWWWEWWWWNNWWWEWWWWWWwO......",
        "OOOWWWWwWWWWnnWWWWWWWWwOOO......",
        ".OOWWWWPWWWWWWWWWWPWWWwOO..OO...",
        "..OWWWWwWWWWWWWWWWWWWwOO..OCCO..",
        "...OOwwwwWWWWWWWWwwwOOO..OWCCLO.",
        "....OOOOOOOOOOOOOOOOO...OWWWCCLO",
        ".....OOpPPPPPPPPPPpOOOOOWWWWCLO.",
        "....OWOpPPBBBBBBPPpWWWWWWWWOO...",
        "...OWWwOBNBBBBBBBNBOOOOOOOO.....",
        "...OWwOOBBBBBBBBBBBBO...........",
        "....OO.OBBcBBBBBBbBBO...........",
        ".......OBBcBBBBBBbBBO...........",
        ".......OBBBBBBBBBBBBO...........",
        ".......OBBBBBOOOBBBBO...........",
        ".......OBBbBO..OBbBBO...........",
        ".......OBbBBO..OBbBBO...........",
        ".......OWWWWO..OWWWWO...........",
        ".......OwwwWO..OwwwWO...........",
        ".......OKKKKO..OKKKKO...........",
        ".......OOOOOO..OOOOOO...........",
        "................................"
    };

    private static readonly string[] KittyScratch2Grid =
    {
        ".....OOO...............OOO......",
        "....OWWWO.............OrrRO.....",
        "...OWWWWWO...........OrRRaRO....",
        "...OWwWWwO..OOOOOOO..ORRRrRO....",
        "..OWWWWWwOOOqqqqqqqOORRrrRRRO...",
        "..OWWWWWWWWWWWWWWWWOORRRrRRRRO..",
        "..OWWWWWWWWWWWWWWWWOORRrRRRrRO..",
        ".OWWWWWWWWWWWWWWWWWWOOOrRRrOO...",
        ".OWWWWWWWWWWWWWWWWWWWWWOOOO..aa.",
        ".OWWWWWWEWWWWWWWWWEWWWWWWwO..RNa",
        ".OOOWWWWEWWWWWWWWWEWWWWwOOO...RN",
        ".OWWWWWWEWWWWNNWWWEWWWWWWwO..aaC",
        ".OOOWWWWwWWWWnnWWWWWWWWwOOO..RNC",
        "..OOWWWWPWWWWWWWWWWPWWWwOO....RN",
        "...OWWWWwWWWWWWWWWWWWWwOO...aaCL",
        "....OOwwwwWWWWWWWWwwwOOO....RNCL",
        ".....OOOOOOOOOOOOOOOOO.OOOO..RNC",
        "....OWWOpPPPPPPPPPPpOOWWWWWO..CL",
        "...OWWwOOBBBBBBBBBBpWWWWWWCCLO..",
        "....OOO.OBNBBBBBBBNBOOOOOOOCCLO.",
        "........OBBBBBBBBBBBBO.....OOOO.",
        ".......OOBBcBBBBBBbBBOO.........",
        "......OBBcBBBOOOOBBbBBO.........",
        ".....OBBbBBO.....OBBbBO.........",
        ".....OBbBBO.......OBbBBO........",
        ".....OWWWWO.......OWWWWO........",
        ".....OwwwWO.......OwwwWO........",
        ".....OKKKKO.......OKKKKO........",
        ".....OOOOOO.......OOOOOO........",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] KittyScratch3Grid =
    {
        ".....OOO...............OOO......",
        "....OWWWO.............OrrRO.....",
        "...OWWWWWO...........OrRRaRO....",
        "...OWwWWwO..OOOOOOO..ORRRrRO....",
        "..OWWWWWwOOOqqqqqqqOORRrrRRRO...",
        "..OWWWWWWWWWWWWWWWWOORRRrRRRRO..",
        "..OWWWWWWWWWWWWWWWWOORRrRRRrRO..",
        ".OWWWWWWWWWWWWWWWWWWOOOrRRrOO...",
        ".OWWWWWWWWWWWWWWWWWWWWWOOOO.....",
        ".OWWWWWWEWWWWWWWWWEWWWWWWwO.....",
        ".OOOWWWWEWWWWWWWWWEWWWWwOOO.....",
        ".OWWWWWWEWWWWNNWWWEWWWWWWwO.....",
        ".OOOWWWWwWWWWnnWWWWWWWWwOOO.....",
        "..OOWWWWPWWWWWWWWWWPWWWwOO......",
        "...OWWWWwWWWWWWWWWWWWWwOO.......",
        "....OOwwwwWWWWWWWWwwwOOO........",
        ".....OOOOOOOOOOOOOOOOO..........",
        ".....OOpPPPPPPPPPPpOO...........",
        "....OWOpPPBBBBBBPPpOWWO....aa...",
        "....OWOOBNBBBBBBBNBOwWWO..RNCL..",
        ".....OOOBBBBBBBBBBBBOWWWO..RNCL.",
        ".......OBBcBBBBBBbBBOOWCCLO.RNC.",
        ".......OBBcBBBBBBbBBO..OCCLO.CL.",
        ".......OBBBBBBBBBBBBO...OOOO....",
        ".......OBBBBBOOOBBBBO...........",
        ".......OBBbBO..OBbBBO...........",
        ".......OBbBBO..OBbBBO...........",
        ".......OWWWWO..OWWWWO...........",
        ".......OwwwWO..OwwwWO...........",
        ".......OKKKKO..OKKKKO...........",
        ".......OOOOOO..OOOOOO...........",
        "................................"
    };

    public int FacingDirection => facingDirection;
    public bool IsScratching => scratchAnimTimer > 0f;

    private void OnEnable()
    {
        EnsureSprites();
        ApplyFrame(GetCurrentIdleSprite(0));
    }

    private void OnValidate()
    {
        EnsureSprites();
        if (!Application.isPlaying)
        {
            ApplyFrame(GetCurrentIdleSprite(0));
        }
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
        }
        if (unityAnimator == null)
        {
            unityAnimator = GetComponent<Animator>();
        }

        ColoredBox2D oldBox = GetComponent<ColoredBox2D>();
        if (oldBox != null)
        {
            oldBox.enabled = false;
        }

        if (boyIdleSprites == null || boyIdleSprites.Length != 2 || boyIdleSprites[0] == null)
        {
            boyIdleSprites = new[]
            {
                BuildSpriteFromGrid(BoyIdle1Grid, "HeroBoy32_Idle1"),
                BuildSpriteFromGrid(BoyIdle2Grid, "HeroBoy32_Idle2")
            };
        }

        if (boyWalkSprites == null || boyWalkSprites.Length != 4 || boyWalkSprites[0] == null)
        {
            Sprite w1 = BuildSpriteFromGrid(BoyWalk1Grid, "HeroBoy32_Walk1");
            Sprite w2 = BuildSpriteFromGrid(BoyWalk2Grid, "HeroBoy32_Walk2");
            Sprite w3 = BuildSpriteFromGrid(BoyWalk3Grid, "HeroBoy32_Walk3");
            boyWalkSprites = new[] { w1, w2, w3, w2 };
        }

        if (boyJumpSprite == null)
        {
            boyJumpSprite = BuildSpriteFromGrid(BoyJumpGrid, "HeroBoy32_Jump");
        }

        if (boyShootSprite == null)
        {
            boyShootSprite = BuildSpriteFromGrid(BoyShootGrid, "HeroBoy32_Shoot");
        }

        if (boyStompSprites == null || boyStompSprites.Length != 3 || boyStompSprites[0] == null)
        {
            boyStompSprites = new[]
            {
                BuildSpriteFromGrid(BoyStompPrepGrid, "HeroBoy32_StompPrep"),
                BuildSpriteFromGrid(BoyStompDiveGrid, "HeroBoy32_StompDive"),
                BuildSpriteFromGrid(BoyStompBounceGrid, "HeroBoy32_StompBounce")
            };
        }

        if (kittyIdleSprites == null || kittyIdleSprites.Length != 2 || kittyIdleSprites[0] == null)
        {
            kittyIdleSprites = new[]
            {
                BuildSpriteFromGrid(KittyIdle1Grid, "HeroKitty32_Idle1"),
                BuildSpriteFromGrid(KittyIdle2Grid, "HeroKitty32_Idle2")
            };
        }

        if (kittyWalkSprites == null || kittyWalkSprites.Length != 4 || kittyWalkSprites[0] == null)
        {
            Sprite kw1 = BuildSpriteFromGrid(KittyWalk1Grid, "HeroKitty32_Walk1");
            Sprite kw2 = BuildSpriteFromGrid(KittyWalk2Grid, "HeroKitty32_Walk2");
            Sprite kw3 = BuildSpriteFromGrid(KittyWalk3Grid, "HeroKitty32_Walk3");
            kittyWalkSprites = new[] { kw1, kw2, kw3, kw2 };
        }

        if (kittyJumpSprite == null)
        {
            kittyJumpSprite = BuildSpriteFromGrid(KittyJumpGrid, "HeroKitty32_Jump");
        }

        if (kittyScratchSprites == null || kittyScratchSprites.Length != 3 || kittyScratchSprites[0] == null)
        {
            kittyScratchSprites = new[]
            {
                BuildSpriteFromGrid(KittyScratch1Grid, "HeroKitty32_Scratch1"),
                BuildSpriteFromGrid(KittyScratch2Grid, "HeroKitty32_Scratch2"),
                BuildSpriteFromGrid(KittyScratch3Grid, "HeroKitty32_Scratch3")
            };
        }

        spriteRenderer.color = Color.white;
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

        // Sincroniza parâmetros com Animator Controller do Unity (padrão da skill unity-animation)
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
            // Se o Menino já desbloqueou o Poder de Pulo Esmagador na Fase 2, mostra a animação de Dive-Stomp ao cair!
            if (characterType == PlayerCharacterType.BoyLaser && hasStage2Power && boyStompSprites != null && boyStompSprites.Length >= 2)
            {
                if (verticalVelocity < -1.5f)
                {
                    ApplyFrame(boyStompSprites[1]); // BoyStompDive (botas energizadas apontadas para baixo)
                    animTimer = 0f;
                    return;
                }
                else if (verticalVelocity < 2.2f)
                {
                    ApplyFrame(boyStompSprites[0]); // BoyStompPrep (preparando o pisão no ápice do pulo)
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
        shootRecoilTimer = 0.09f;
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
        return transform.position + new Vector3(facingDirection * 0.56f, 0.01f, 0f);
    }

    public Vector3 GetScratchSpawnWorldPosition()
    {
        return transform.position + new Vector3(facingDirection * 0.68f, 0.02f, 0f);
    }

    public static Texture2D GetBoyPreviewTexture()
    {
        if (cachedBoyPreviewTex == null)
        {
            cachedBoyPreviewTex = BuildTextureFromGrid("HeroBoy32_Preview", BoyIdle1Grid);
        }
        return cachedBoyPreviewTex;
    }

    public static Texture2D GetKittyPreviewTexture()
    {
        if (cachedKittyPreviewTex == null)
        {
            cachedKittyPreviewTex = BuildTextureFromGrid("HeroKitty32_Preview", KittyIdle1Grid);
        }
        return cachedKittyPreviewTex;
    }

    public static Texture2D[] GetBoyStompPreviewTextures()
    {
        if (cachedBoyStompPreviewTexs == null || cachedBoyStompPreviewTexs.Length != 3 || cachedBoyStompPreviewTexs[0] == null)
        {
            cachedBoyStompPreviewTexs = new[]
            {
                BuildTextureFromGrid("HeroBoy32_StompPrep_Prev", BoyStompPrepGrid),
                BuildTextureFromGrid("HeroBoy32_StompDive_Prev", BoyStompDiveGrid),
                BuildTextureFromGrid("HeroBoy32_StompBounce_Prev", BoyStompBounceGrid)
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
                BuildTextureFromGrid("HeroKitty32_Scratch1_Prev", KittyScratch1Grid),
                BuildTextureFromGrid("HeroKitty32_Scratch2_Prev", KittyScratch2Grid),
                BuildTextureFromGrid("HeroKitty32_Scratch3_Prev", KittyScratch3Grid)
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

    private static Sprite BuildSpriteFromGrid(string[] grid, string spriteName)
    {
        return PixelArtBuilder.CreateSpriteFromGrid(spriteName, grid, CharToColor, pixelsPerUnit: 32f, width: 32, height: 32);
    }

    private static Texture2D BuildTextureFromGrid(string key, string[] grid)
    {
        return PixelArtBuilder.CreateTextureFromGrid(key, grid, CharToColor, width: 32, height: 32);
    }

    private static Color CharToColor(char c)
    {
        switch (c)
        {
            // Contorno e detalhes comuns
            case 'O': return new Color(0.08f, 0.08f, 0.13f, 1f); // Contorno pixel art escuro (estilo Celeste/Duck Souls)
            case 'W': return new Color(0.98f, 0.98f, 1.00f, 1f); // Branco puro / esclera / pelagem
            case 'E': return new Color(0.09f, 0.10f, 0.16f, 1f); // Íris escura

            // Menino (Cabelo, pele, jaqueta amarela, calça jeans, laser)
            case 'H': return new Color(0.32f, 0.19f, 0.11f, 1f); // Cabelo castanho médio
            case 'h': return new Color(0.20f, 0.11f, 0.06f, 1f); // Sombra do cabelo
            case 'l': return new Color(0.50f, 0.32f, 0.19f, 1f); // Luz quente do cabelo
            case 'S': return new Color(1.00f, 0.81f, 0.63f, 1f); // Pele base
            case 's': return new Color(0.86f, 0.63f, 0.46f, 1f); // Sombra da pele
            case 'Y': return new Color(0.99f, 0.82f, 0.14f, 1f); // Camiseta/jaqueta amarela
            case 'y': return new Color(0.78f, 0.56f, 0.04f, 1f); // Sombra âmbar da jaqueta
            case 'U': return new Color(1.00f, 0.94f, 0.45f, 1f); // Brilho solar na jaqueta
            case 'D': return new Color(0.24f, 0.16f, 0.12f, 1f); // Cinto de couro
            case 'M': return new Color(0.94f, 0.78f, 0.28f, 1f); // Fivela dourada
            case 'B': return new Color(0.24f, 0.56f, 0.90f, 1f); // Calça jeans / jardineira azul
            case 'b': return new Color(0.14f, 0.36f, 0.68f, 1f); // Sombra azul
            case 'c': return new Color(0.46f, 0.76f, 0.98f, 1f); // Realce jeans
            case 'K': return new Color(0.18f, 0.20f, 0.27f, 1f); // Botas escuras
            case 'k': return new Color(0.34f, 0.37f, 0.46f, 1f); // Sola / brilho da bota
            case 'G': return new Color(0.60f, 0.68f, 0.78f, 1f); // Metal da pistola laser
            case 'g': return new Color(0.28f, 0.33f, 0.42f, 1f); // Empunhadura/sombra metálica
            case 'L': return new Color(0.05f, 0.90f, 1.00f, 1f); // Energia ciano do laser
            case 'C': return new Color(0.78f, 1.00f, 1.00f, 1f); // Núcleo intenso do laser

            // Menina Kitty (Pelagem com volume, laço rubi, blusa rosa)
            case 'w': return new Color(0.84f, 0.86f, 0.93f, 1f); // Sombra suave da pelagem branca
            case 'q': return new Color(0.68f, 0.72f, 0.82f, 1f); // Sombra profunda (volume)
            case 'R': return new Color(0.95f, 0.16f, 0.36f, 1f); // Laço vermelho-rosa vibrante
            case 'r': return new Color(0.68f, 0.07f, 0.24f, 1f); // Dobra/sombra do laço
            case 'a': return new Color(1.00f, 0.55f, 0.68f, 1f); // Brilho do laço
            case 'N': return new Color(1.00f, 0.83f, 0.14f, 1f); // Nariz / botões dourados
            case 'n': return new Color(0.84f, 0.60f, 0.05f, 1f); // Sombra do nariz
            case 'P': return new Color(0.98f, 0.46f, 0.68f, 1f); // Blusinha rosa / rubor
            case 'p': return new Color(1.00f, 0.70f, 0.84f, 1f); // Brilho rosa

            default:  return new Color(0f, 0f, 0f, 0f);
        }
    }
}
