using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class NpcLoreVerifier
{
    private static readonly Dictionary<string, string[]> ExpectedNpcs = new()
    {
        ["Assets/Scenes/Fase1.unity"] = new[] { "NPC_MERCHANT_01" },
        ["Assets/Scenes/Fase2.unity"] = new[] { "NPC_PRISONER_02", "NPC_GUARD_03" },
        ["Assets/Scenes/Fase3.unity"] = new[] { "NPC_HISTORIAN_04" },
        ["Assets/Scenes/Fase4.unity"] = new[] { "NPC_SPIRIT_05" }
    };

    private static int playPhase;
    private static float phaseStarted;
    private static NpcDialogue2D dialogue;
    private static PlayerMovement player;
    private static EnemyAI2D[] enemies;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    [MenuItem("Tools/Top Down/Verify Narrative NPCs")]
    public static void VerifyStructure()
    {
        foreach (var pair in ExpectedNpcs)
        {
            var scene = EditorSceneManager.OpenScene(pair.Key, OpenSceneMode.Single);
            Physics2D.SyncTransforms();
            var ground = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(tilemap => tilemap.name == "Chao");
            if (ground == null)
            {
                throw new InvalidOperationException($"Chao tilemap missing in {pair.Key}.");
            }

            var dialogues = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<NpcDialogue2D>(true))
                .ToArray();
            var actualIds = dialogues.Select(candidate => candidate.NpcId).OrderBy(id => id).ToArray();
            var expectedIds = pair.Value.OrderBy(id => id).ToArray();
            if (!actualIds.SequenceEqual(expectedIds))
            {
                throw new InvalidOperationException($"NPC mismatch in {pair.Key}. Expected={string.Join(",", expectedIds)}; actual={string.Join(",", actualIds)}.");
            }

            foreach (var candidate in dialogues)
            {
                var body = candidate.GetComponent<Rigidbody2D>();
                var collider = candidate.GetComponent<Collider2D>();
                var animator = candidate.GetComponent<Animator>();
                if (body == null || body.bodyType != RigidbodyType2D.Static || collider == null || collider.isTrigger || animator == null)
                {
                    throw new InvalidOperationException($"NPC is not physically or visually ready: {candidate.NpcId}.");
                }
                if (candidate.GetComponent<EnemyAI2D>() != null || candidate.GetComponent<TopDownCharacterPhysics>() != null)
                {
                    throw new InvalidOperationException($"NPC still contains hostile or movable behavior: {candidate.NpcId}.");
                }
                if (candidate.OptionCount < 2 || !ground.HasTile(ground.WorldToCell(candidate.transform.position)))
                {
                    throw new InvalidOperationException($"NPC dialogue or floor placement is invalid: {candidate.NpcId}.");
                }

                var nearestEnemy = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EnemyAI2D>(true))
                    .Select(enemy => Vector2.Distance(enemy.transform.position, candidate.transform.position))
                    .DefaultIfEmpty(float.PositiveInfinity)
                    .Min();
                if (nearestEnemy < 4f)
                {
                    throw new InvalidOperationException($"NPC is too close to an enemy: {candidate.NpcId}, distance={nearestEnemy:0.00}.");
                }

                Debug.Log($"NPC verified: scene={scene.name}, id={candidate.NpcId}, position={(Vector2)candidate.transform.position}, options={candidate.OptionCount}, nearestEnemy={nearestEnemy:0.00}.");
            }
        }

        Debug.Log("Narrative NPC structure verification passed: five NPCs, four scenes, dialogue data, animation and safe placement are valid.");
    }

    [MenuItem("Tools/Top Down/Play Test Narrative NPC")]
    public static void RunPlayMode()
    {
        VerifyStructure();
        EditorSceneManager.OpenScene("Assets/Scenes/Fase1.unity", OpenSceneMode.Single);
        previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        playPhase = 0;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Top Down/Capture Narrative NPC Previews")]
    public static void CapturePreviews()
    {
        var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/npc-lore-previews"));
        Directory.CreateDirectory(outputDirectory);

        foreach (var pair in ExpectedNpcs)
        {
            var scene = EditorSceneManager.OpenScene(pair.Key, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault();
            if (camera == null)
            {
                throw new InvalidOperationException($"Camera missing in {pair.Key}.");
            }

            camera.orthographic = true;
            camera.orthographicSize = 6f;
            foreach (var candidate in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<NpcDialogue2D>(true)))
            {
                camera.transform.position = new Vector3(candidate.transform.position.x, candidate.transform.position.y, -10f);
                var renderTexture = new RenderTexture(800, 600, 24);
                var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                var path = Path.Combine(outputDirectory, $"{scene.name}_{candidate.NpcId}.png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                Debug.Log($"NPC preview captured: {path}");
            }
        }

        Debug.Log($"Narrative NPC previews captured in {outputDirectory}.");
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (playPhase == 0)
            {
                SetupConversation();
            }
            else if (playPhase == 1 && Time.realtimeSinceStartup - phaseStarted > 0.25f)
            {
                VerifyOpenConversation();
            }
            else if (playPhase == 2 && Time.realtimeSinceStartup - phaseStarted > 0.25f)
            {
                VerifyClosedConversation();
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void SetupConversation()
    {
        dialogue = UnityEngine.Object.FindFirstObjectByType<NpcDialogue2D>();
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        enemies = UnityEngine.Object.FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None);
        if (dialogue == null || player == null || enemies.Length != 19)
        {
            throw new InvalidOperationException("Play test prerequisites are missing in Fase1.");
        }

        player.transform.position = dialogue.transform.position + Vector3.right;
        Physics2D.SyncTransforms();
        if (!dialogue.BeginConversation(player))
        {
            throw new InvalidOperationException("NPC conversation did not open.");
        }

        phaseStarted = Time.realtimeSinceStartup;
        playPhase = 1;
    }

    private static void VerifyOpenConversation()
    {
        if (!dialogue.IsConversationOpen || player.enabled || enemies.Any(enemy => enemy.enabled))
        {
            throw new InvalidOperationException("Conversation did not pause player movement and enemies.");
        }

        dialogue.SelectOption(0);
        dialogue.AdvanceConversation();
        dialogue.AdvanceConversation();
        phaseStarted = Time.realtimeSinceStartup;
        playPhase = 2;
    }

    private static void VerifyClosedConversation()
    {
        if (dialogue.IsConversationOpen || !player.enabled || enemies.Any(enemy => !enemy.enabled))
        {
            throw new InvalidOperationException("Conversation did not restore player movement and enemies.");
        }

        Finish(true, "Narrative NPC play test passed: interaction opens, choices advance, combat pauses, and gameplay resumes.");
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Debug.Log(success ? message : $"Narrative NPC play test failed: {message}");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.Exit(success ? 0 : 1);
        };
    }
}
