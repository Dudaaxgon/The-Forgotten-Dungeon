using UnityEngine;

public class SistemaAlavancas : MonoBehaviour
{
    public static SistemaAlavancas Instance;

    public int[] sequenciaCorreta = { 3, 1, 4, 2, 5 };
    private int[] sequenciaJogador;
    private int posicaoAtual = 0;

    public GameObject portaObj; // referência ao objeto da porta
    private bool portaAberta = false;

    void Awake()
    {
        Instance = this;
        sequenciaJogador = new int[sequenciaCorreta.Length];
    }

    public void AtivarAlavanca(int numeroAlavanca)
    {
        sequenciaJogador[posicaoAtual] = numeroAlavanca;
        posicaoAtual++;

        Debug.Log("Alavanca " + numeroAlavanca + " ativada! Posição: " + posicaoAtual);

        if (posicaoAtual == sequenciaCorreta.Length)
        {
            VerificarSequencia();
        }
    }

    void VerificarSequencia()
    {
        for (int i = 0; i < sequenciaCorreta.Length; i++)
        {
            if (sequenciaJogador[i] != sequenciaCorreta[i])
            {
                Debug.Log("Sequência errada! Resetando...");
                ResetarSequencia();
                return;
            }
        }

        Debug.Log("Sequência correta! Porta abrindo!");
        AbrirPorta();
    }

    void ResetarSequencia()
    {
        posicaoAtual = 0;
        sequenciaJogador = new int[sequenciaCorreta.Length];

        Alavanca[] todasAlavancas = FindObjectsByType<Alavanca>(FindObjectsSortMode.None);
        foreach (Alavanca a in todasAlavancas)
        {
            a.Resetar();
        }
    }

    void AbrirPorta()
    {
        portaAberta = true;
        if (portaObj != null)
        {
            portaObj.SetActive(false); // desativa o colisor da porta
        }
        Debug.Log("Porta aberta!");
    }

    public bool PortaEstaAberta()
    {
        return portaAberta;
    }
}