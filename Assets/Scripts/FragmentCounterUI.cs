using TMPro;
using UnityEngine;

// Mostra "Fragmentos: X/3" na tela, atualizando sempre que
// GameManager.Instance.fragmentosColetados mudar. Nao interfere na logica
// de coleta, que continua no ItemColetar.cs - aqui so LEMOS o valor.
public class FragmentCounterUI : MonoBehaviour
{
    [Header("Referencia da UI (arraste o TextMeshPro no Inspector)")]
    public TextMeshProUGUI counterLabel;

    [Header("Total de fragmentos da fase")]
    public int totalFragmentos = 3;

    void Update()
    {
        if (counterLabel == null || GameManager.Instance == null)
        {
            return;
        }

        counterLabel.text = $"Fragmentos: {GameManager.Instance.fragmentosColetados}/{totalFragmentos}";
    }
}
