using UnityEngine;

/// <summary>
/// Marcador de perigo (espinhos, monólitos, inimigos) que substitui checagens frágeis por nome ("Obstacle").
/// </summary>
public class Hazard2D : MonoBehaviour
{
    [Header("Configuração de Dano")]
    public int damage = 1;
    public bool stompable = false;
    public bool blocksGroundCheck = true;
}
