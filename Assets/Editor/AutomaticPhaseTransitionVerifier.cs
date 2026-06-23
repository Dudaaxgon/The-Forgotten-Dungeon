using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutomaticPhaseTransitionVerifier
{
    private const string PhaseOnePath = "Assets/Scenes/Fase1.unity";
    private static readonly Vector2 ExpectedPhaseOneSpawn = new(-4.5f, 56.5f);

    private static int phase;
    private static float phaseStarted;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    [MenuItem("Tools/Top Down/Verify Automatic Transitions Disabled")]
    public static void VerifyStructure()
    {
        if (PhaseProgressionBootstrap.AutomaticTransitionsEnabled)
        {
            throw new InvalidOperationException("Automatic phase transitions must remain disabled.");
        }

        foreach (var sceneName in new[] { "Fase1", "Fase2", "Fase3" })
        {
            var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);
            var transitions = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTransition2D>(true))
                .ToArray();
            if (transitions.Length != 0)
            {
                throw new InvalidOperationException($"{sceneName} still contains {transitions.Length} automatic transition trigger(s).");
            }
        }

        var phaseOne = EditorSceneManager.OpenScene(PhaseOnePath, OpenSceneMode.Single);
        var players = phaseOne.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerMovement>(true))
            .ToArray();
        if (players.Length != 1)
        {
            throw new InvalidOperationException($"Fase1 must contain exactly one player, found {players.Length}.");
        }

        var playerPosition = (Vector2)players[0].transform.position;
        if (Vector2.Distance(playerPosition, ExpectedPhaseOneSpawn) > 0.01f)
        {
            throw new InvalidOperationException($"Fase1 player spawn changed. Expected={ExpectedPhaseOneSpawn}, actual={playerPosition}.");
        }

        Debug.Log($"Automatic transition structure verification passed. Fase1 spawn={playerPosition}; Fase1-Fase3 contain no transition triggers.");
    }

    [MenuItem("Tools/Top Down/Play Test Automatic Transitions Disabled")]
    public static void RunPlayMode()
    {
        VerifyStructure();
        EditorSceneManager.OpenScene(PhaseOnePath, OpenSceneMode.Single);
        previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        phase = 0;
        phaseStarted = Time.realtimeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup - phaseStarted < 0.35f)
            {
                return;
            }

            if (phase == 0)
            {
                VerifyRuntimeScene("Fase1", "Fase1ExitToFase2");
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
                if (player == null || Vector2.Distance(player.transform.position, ExpectedPhaseOneSpawn) > 0.01f)
                {
                    throw new InvalidOperationException("Player did not remain at the Fase1 initial spawn.");
                }
                LoadNext("Fase2", 1);
                return;
            }

            if (phase == 1)
            {
                VerifyRuntimeScene("Fase2", "Fase2ExitToFase3");
                LoadNext("Fase3", 2);
                return;
            }

            VerifyRuntimeScene("Fase3", "Fase3ExitToFase4");
            Finish(true, "Automatic transition play test passed: no runtime exits were created in Fase1-Fase3 and the Fase1 player remained at its initial spawn.");
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void VerifyRuntimeScene(string expectedScene, string forbiddenExit)
    {
        if (SceneManager.GetActiveScene().name != expectedScene)
        {
            throw new InvalidOperationException($"Expected active scene {expectedScene}, found {SceneManager.GetActiveScene().name}.");
        }
        if (GameObject.Find(forbiddenExit) != null || UnityEngine.Object.FindAnyObjectByType<SceneTransition2D>() != null)
        {
            throw new InvalidOperationException($"Automatic transition was created in {expectedScene}.");
        }
    }

    private static void LoadNext(string sceneName, int nextPhase)
    {
        SceneManager.LoadScene(sceneName);
        phase = nextPhase;
        phaseStarted = Time.realtimeSinceStartup;
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Debug.Log(success ? message : $"Automatic transition play test failed: {message}");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.Exit(success ? 0 : 1);
        };
    }
}
