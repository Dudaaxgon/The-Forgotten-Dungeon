using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class NpcLoreBuilder
{
    private const string NpcRootName = "Narrative_NPCs";

    private sealed class NpcSpec
    {
        public string ScenePath;
        public string Id;
        public string DisplayName;
        public string PrefabPath;
        public Vector2 Position;
        public string Opening;
        public NpcDialogue2D.DialogueOption[] Options;
        public string ContinuationPrompt;
        public string ContinuationResponse;
    }

    private static readonly NpcSpec[] Npcs =
    {
        new()
        {
            ScenePath = "Assets/Scenes/Fase1.unity",
            Id = "NPC_MERCHANT_01",
            DisplayName = "Mercador",
            PrefabPath = "Assets/Characters/CraftPixRuinedTemple/Prepared/Leader/Prefabs/Leader.prefab",
            Position = new Vector2(-4f, 57f),
            Opening = "Ah... voc\u00ea finalmente se levantou. Sem nossas lembran\u00e7as, somos apenas cascas vazias tateando no escuro. Mas respire fundo. Voc\u00ea n\u00e3o \u00e9 o primeiro a acordar sobre estas pedras frias sem saber o pr\u00f3prio nome.",
            Options = new[]
            {
                Option("Quem \u00e9 voc\u00ea e o que quer de mim?", "O que eu quero? Apenas ajudar um viajante desorientado. Sou um simples negociante de oportunidades. Encontro coisas que as pessoas deixam para tr\u00e1s e as repasso a quem precisa, por um pre\u00e7o justo."),
                Option("Onde n\u00f3s estamos?", "Em uma cova de ferro e pedra que o tempo esqueceu. Alguns chamavam este lugar de pris\u00e3o; outros, de cofre. O que importa \u00e9 que a porta adiante est\u00e1 trancada."),
                Option("(Sil\u00eancio)", "Cauteloso. A verdade \u00e9 uma moeda rara por aqui. Guarde sua desconfian\u00e7a para o que rasteja nas sombras. De mim, voc\u00ea s\u00f3 precisa comprar.")
            },
            ContinuationPrompt = "Voc\u00ea encontrou algo sobre quem eu sou?",
            ContinuationResponse = "Em meus registros n\u00e3o h\u00e1 um nome, mas h\u00e1 um rastro que come\u00e7ou muito antes de voc\u00ea acordar."
        },
        new()
        {
            ScenePath = "Assets/Scenes/Fase2.unity",
            Id = "NPC_PRISONER_02",
            DisplayName = "Prisioneira",
            PrefabPath = "Assets/Characters/CraftPixRuinedTemple/Prepared/Cultist3/Prefabs/Cultist3.prefab",
            Position = new Vector2(-39f, 12f),
            Opening = "Passos que n\u00e3o arrastam correntes... Voc\u00ea \u00e9 real? Esse olhar vazio. Voc\u00ea tamb\u00e9m n\u00e3o sabe quem \u00e9, n\u00e3o \u00e9? Deixou-se enganar pelo homem das moedas na Pris\u00e3o?",
            Options = new[]
            {
                Option("O Mercador? Ele me ajudou a sair de l\u00e1.", "Ajudou? O pre\u00e7o da ajuda dele \u00e9 a sua ru\u00edna. N\u00e3o existem portas trancadas para fora daqui, apenas portas trancadas para as entranhas deste lugar. Ele quer que voc\u00ea des\u00e7a."),
                Option("Por que eu deveria acreditar em voc\u00ea e n\u00e3o nele?", "Voc\u00ea n\u00e3o deveria. Mas pergunte a si mesmo o que um mercador ganha vivendo entre os esquecidos. Ele apontou a dire\u00e7\u00e3o do abismo, n\u00e3o da superf\u00edcie.")
            },
            ContinuationPrompt = "Se o Mercador \u00e9 t\u00e3o perigoso, por que voc\u00ea n\u00e3o foge?",
            ContinuationResponse = "As catacumbas s\u00e3o um labirinto de arrependimentos. Pelo menos aqui eu sei exatamente quem est\u00e1 tentando me destruir."
        },
        new()
        {
            ScenePath = "Assets/Scenes/Fase2.unity",
            Id = "NPC_GUARD_03",
            DisplayName = "Guarda Ferido",
            PrefabPath = "Assets/Characters/AutoImported/Swordsman_lvl4/Prefabs/Swordsman_lvl4.prefab",
            Position = new Vector2(59f, -23f),
            Opening = "Alto l\u00e1... Um passo a mais e eu corto voc\u00ea ao meio. Espere. Voc\u00ea n\u00e3o tem os olhos daquelas criaturas, mas tamb\u00e9m n\u00e3o carrega o emblema da guarda. Quem \u00e9 voc\u00ea?",
            Options = new[]
            {
                Option("N\u00e3o sei quem sou. Estou tentando encontrar uma sa\u00edda.", "Sair... Voc\u00ea caminha sobre o t\u00famulo de centenas de irm\u00e3os meus. Demos nosso sangue para que os selos abaixo n\u00e3o fossem rompidos. N\u00e3o h\u00e1 salva\u00e7\u00e3o no escuro, apenas o eco das suas escolhas."),
                Option("Que maldi\u00e7\u00e3o \u00e9 essa?", "O artefato \u00e9 a raiz desta pris\u00e3o. Fomos forjados no juramento de proteg\u00ea-lo, mas a ambi\u00e7\u00e3o apodrece at\u00e9 a armadura mais reluzente. Agora a masmorra cobra o pre\u00e7o desse pecado antigo.")
            },
            ContinuationPrompt = "Se eu encontrar o artefato, o que devo fazer?",
            ContinuationResponse = "Pergunte-se o que \u00e9 capaz de perder. Se quer ser her\u00f3i, destrua-o. Se quer ser mestre, carregue o fardo. Mestres acabam se tornando monstros."
        },
        new()
        {
            ScenePath = "Assets/Scenes/Fase3.unity",
            Id = "NPC_HISTORIAN_04",
            DisplayName = "Historiador",
            PrefabPath = "Assets/Characters/CraftPixRuinedTemple/Prepared/Cultist6/Prefabs/Cultist6.prefab",
            Position = new Vector2(25f, -22f),
            Opening = "A sequ\u00eancia n\u00e3o \u00e9 matem\u00e1tica, \u00e9 cronol\u00f3gica! A queda do rei, a trai\u00e7\u00e3o da guarda... Uma folha em branco caminhando pelas cinzas da hist\u00f3ria. Veio desvendar o mecanismo do Sal\u00e3o?",
            Options = new[]
            {
                Option("Voc\u00ea sabe como as alavancas funcionam?", "O mecanismo exige a narrativa correta. A ordem reflete a ru\u00edna deste lugar: primeiro a ilus\u00e3o, depois o sangue e, por fim, o esquecimento."),
                Option("O que as inscri\u00e7\u00f5es dizem sobre este lugar?", "O artefato n\u00e3o foi feito para ser guardado, mas contido. Para abrir os port\u00f5es, acione as alavancas na ordem da corrup\u00e7\u00e3o. O Mercador, o Guarda... cada s\u00edmbolo tem seu lugar.")
            },
            ContinuationPrompt = "Existe uma forma de purificar este lugar?",
            ContinuationResponse = "A hist\u00f3ria n\u00e3o se purifica, ela se repete. N\u00e3o busque purifica\u00e7\u00e3o; busque entender o seu papel no ciclo."
        },
        new()
        {
            ScenePath = "Assets/Scenes/Fase4.unity",
            Id = "NPC_SPIRIT_05",
            DisplayName = "Esp\u00edrito",
            PrefabPath = "Assets/Characters/AutoImported/Ghost1/Prefabs/Ghost1.prefab",
            Position = new Vector2(2f, 7f),
            Opening = "Voc\u00ea finalmente chegou. Est\u00e1 diante da origem de toda a dor deste lugar. O artefato pode ser destru\u00eddo, selado novamente ou reclamado por voc\u00ea. O peso de uma escolha aqui \u00e9 eterno.",
            Options = new[]
            {
                Option("Eu escolho destruir o artefato.", "Voc\u00ea escolheu o vazio. A masmorra finalmente poder\u00e1 cair em esquecimento."),
                Option("Eu vou sel\u00e1-lo novamente.", "A ordem ser\u00e1 mantida, mas ao custo de um eterno retorno. A hist\u00f3ria se repetir\u00e1."),
                Option("Esse poder deveria ser meu.", "O poder \u00e9 seu, mas a escurid\u00e3o da masmorra agora habita em voc\u00ea.")
            },
            ContinuationPrompt = "E se eu escolher n\u00e3o fazer nada e apenas for embora?",
            ContinuationResponse = "A masmorra n\u00e3o conhece o ir embora. A in\u00e9rcia tamb\u00e9m \u00e9 uma decis\u00e3o, e o artefato n\u00e3o perdoa quem se recusa a escolher."
        }
    };

    [MenuItem("Tools/Top Down/Build Narrative NPCs")]
    public static void Build()
    {
        foreach (var sceneGroup in Npcs.GroupBy(npc => npc.ScenePath))
        {
            BuildScene(sceneGroup.Key, sceneGroup.ToArray());
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Narrative NPC build completed. NPCs={Npcs.Length}, scenes={Npcs.Select(npc => npc.ScenePath).Distinct().Count()}.");
    }

    private static void BuildScene(string scenePath, NpcSpec[] specs)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        TopDownSceneCollisionBootstrap.ConfigureSceneColliders(scene);
        foreach (var tilemapCollider in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>(true)))
        {
            tilemapCollider.ProcessTilemapChanges();
        }

        Physics2D.SyncTransforms();
        var ground = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(tilemap => tilemap.name == "Chao");
        if (ground == null)
        {
            throw new InvalidOperationException($"Chao tilemap not found in {scenePath}.");
        }

        var existingRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == NpcRootName);
        if (existingRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }

        var root = new GameObject(NpcRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        foreach (var spec in specs)
        {
            CreateNpc(spec, ground, root.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException($"Could not save {scenePath}.");
        }

        Debug.Log($"Narrative NPCs saved in {scenePath}: {string.Join(", ", specs.Select(spec => spec.Id))}.");
    }

    private static void CreateNpc(NpcSpec spec, Tilemap ground, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"NPC source prefab not found: {spec.PrefabPath}");
        }

        var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        npc.name = $"{spec.Id}_{spec.DisplayName.Replace(" ", "_")}";
        npc.transform.SetParent(parent);
        npc.transform.position = FindSafePosition(ground, spec.Position);

        foreach (var ai in npc.GetComponents<EnemyAI2D>())
        {
            UnityEngine.Object.DestroyImmediate(ai);
        }
        foreach (var physics in npc.GetComponents<TopDownCharacterPhysics>())
        {
            UnityEngine.Object.DestroyImmediate(physics);
        }

        var body = npc.GetComponent<Rigidbody2D>() ?? npc.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        var collider = npc.GetComponent<Collider2D>() ?? npc.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = false;
        foreach (var renderer in npc.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = 25;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        var dialogue = npc.GetComponent<NpcDialogue2D>() ?? npc.AddComponent<NpcDialogue2D>();
        dialogue.Configure(spec.Id, spec.DisplayName, spec.Opening, spec.Options, spec.ContinuationPrompt, spec.ContinuationResponse);
        Physics2D.SyncTransforms();
        Debug.Log($"NPC placed: {spec.Id} at {(Vector2)npc.transform.position} using {spec.PrefabPath}.");
    }

    private static Vector3 FindSafePosition(Tilemap ground, Vector2 desired)
    {
        var center = ground.WorldToCell(desired);
        for (var radius = 0; radius <= 12; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                    {
                        continue;
                    }

                    var cell = center + new Vector3Int(x, y, 0);
                    if (!ground.HasTile(cell))
                    {
                        continue;
                    }

                    var world = ground.GetCellCenterWorld(cell);
                    var blockingCollider = Physics2D.OverlapCircleAll(world, 0.45f).Any(IsBlockingCollider);
                    var enemyTooClose = UnityEngine.Object.FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None)
                        .Any(enemy => Vector2.Distance(world, enemy.transform.position) < 4f);
                    if (!blockingCollider && !enemyTooClose)
                    {
                        return new Vector3(world.x, world.y, 0f);
                    }
                }
            }
        }

        throw new InvalidOperationException($"No safe floor position found near {desired}.");
    }

    private static bool IsBlockingCollider(Collider2D collider)
    {
        return collider.GetComponent<TilemapCollider2D>() != null
            || collider.GetComponent<InstantDeathZone2D>() != null
            || collider.GetComponent<EnemyAI2D>() != null
            || collider.GetComponent<NpcDialogue2D>() != null;
    }

    private static NpcDialogue2D.DialogueOption Option(string label, string response)
    {
        return new NpcDialogue2D.DialogueOption(label, response);
    }
}
