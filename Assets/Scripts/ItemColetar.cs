using UnityEngine;

public class ItemColetar : MonoBehaviour
{
    public enum TipoItem { Chave, Fragmento }
    public TipoItem tipoItem;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            if (tipoItem == TipoItem.Chave)
            {
                GameManager.Instance.temChave = true;
                Debug.Log("Chave coletada!");
            }
            else if (tipoItem == TipoItem.Fragmento)
            {
                GameManager.Instance.fragmentosColetados++;
                Debug.Log("Fragmento coletado: " + 
                GameManager.Instance.fragmentosColetados);
            }

            Destroy(gameObject); // Item some do mapa
        }
    }
}