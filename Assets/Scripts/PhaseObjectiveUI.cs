using TMPro;
using UnityEngine;


public class PhaseObjectiveUI : MonoBehaviour
{
    [Header("Texto do objetivo (edite por fase no Inspector)")]
    [TextArea(2, 4)]
    public string objectiveText = "Objetivo: Encontre a chave e depois encontre a porta de saida.";

    [Header("Referencia da UI (arraste o TextMeshPro no Inspector)")]
    public TextMeshProUGUI objectiveLabel;

    [Header("Atualizar apos pegar a chave (opcional, ex: Fase1)")]
    public bool watchTemChave = false;
    [TextArea(2, 4)]
    public string objectiveTextAfterKey = "Chave encontrada, agora encontre a saida.";

    private bool lastTemChave;

    void Start()
    {
        if (objectiveLabel != null)
        {
            objectiveLabel.text = objectiveText;
        }

        if (watchTemChave && GameManager.Instance != null)
        {
            lastTemChave = GameManager.Instance.temChave;
        }
    }

    void Update()
    {
        if (!watchTemChave || GameManager.Instance == null || objectiveLabel == null)
        {
            return;
        }

        bool currentTemChave = GameManager.Instance.temChave;

        // So atualiza o texto no momento em que temChave passa de false para true,
        // assim nao sobrescreve o texto repetidamente a cada frame.
        if (currentTemChave && !lastTemChave)
        {
            objectiveLabel.text = objectiveTextAfterKey;
        }

        lastTemChave = currentTemChave;
    }
}