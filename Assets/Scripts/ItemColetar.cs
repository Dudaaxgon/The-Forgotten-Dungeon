using UnityEngine;

public class ItemColetar : MonoBehaviour
{
    [Header("AudioChave")]
    public AudioClip somChave;
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("AudioFragmento")]
    public AudioClip somFragmento;

    public enum TipoItem { Chave, Fragmento }
    public TipoItem tipoItem;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("Player"))
        {
            if (tipoItem == TipoItem.Chave)
            {
                if (GameManager.Instance.temChave)
                {
                    Debug.Log("Você já tem uma chave!");
                    return;
                }
                AudioSource.PlayClipAtPoint(somChave, transform.position, volume);
                GameManager.Instance.temChave = true;
                Debug.Log("Chave coletada!");
                Destroy(gameObject);
            }
            else if (tipoItem == TipoItem.Fragmento)
            {
                AudioSource.PlayClipAtPoint(somFragmento, transform.position, volume);
                GameManager.Instance.fragmentosColetados++;
                Debug.Log("Fragmento coletado: " + 
                GameManager.Instance.fragmentosColetados);
                Destroy(gameObject);
            }
        }
    }
}