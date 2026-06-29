using TMPro;
using UnityEngine;

// Mostra um aviso grande e centralizado no inicio da fase (ex: "Pressione Espaco
// para atacar."). O aviso fica visivel até o jogador apertar a tecla pela primeira
// vez, e depois desaparece sozinho (mesmo que o jogador mude de cena e volte,
// ele aparece de novo, já que o script reseta junto com a cena).
public class AttackHintUI : MonoBehaviour
{
    [Header("Referencia da UI (arraste o TextMeshPro no Inspector)")]
    public TextMeshProUGUI hintLabel;

    [Header("Texto do aviso")]
    [TextArea(2, 6)]
    public string hintText = "Use W, A, S, D ou as setas para se mover.\nPressione Espaço para atacar.";

    [Header("Tecla que faz o aviso desaparecer")]
    public KeyCode tecla = KeyCode.Space;

    private bool jaApertou = false;

    void Awake()
    {
        if (hintLabel != null)
        {
            hintLabel.text = hintText;
            hintLabel.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (jaApertou)
        {
            return;
        }

        if (Input.GetKeyDown(tecla))
        {
            jaApertou = true;
            if (hintLabel != null)
            {
                hintLabel.gameObject.SetActive(false);
            }
        }
    }
}
