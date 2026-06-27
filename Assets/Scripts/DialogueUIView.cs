using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Este script substitui o antigo OnGUI do NpcDialogue2D.
// Ele NÃO duplica a lógica de diálogo (proximidade, pausa de inimigos, etc) -
// toda essa lógica continua no NpcDialogue2D. Aqui a gente só LÊ os dados
// públicos dele (DisplayName, OpeningLine, Options, etc.) e desenha na UI.
public class DialogueUIView : MonoBehaviour
{
    [Header("Referências da UI (arraste no Inspector)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choicesPanel;

    [Header("Botões de escolha (na ordem 1, 2, 3)")]
    public Button[] choiceButtons;          // arraste Choice1Button, Choice2Button, Choice3Button
    public TextMeshProUGUI[] choiceButtonTexts; // o texto (TMP) de cada botão acima, mesma ordem

    [Header("Botão de avançar/encerrar (opcional)")]
    public Button advanceButton;            // se você criar um botão "Continuar/Encerrar" depois
    public TextMeshProUGUI advanceButtonText;

    [Header("Prompt 'Aperte E' (opcional)")]
    public GameObject interactPromptPanel;  // objeto pai do texto "Aperte E" (pode ser o próprio texto, se preferir)
    public TextMeshProUGUI interactPromptText; // texto que mostra "Aperte E para conversar"

    // O NPC que está com diálogo aberto no momento (ou null se nenhum)
    private NpcDialogue2D currentNpc;

    void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (interactPromptPanel != null)
            interactPromptPanel.SetActive(false);
    }

    void Update()
    {
        // Procura, entre todos os NPCs da cena, qual está com IsConversationOpen = true
        NpcDialogue2D openNpc = FindOpenDialogue();

        if (openNpc == null)
        {
            // Nenhum diálogo aberto: garante que a UI está escondida
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(false);
                currentNpc = null;
            }

            // Diálogo fechado: verifica se algum NPC está com o player perto,
            // para mostrar o prompt "Aperte E"
            UpdateInteractPrompt();
            return;
        }

        // Achou um NPC em conversa: mostra/atualiza a UI do diálogo
        currentNpc = openNpc;
        if (!dialoguePanel.activeSelf)
            dialoguePanel.SetActive(true);

        // Com o diálogo aberto, o prompt "Aperte E" não deve aparecer
        if (interactPromptPanel != null && interactPromptPanel.activeSelf)
            interactPromptPanel.SetActive(false);

        RefreshUI();
    }

    // Verifica, entre todos os NpcDialogue2D da cena, se algum está com o player
    // dentro da área de interação (e nenhum diálogo aberto). Mostra/esconde o prompt.
    private void UpdateInteractPrompt()
    {
        if (interactPromptPanel == null)
            return;

        NpcDialogue2D[] allNpcs = FindObjectsByType<NpcDialogue2D>(FindObjectsSortMode.None);
        foreach (var npc in allNpcs)
        {
            if (npc.IsPlayerInRange)
            {
                if (!interactPromptPanel.activeSelf)
                    interactPromptPanel.SetActive(true);

                if (interactPromptText != null)
                    interactPromptText.text = $"Aperte E para conversar com {npc.DisplayName}";

                return;
            }
        }

        // Nenhum NPC com o player perto: esconde o prompt
        if (interactPromptPanel.activeSelf)
            interactPromptPanel.SetActive(false);
    }

    // Procura entre todos os NpcDialogue2D da cena qual está com a conversa aberta
    private NpcDialogue2D FindOpenDialogue()
    {
        NpcDialogue2D[] allNpcs = FindObjectsByType<NpcDialogue2D>(FindObjectsSortMode.None);
        foreach (var npc in allNpcs)
        {
            if (npc.IsConversationOpen)
                return npc;
        }
        return null;
    }

    // Atualiza nome, texto e opções/botões de acordo com a página atual do NPC
    private void RefreshUI()
    {
        npcNameText.text = currentNpc.DisplayName;

        int currentPage = currentNpc.CurrentPage;

        if (currentPage == 0)
        {
            // Página 0: fala inicial + opções de escolha
            dialogueText.text = currentNpc.OpeningLine;
            ShowChoices(currentNpc.Options);
        }
        else if (currentPage == 1)
        {
            // Página 1: resposta da opção escolhida
            HideChoices();
            var options = currentNpc.Options;
            int selected = currentNpc.SelectedOptionIndex;
            if (options != null && selected >= 0 && selected < options.Length)
            {
                dialogueText.text = options[selected].Response;
            }
            SetAdvanceButtonLabel(!string.IsNullOrWhiteSpace(currentNpc.ContinuationPrompt) ? "Continuar" : "Encerrar");
        }
        else
        {
            // Página 2: camada de continuação (pergunta extra + resposta)
            HideChoices();
            dialogueText.text = $"Jogador: {currentNpc.ContinuationPrompt}\n\n{currentNpc.DisplayName}: {currentNpc.ContinuationResponse}";
            SetAdvanceButtonLabel("Encerrar");
        }
    }

    private void SetAdvanceButtonLabel(string label)
    {
        if (advanceButton != null)
        {
            advanceButton.gameObject.SetActive(true);
            if (advanceButtonText != null)
                advanceButtonText.text = label;
        }
    }

    private void ShowChoices(NpcDialogue2D.DialogueOption[] options)
    {
        Debug.Log($"[DialogueUIView] ShowChoices chamado. options é null? {options == null}. Quantidade: {options?.Length ?? -1}. choicesPanel null? {choicesPanel == null}. choiceButtons.Length: {choiceButtons?.Length ?? -1}");

        choicesPanel.SetActive(true);
        if (advanceButton != null)
            advanceButton.gameObject.SetActive(false);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (options != null && i < options.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtonTexts[i].text = options[i].Label;

                int optionIndex = i; // cópia local para evitar bug de closure
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(optionIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideChoices()
    {
        choicesPanel.SetActive(false);
    }

    private void OnChoiceClicked(int index)
    {
        if (currentNpc != null)
        {
            currentNpc.SelectOption(index);
        }
    }

    // Conecte este método ao "On Click" do seu botão de Avançar/Encerrar no Inspector,
    // ou chame AdvanceOrClose() de outro script se preferir usar tecla Enter/Espaço.
    public void OnAdvanceClicked()
    {
        if (currentNpc != null)
        {
            currentNpc.AdvanceConversation();
        }
    }
}