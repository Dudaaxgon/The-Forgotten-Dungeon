using UnityEngine;

public class Alavanca : MonoBehaviour
{
    public int numeroAlavanca;
    private bool ativada = false;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player") && !ativada)
        {
            ativada = true;
            GetComponent<SpriteRenderer>().color = Color.yellow;
            Debug.Log("Alavanca " + numeroAlavanca + " acionada!");
            SistemaAlavancas.Instance.AtivarAlavanca(numeroAlavanca);
        }
    }

    public void Resetar()
    {
        ativada = false;
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}