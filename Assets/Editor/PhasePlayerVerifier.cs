using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class PhasePlayerVerifier
{
    private static readonly Dictionary<string, Vector2> ExpectedScenes = new()
    {
        ["Assets/Scenes/Fase1.unity"] = new(-4.5f, 56.5f),
        ["Assets/Scenes/Fase2.unity"] = PhaseProgressionBootstrap.PhaseTwoEntryPosition,
        ["Assets/Scenes/Fase3.unity"] = PhaseProgressionBootstrap.PhaseThreeEntryPosition,
        ["Assets/Scenes/Fase4.unity"] = PhaseProgressionBootstrap.PhaseFourEntryPosition
    };

    [MenuItem("Tools/Top Down/Verify Phase Players")]
    public static void Verify()
    {
        foreach (var pair in ExpectedScenes)
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

            var players = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerMovement>(true))
                .Select(movement => movement.gameObject)
                .Distinct()
                .ToArray();
            if (players.Length != 1)
            {
                throw new InvalidOperationException($"{pair.Key} must contain exactly one player. Actual={players.Length}.");
            }

            var player = players[0];
            var body = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<Collider2D>();
            var health = player.GetComponent<PlayerHealth>();
            var animator = player.GetComponent<Animator>();
            if (player.name != "Player_Swordsman" || !player.CompareTag("Player") || body == null || collider == null || health == null || animator == null)
            {
                throw new InvalidOperationException($"Player is not fully configured in {pair.Key}.");
            }

            if (body.bodyType != RigidbodyType2D.Dynamic || body.gravityScale != 0f || collider.isTrigger)
            {
                throw new InvalidOperationException($"Player physics is invalid in {pair.Key}.");
            }

            if (!ground.HasTile(ground.WorldToCell(player.transform.position)))
            {
                throw new InvalidOperationException($"Player is not on floor in {pair.Key}: {(Vector2)player.transform.position}.");
            }

            var nearestBlockingTile = Physics2D.OverlapCircleAll(player.transform.position, 0.35f)
                .Any(hit => hit.GetComponent<TilemapCollider2D>() != null || hit.GetComponent<InstantDeathZone2D>() != null);
            if (nearestBlockingTile)
            {
                throw new InvalidOperationException($"Player overlaps blocking or lethal geometry in {pair.Key}.");
            }

            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"))
                ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var follow = camera != null ? camera.GetComponent<CameraFollow2D>() : null;
            if (camera == null || follow == null)
            {
                throw new InvalidOperationException($"Main camera is not configured to follow the player in {pair.Key}.");
            }

            Debug.Log($"Player verified: scene={scene.name}, name={player.name}, position={(Vector2)player.transform.position}, requestedSpawn={pair.Value}.");
        }

        Debug.Log("Phase player verification passed: Player_Swordsman is present and ready in Fase1-Fase4.");
    }
}
