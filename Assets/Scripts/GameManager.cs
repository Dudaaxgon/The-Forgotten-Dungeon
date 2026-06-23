using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Fase 1 - Chave
    public bool temChave = false;

    // Fase 2 - Fragmentos
    public int fragmentosColetados = 0;

    // Decisões do jogador para os finais
    public bool ajudouMercador = false;
    public bool libertouPrisioneira = false;
    public bool falouComGuarda = false;
    public bool falouComHistoriador = false;
    public bool encontrouEspirito = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}