using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NpcDialogue2D : MonoBehaviour
{
    [Serializable]
    public sealed class DialogueOption
    {
        [SerializeField] private string label;
        [SerializeField, TextArea(3, 8)] private string response;

        public DialogueOption(string label, string response)
        {
            this.label = label;
            this.response = response;
        }

        public string Label => label;
        public string Response => response;
    }

    private const float InteractionRadius = 2.25f;
    private static NpcDialogue2D activeDialogue;

    [SerializeField] private string npcId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(4, 10)] private string openingLine;
    [SerializeField] private DialogueOption[] options = Array.Empty<DialogueOption>();
    [SerializeField, TextArea(2, 5)] private string continuationPrompt;
    [SerializeField, TextArea(3, 8)] private string continuationResponse;

    private readonly List<EnemyAI2D> pausedEnemies = new();
    private PlayerMovement nearbyPlayer;
    private PlayerMovement conversationPlayer;
    private bool playerMovementWasEnabled;
    private int page;
    private int selectedOption = -1;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle promptStyle;

    public string NpcId => npcId;
    public string DisplayName => displayName;
    public int OptionCount => options?.Length ?? 0;
    public bool IsConversationOpen => activeDialogue == this;

    // --- Getters adicionados para a interface visual (Canvas/TextMeshPro) ---
    public string OpeningLine => openingLine;
    public DialogueOption[] Options => options;
    public string ContinuationPrompt => continuationPrompt;
    public string ContinuationResponse => continuationResponse;
    public int CurrentPage => page;
    public int SelectedOptionIndex => selectedOption;

    public void Configure(
        string id,
        string characterName,
        string opening,
        DialogueOption[] dialogueOptions,
        string followUpPrompt,
        string followUpResponse)
    {
        npcId = id;
        displayName = characterName;
        openingLine = opening;
        options = dialogueOptions ?? Array.Empty<DialogueOption>();
        continuationPrompt = followUpPrompt;
        continuationResponse = followUpResponse;
    }

    private void Awake()
    {
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            var idleHash = Animator.StringToHash("Base Layer.Idle_Down");
            if (animator.HasState(0, idleHash))
            {
                animator.Play(idleHash);
            }
        }
    }

    private void Update()
    {
        if (activeDialogue == this)
        {
            HandleConversationInput();
            return;
        }

        if (activeDialogue != null)
        {
            nearbyPlayer = null;
            return;
        }

        nearbyPlayer = FindNearbyPlayer();
        if (nearbyPlayer != null && Input.GetKeyDown(KeyCode.E))
        {
            BeginConversation(nearbyPlayer);
        }
    }

    private void OnDisable()
    {
        if (activeDialogue == this)
        {
            CloseConversation();
        }
    }

    // OnGUI antigo desativado: a interface visual agora é feita pelo DialogueUIView (Canvas/TextMeshPro).
    // Mantido aqui comentado apenas como referência, caso precise comparar o comportamento original.
    /*
    private void OnGUI()
    {
        EnsureStyles();

        if (activeDialogue == this)
        {
            DrawConversation();
            return;
        }

        if (activeDialogue == null && nearbyPlayer != null)
        {
            var width = Mathf.Min(360f, Screen.width - 32f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 62f, width, 40f);
            GUI.Label(rect, $"E  Conversar com {displayName}", promptStyle);
        }
    }
    */

    public bool BeginConversation(PlayerMovement player)
    {
        if (player == null || activeDialogue != null)
        {
            return false;
        }

        activeDialogue = this;
        conversationPlayer = player;
        playerMovementWasEnabled = player.enabled;
        player.movement = Vector2.zero;
        player.enabled = false;
        page = 0;
        selectedOption = -1;

        pausedEnemies.Clear();
        foreach (var enemy in FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None))
        {
            if (!enemy.enabled)
            {
                continue;
            }

            pausedEnemies.Add(enemy);
            enemy.enabled = false;
            var body = enemy.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        return true;
    }

    public void SelectOption(int index)
    {
        if (activeDialogue != this || page != 0 || options == null || index < 0 || index >= options.Length)
        {
            return;
        }

        selectedOption = index;
        page = 1;
    }

    public void AdvanceConversation()
    {
        if (activeDialogue != this)
        {
            return;
        }

        if (page == 1 && !string.IsNullOrWhiteSpace(continuationPrompt))
        {
            page = 2;
            return;
        }

        CloseConversation();
    }

    public void CloseConversation()
    {
        if (activeDialogue != this)
        {
            return;
        }

        foreach (var enemy in pausedEnemies)
        {
            if (enemy != null)
            {
                enemy.enabled = true;
            }
        }

        pausedEnemies.Clear();
        if (conversationPlayer != null)
        {
            conversationPlayer.enabled = playerMovementWasEnabled;
        }

        conversationPlayer = null;
        activeDialogue = null;
        page = 0;
        selectedOption = -1;
    }

    private PlayerMovement FindNearbyPlayer()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player == null || !player.isActiveAndEnabled)
        {
            return null;
        }

        return Vector2.Distance(transform.position, player.transform.position) <= InteractionRadius ? player : null;
    }

    private void HandleConversationInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseConversation();
            return;
        }

        if (page == 0 && options != null)
        {
            for (var index = 0; index < Mathf.Min(options.Length, 9); index++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index)))
                {
                    SelectOption(index);
                    return;
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceConversation();
        }
    }

    private void DrawConversation()
    {
        var width = Mathf.Min(920f, Screen.width - 48f);
        var height = Mathf.Min(430f, Screen.height - 48f);
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 24f, width, height);
        GUI.Box(rect, GUIContent.none, panelStyle);

        const float padding = 24f;
        var innerWidth = width - padding * 2f;
        GUI.Label(new Rect(rect.x + padding, rect.y + 18f, innerWidth, 34f), displayName, titleStyle);

        if (page == 0)
        {
            GUI.Label(new Rect(rect.x + padding, rect.y + 58f, innerWidth, 130f), openingLine, bodyStyle);
            DrawOptions(rect, padding, innerWidth);
            return;
        }

        var body = page == 1
            ? options[selectedOption].Response
            : $"Jogador: {continuationPrompt}\n\n{displayName}: {continuationResponse}";
        GUI.Label(new Rect(rect.x + padding, rect.y + 62f, innerWidth, height - 142f), body, bodyStyle);
        var buttonLabel = page == 1 && !string.IsNullOrWhiteSpace(continuationPrompt) ? "Continuar" : "Encerrar";
        if (GUI.Button(new Rect(rect.x + width - 184f, rect.y + height - 60f, 152f, 38f), buttonLabel, buttonStyle))
        {
            AdvanceConversation();
        }
    }

    private void DrawOptions(Rect panel, float padding, float innerWidth)
    {
        if (options == null)
        {
            return;
        }

        var y = panel.y + 194f;
        for (var index = 0; index < options.Length; index++)
        {
            var label = $"{index + 1}. {options[index].Label}";
            if (GUI.Button(new Rect(panel.x + padding, y, innerWidth, 54f), label, buttonStyle))
            {
                SelectOption(index);
            }
            y += 62f;
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        var panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(0, 0, new Color(0.035f, 0.04f, 0.055f, 0.96f));
        panelTexture.Apply();
        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture }
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.78f, 0.36f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 14, 8, 8)
        };
        promptStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }
}