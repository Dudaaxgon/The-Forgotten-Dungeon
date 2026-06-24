using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class WallCollisionVerifier
{
    private static readonly string[] SceneNames = { "Fase1", "Fase2", "Fase3", "Fase4" };
    private static readonly string[] WallNameParts = { "parede", "pilastra", "janela", "acabamento" };
    private static readonly Vector2 PlayerColliderSize = new(0.55f, 0.8f);

    private static int sceneIndex;
    private static float sceneLoadedAt;
    private static bool previousEnterPlayModeOptionsEnabled;
    private static EnterPlayModeOptions previousEnterPlayModeOptions;

    [MenuItem("Tools/Top Down/Play Test Wall Collisions")]
    public static void RunPlayMode()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Fase1.unity", OpenSceneMode.Single);
        previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        sceneIndex = 0;
        sceneLoadedAt = Time.realtimeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup - sceneLoadedAt < 0.75f)
            {
                return;
            }

            var expectedScene = SceneNames[sceneIndex];
            if (SceneManager.GetActiveScene().name != expectedScene)
            {
                return;
            }

            VerifyLoadedScene(SceneManager.GetActiveScene());
            sceneIndex++;
            if (sceneIndex >= SceneNames.Length)
            {
                Finish(true, "Wall collision play test passed in Fase1-Fase4.");
                return;
            }

            SceneManager.LoadScene(SceneNames[sceneIndex]);
            sceneLoadedAt = Time.realtimeSinceStartup;
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void VerifyLoadedScene(Scene scene)
    {
        Physics2D.SyncTransforms();
        var walls = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Where(tilemap => IsWall(tilemap.name))
            .ToArray();
        if (walls.Length == 0)
        {
            throw new InvalidOperationException($"No wall tilemaps were found in {scene.name}.");
        }

        foreach (var wall in walls)
        {
            var tilemapCollider = wall.GetComponent<TilemapCollider2D>();
            var composite = wall.GetComponent<CompositeCollider2D>();
            var body = wall.GetComponent<Rigidbody2D>();
            if (tilemapCollider == null || !tilemapCollider.enabled || tilemapCollider.isTrigger)
            {
                throw new InvalidOperationException($"{scene.name}/{wall.name} lost its solid TilemapCollider2D.");
            }
            if (composite == null || !composite.enabled || composite.isTrigger || composite.shapeCount == 0)
            {
                throw new InvalidOperationException($"{scene.name}/{wall.name} has no blocking composite geometry.");
            }
            if (body == null || !body.simulated || body.bodyType != RigidbodyType2D.Static)
            {
                throw new InvalidOperationException($"{scene.name}/{wall.name} has no simulated static Rigidbody2D.");
            }
            if (!CapsuleCastHits(composite))
            {
                throw new InvalidOperationException($"A player-sized CapsuleCast did not hit {scene.name}/{wall.name}.");
            }

            Debug.Log($"Wall collision verified: scene={scene.name}, tilemap={wall.name}, shapes={composite.shapeCount}, bounds={composite.bounds}.");
        }

        if (scene.name == "Fase1")
        {
            VerifyPhaseOneDoorways(scene, walls);
        }
    }

    private static void VerifyPhaseOneDoorways(Scene scene, Tilemap[] walls)
    {
        var tilemaps = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .ToArray();
        var doors = tilemaps.Where(tilemap => Normalize(tilemap.name).Contains("porta")).ToArray();
        if (doors.Length == 0)
        {
            throw new InvalidOperationException("Fase1 has no doorway tilemaps to validate.");
        }

        foreach (var door in doors)
        {
            if (door.GetComponents<Collider2D>().Length != 0 || door.GetComponent<Rigidbody2D>() != null)
            {
                throw new InvalidOperationException($"Fase1/{door.name} should remain passable.");
            }

            foreach (var cell in door.cellBounds.allPositionsWithin)
            {
                if (door.HasTile(cell) && walls.Any(wall => wall.HasTile(cell)))
                {
                    throw new InvalidOperationException($"A wall tile still overlaps doorway cell {cell}.");
                }
            }
        }

        var passableDetails = tilemaps.FirstOrDefault(tilemap => tilemap.name == "PassableDoorDetails_Runtime");
        if (passableDetails == null || passableDetails.GetComponents<Collider2D>().Length != 0)
        {
            throw new InvalidOperationException("Fase1 passable doorway details are missing or collidable.");
        }

        Debug.Log($"Fase1 doorway clearance verified: doorTilemaps={doors.Length}, passableDetailsBounds={passableDetails.cellBounds}.");
    }

    private static bool CapsuleCastHits(CompositeCollider2D target)
    {
        var bounds = target.bounds;
        var horizontalDistance = bounds.size.x + 2f;
        var verticalDistance = bounds.size.y + 2f;
        for (var sample = 0; sample <= 12; sample++)
        {
            var t = sample / 12f;
            var y = Mathf.Lerp(bounds.min.y, bounds.max.y, t);
            var horizontalHits = Physics2D.CapsuleCastAll(
                new Vector2(bounds.min.x - 1f, y),
                PlayerColliderSize,
                CapsuleDirection2D.Vertical,
                0f,
                Vector2.right,
                horizontalDistance);
            if (horizontalHits.Any(hit => hit.collider == target))
            {
                return true;
            }

            var x = Mathf.Lerp(bounds.min.x, bounds.max.x, t);
            var verticalHits = Physics2D.CapsuleCastAll(
                new Vector2(x, bounds.min.y - 1f),
                PlayerColliderSize,
                CapsuleDirection2D.Vertical,
                0f,
                Vector2.up,
                verticalDistance);
            if (verticalHits.Any(hit => hit.collider == target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWall(string objectName)
    {
        return WallNameParts.Any(Normalize(objectName).Contains);
    }

    private static string Normalize(string value)
    {
        return value.Normalize(NormalizationForm.FormD).ToLowerInvariant();
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= Tick;
        Debug.Log(success ? message : $"Wall collision play test failed: {message}");
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            EditorApplication.Exit(success ? 0 : 1);
        };
    }
}
