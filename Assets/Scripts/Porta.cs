using UnityEngine;
using UnityEngine.SceneManagement;

public class Porta : MonoBehaviour
{
    public string proximaFase;
    public bool precisaDeFragmentos = false;
    public bool portaLivre = false;
    public bool precisaDeAlavancas = false;

    private bool aberta = false;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            if (portaLivre || aberta)
            {
                CarregarProximaFase();
            }
            else if (precisaDeFragmentos)
            {
                if (GameManager.Instance.fragmentosColetados >= 3)
                {
                    CarregarProximaFase();
                }
                else
                {
                    Debug.Log("Faltam fragmentos: " +
                    GameManager.Instance.fragmentosColetados + "/3");
                }
            }
            else if (precisaDeAlavancas)
            {
                Debug.Log("Ative as alavancas na sequência correta!");
            }
            else
            {
                if (GameManager.Instance.temChave)
                {
                    GameManager.Instance.temChave = false;
                    CarregarProximaFase();
                }
                else
                {
                    Debug.Log("Você precisa de uma chave!");
                }
            }
        }
    }

    public void AbrirPorta()
    {
        aberta = true;
        GetComponent<SpriteRenderer>().color = Color.green;
        Debug.Log("Porta aberta!");
    }

    void CarregarProximaFase()
    {
        SceneManager.LoadScene(proximaFase);
    }
}