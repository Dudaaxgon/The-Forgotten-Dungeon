using UnityEngine;

public class SistemaAlavancas : MonoBehaviour
{
    public static SistemaAlavancas Instance;

    public int[] sequenciaCorreta = { 2, 4, 1, 3 };
    private int[] sequenciaJogador;
    private int posicaoAtual = 0;

    public Porta[] portas; // referência às duas portas de saída
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

        Debug.Log("Sequência correta! Portas abrindo!");
        AbrirPortas();
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

    void AbrirPortas()
    {
        portaAberta = true;
        foreach (Porta porta in portas)
        {
            if (porta != null)
            {
                porta.AbrirPorta();
            }
        }
        Debug.Log("Portas abertas!");
    }

    public bool PortaEstaAberta()
    {
        return portaAberta;
    }
}