using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Fase1GameplayVerifier
{
    private const string ScenePath = "Assets/Scenes/Fase1.unity";
    private static int phase;
    private static float phaseStarted;
    private static PlayerHealth player;
    private static EnemyAI2D enemy;
    private static Rigidbody2D enemyBody;
    private static int healthBeforeAttack;
    private static Vector2 chaseStart;
    private static Vector2 blockedStart;
    private static GameObject wall;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    public static void VerifyStructure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var playerObject = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerHealth>(true))
            .SingleOrDefault();
        if (playerObject == null || playerObject.GetComponent<PlayerMovement>() == null || !playerObject.CompareTag("Player"))
        {
            throw new InvalidOperationException("Fase1 must contain one configured Player.");
        }

        var enemies = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EnemyAI2D>(true))
            .ToArray();
        if (enemies.Length != 19)
        {
            throw new InvalidOperationException($"Expected 19 enemies, found {enemies.Length}.");
        }

        foreach (var candidate in enemies)
        {
            var body = candidate.GetComponent<Rigidbody2D>();
            var collider = candidate.GetComponent<Collider2D>();
            var animator = candidate.GetComponent<Animator>();
            if (body == null || collider == null || animator == null || body.gravityScale != 0f || collider.isTrigger)
            {
                throw new InvalidOperationException($"Enemy is not fully configured: {candidate.name}.");
            }

            if (candidate.DetectionRadius <= 0f || candidate.LeashRadius < candidate.DetectionRadius || candidate.AttackDamage <= 0)
            {
                throw new InvalidOperationException($"Enemy has invalid combat settings: {candidate.name}.");
            }

            Debug.Log($"Fase1 enemy verified: {candidate.name} at {(Vector2)candidate.transform.position}.");
        }

        var camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CameraFollow2D>(true))
            .SingleOrDefault();
        if (camera == null)
        {
            throw new InvalidOperationException("Fase1 camera follow is missing.");
        }

        if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != ScenePath || !EditorBuildSettings.scenes[0].enabled)
        {
            throw new InvalidOperationException("Fase1 is not the first enabled build scene.");
        }

        Debug.Log("Fase1 structure verification passed: player, camera, 19 enemies, physics and combat settings are valid.");
    }

    public static void RunPlayMode()
    {
        VerifyStructure();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        phase = 0;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            switch (phase)
            {
                case 0:
                    SetupChaseTest();
                    break;
                case 1:
                    VerifyChaseAndSetupAttack();
                    break;
                case 2:
                    VerifyAttackAndSetupLineOfSight();
                    break;
                case 3:
                    VerifyLineOfSightAndFinish();
                    break;
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void SetupChaseTest()
    {
        player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None);
        enemy = enemies.First();
        foreach (var candidate in enemies)
        {
            if (candidate != enemy)
            {
                candidate.gameObject.SetActive(false);
            }
        }

        enemyBody = enemy.GetComponent<Rigidbody2D>();
        enemyBody.position = new Vector2(100f, 100f);
        enemy.transform.position = enemyBody.position;
        enemy.SetGuardPosition(enemyBody.position);
        player.transform.position = new Vector2(103f, 100f);
        chaseStart = enemyBody.position;
        phaseStarted = Time.realtimeSinceStartup;
        phase = 1;
        Debug.Log("Fase1 play test: chase phase started.");
    }

    private static void VerifyChaseAndSetupAttack()
    {
        if (Time.realtimeSinceStartup - phaseStarted < 1.25f)
        {
            return;
        }

        if (enemyBody.position.x <= chaseStart.x + 0.5f)
        {
            throw new InvalidOperationException($"Enemy did not chase the player. Start={chaseStart}, current={enemyBody.position}.");
        }

        healthBeforeAttack = player.CurrentHealth;
        player.transform.position = enemyBody.position + Vector2.right * 0.55f;
        phaseStarted = Time.realtimeSinceStartup;
        phase = 2;
        Debug.Log($"Fase1 play test: chase passed at {enemyBody.position}; attack phase started.");
    }

    private static void VerifyAttackAndSetupLineOfSight()
    {
        if (Time.realtimeSinceStartup - phaseStarted < 1.6f)
        {
            return;
        }

        if (player.CurrentHealth >= healthBeforeAttack)
        {
            throw new InvalidOperationException($"Enemy attack did not damage the player. Health={player.CurrentHealth}.");
        }

        enemyBody.position = new Vector2(100f, 100f);
        enemy.transform.position = enemyBody.position;
        enemy.SetGuardPosition(enemyBody.position);
        player.transform.position = new Vector2(103f, 100f);
        wall = new GameObject("LineOfSightTestWall");
        wall.transform.position = new Vector2(101.5f, 100f);
        var wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(0.4f, 3f);
        Physics2D.SyncTransforms();
        blockedStart = enemyBody.position;
        phaseStarted = Time.realtimeSinceStartup;
        phase = 3;
        Debug.Log($"Fase1 play test: attack passed, player health {healthBeforeAttack}->{player.CurrentHealth}; line-of-sight phase started.");
    }

    private static void VerifyLineOfSightAndFinish()
    {
        if (Time.realtimeSinceStartup - phaseStarted < 1.2f)
        {
            return;
        }

        if (Vector2.Distance(enemyBody.position, blockedStart) > 0.15f)
        {
            throw new InvalidOperationException($"Enemy ignored the blocking wall. Start={blockedStart}, current={enemyBody.position}.");
        }

        Finish(true, "Fase1 play test passed: chase, attack damage and wall line-of-sight blocking are functional.");
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Debug.Log(success ? message : $"Fase1 play test failed: {message}");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.Exit(success ? 0 : 1);
        };
    }
}
