using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TopDownSceneCollisionBootstrap
{
    private const string PassableDoorDetailsName = "PassableDoorDetails_Runtime";

    private static readonly string[] SolidTilemapNameParts =
    {
        "parede",
        "pilastra",
        "janela",
        "acabamento",
        "buraco",
        "armadilha",
        "espinho",
        "spike"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureLoadedScene()
    {
        ConfigureSceneColliders(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneColliders(scene);
    }

    public static void ConfigureSceneColliders(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        ConfigurePassableDoorways(scene);

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (IsDoorway(tilemap.gameObject.name))
                {
                    RemoveCollision(tilemap.gameObject);
                    continue;
                }

                if (tilemap.GetComponent<InstantDeathZone2D>() != null)
                {
                    ConfigureTilemap(tilemap.gameObject, true);
                    continue;
                }

                if (ShouldBeSolid(tilemap.gameObject.name))
                {
                    ConfigureTilemap(tilemap.gameObject, false);
                }
            }
        }
    }

    private static bool ShouldBeSolid(string objectName)
    {
        var normalized = objectName.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        foreach (var part in SolidTilemapNameParts)
        {
            if (normalized.Contains(part))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDoorway(string objectName)
    {
        return objectName.Normalize(NormalizationForm.FormD).ToLowerInvariant().Contains("porta");
    }

    private static void ConfigurePassableDoorways(Scene scene)
    {
        if (scene.name != PhaseProgressionBootstrap.PhaseOneScene)
        {
            return;
        }

        Tilemap destination = null;
        var movedTiles = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var tilemaps = root.GetComponentsInChildren<Tilemap>(true);
            foreach (var door in tilemaps)
            {
                if (!IsDoorway(door.name))
                {
                    continue;
                }

                foreach (var wall in tilemaps)
                {
                    if (!wall.name.Normalize(NormalizationForm.FormD).ToLowerInvariant().Contains("parede"))
                    {
                        continue;
                    }

                    foreach (var cell in door.cellBounds.allPositionsWithin)
                    {
                        if (!door.HasTile(cell) || !wall.HasTile(cell))
                        {
                            continue;
                        }

                        if (MoveTileToPassable(wall, cell, ref destination))
                        {
                            movedTiles++;
                        }
                    }

                }
            }

            foreach (var wall in tilemaps)
            {
                if (!wall.name.Normalize(NormalizationForm.FormD).ToLowerInvariant().Contains("parede"))
                {
                    continue;
                }

                var entranceClearance = new BoundsInt(10, 43, 0, 9, 10, 1);
                foreach (var cell in entranceClearance.allPositionsWithin)
                {
                    if (MoveTileToPassable(wall, cell, ref destination))
                    {
                        movedTiles++;
                    }
                }

                var wallCollider = wall.GetComponent<TilemapCollider2D>();
                if (wallCollider != null && wallCollider.hasTilemapChanges)
                {
                    wallCollider.ProcessTilemapChanges();
                }
            }

            foreach (var ground in tilemaps)
            {
                if (ground.name != "Chao")
                {
                    continue;
                }

                for (var x = 13; x <= 15; x++)
                {
                    var targetCell = new Vector3Int(x, 50, 0);
                    if (ground.HasTile(targetCell))
                    {
                        continue;
                    }
                    var sourceCell = new Vector3Int(x, 49, 0);
                    if (!ground.HasTile(sourceCell))
                    {
                        sourceCell = new Vector3Int(x, 51, 0);
                    }
                    var tile = ground.GetTile(sourceCell);
                    if (tile != null)
                    {
                        ground.SetTile(targetCell, tile);
                    }
                }
            }
        }

        if (movedTiles > 0)
        {
            Debug.Log($"Configured {movedTiles} passable doorway detail tiles in {scene.name}.");
        }
    }

    private static bool MoveTileToPassable(Tilemap source, Vector3Int cell, ref Tilemap destination)
    {
        if (!source.HasTile(cell))
        {
            return false;
        }

        destination ??= GetOrCreatePassableTilemap(source);
        var tile = source.GetTile(cell);
        var matrix = source.GetTransformMatrix(cell);
        var color = source.GetColor(cell);
        var flags = source.GetTileFlags(cell);
        destination.SetTile(cell, tile);
        destination.SetTileFlags(cell, TileFlags.None);
        destination.SetTransformMatrix(cell, matrix);
        destination.SetColor(cell, color);
        destination.SetTileFlags(cell, flags);
        source.SetTile(cell, null);
        return true;
    }

    private static Tilemap GetOrCreatePassableTilemap(Tilemap source)
    {
        var existing = source.transform.parent.Find(PassableDoorDetailsName);
        if (existing != null)
        {
            return existing.GetComponent<Tilemap>();
        }

        var targetObject = new GameObject(PassableDoorDetailsName);
        targetObject.transform.SetParent(source.transform.parent, false);
        var target = targetObject.AddComponent<Tilemap>();
        var targetRenderer = targetObject.AddComponent<TilemapRenderer>();
        var sourceRenderer = source.GetComponent<TilemapRenderer>();
        if (sourceRenderer != null)
        {
            targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
            targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }
        return target;
    }

    private static void RemoveCollision(GameObject tilemapObject)
    {
        foreach (var collider in tilemapObject.GetComponents<Collider2D>())
        {
            DestroyComponent(collider);
        }
        var body = tilemapObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            DestroyComponent(body);
        }
    }

    private static void DestroyComponent(Component component)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(component);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void ConfigureTilemap(GameObject tilemapObject, bool isTrigger)
    {
        var tilemapCollider = tilemapObject.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
        }

        tilemapCollider.isTrigger = isTrigger;
        tilemapCollider.usedByComposite = true;

        var body = tilemapObject.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = tilemapObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        var composite = tilemapObject.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = tilemapObject.AddComponent<CompositeCollider2D>();
        }

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = isTrigger;
    }
}
