using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatSystemVerifier
{
    private const string ScenePath = "Assets/Scenes/Fase1.unity";

    private static int phase;
    private static float phaseStarted;
    private static PlayerHealth playerHealth;
    private static PlayerCombat2D playerCombat;
    private static PlayerMovement playerMovement;
    private static Animator playerAnimator;
    private static EnemyAI2D enemyAi;
    private static EnemyHealth2D enemyHealth;
    private static Rigidbody2D enemyBody;
    private static int enemyHealthBeforeAttack;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    [MenuItem("Tools/Top Down/Play Test Combat System")]
    public static void RunPlayMode()
    {
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
                    SetupPlayerAttack();
                    break;
                case 1:
                    VerifyPlayerAttack();
                    break;
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void SetupPlayerAttack()
    {
        playerHealth = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        playerMovement = playerHealth.GetComponent<PlayerMovement>();
        playerCombat = playerHealth.GetComponent<PlayerCombat2D>() ?? playerHealth.gameObject.AddComponent<PlayerCombat2D>();
        playerAnimator = playerHealth.GetComponent<Animator>();

        var enemies = UnityEngine.Object.FindObjectsByType<EnemyAI2D>(FindObjectsSortMode.None);
        enemyAi = enemies.First();
        foreach (var candidate in enemies)
        {
            if (candidate != enemyAi)
            {
                candidate.gameObject.SetActive(false);
            }
        }

        enemyHealth = enemyAi.GetComponent<EnemyHealth2D>() ?? enemyAi.gameObject.AddComponent<EnemyHealth2D>();
        enemyBody = enemyAi.GetComponent<Rigidbody2D>();
        enemyAi.enabled = false;

        playerHealth.RestoreFullHealth();
        playerMovement.enabled = true;
        playerMovement.movement = Vector2.zero;
        playerHealth.transform.position = new Vector2(100f, 100f);
        enemyBody.position = new Vector2(100f, 99.2f);
        enemyAi.transform.position = enemyBody.position;
        Physics2D.SyncTransforms();
        VerifyPlayerEnemyCollisionIgnored();

        enemyHealthBeforeAttack = enemyHealth.CurrentHealth;
        var hit = playerCombat.TryAttack();
        if (!hit)
        {
            throw new InvalidOperationException("Player attack did not find an enemy target.");
        }

        phaseStarted = Time.realtimeSinceStartup;
        phase = 1;
        Debug.Log("Combat play test: player attack phase started.");
    }

    private static void VerifyPlayerEnemyCollisionIgnored()
    {
        var playerColliders = playerHealth.GetComponentsInChildren<Collider2D>(true);
        var enemyColliders = enemyAi.GetComponentsInChildren<Collider2D>(true);
        if (playerColliders.Length == 0 || enemyColliders.Length == 0)
        {
            throw new InvalidOperationException("Combat collision test requires player and enemy colliders.");
        }

        foreach (var playerCollider in playerColliders)
        {
            foreach (var enemyCollider in enemyColliders)
            {
                if (playerCollider != null && enemyCollider != null && !Physics2D.GetIgnoreCollision(playerCollider, enemyCollider))
                {
                    throw new InvalidOperationException("Player and enemy body colliders are still physically colliding.");
                }
            }
        }
    }

    private static void VerifyPlayerAttack()
    {
        if (Time.realtimeSinceStartup - phaseStarted < 0.1f)
        {
            return;
        }

        if (enemyHealth.CurrentHealth >= enemyHealthBeforeAttack)
        {
            throw new InvalidOperationException($"Player attack did not damage the enemy. Enemy health={enemyHealth.CurrentHealth}.");
        }

        if (playerAnimator != null
            && !playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Attack_Down")
            && !playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("WalkAttack_Down")
            && !playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("RunAttack_Down"))
        {
            throw new InvalidOperationException("Player attack visual was overwritten before the attack animation could play.");
        }

        Finish(true, $"Combat play test passed: player damaged enemy ({enemyHealthBeforeAttack}->{enemyHealth.CurrentHealth}). Enemy damage is covered by Fase1GameplayVerifier.");
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Debug.Log(success ? message : $"Combat play test failed: {message}");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.Exit(success ? 0 : 1);
        };
    }
}
