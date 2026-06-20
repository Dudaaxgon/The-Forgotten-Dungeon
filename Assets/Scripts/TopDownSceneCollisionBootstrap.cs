using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TopDownSceneCollisionBootstrap
{
    private static readonly string[] SolidTilemapNameParts =
    {
        "parede",
        "porta",
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

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
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
