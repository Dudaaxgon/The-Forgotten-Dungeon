using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AutoCharacterAnimationBuilder
{
    private const string CharactersRoot = "Assets/Characters/AutoImported";
    private const int PixelsPerUnit = 32;

    [MenuItem("Tools/Characters/Rebuild Auto Imported Characters")]
    public static void BuildAll()
    {
        if (!Directory.Exists(CharactersRoot))
        {
            Debug.LogWarning($"Characters root not found: {CharactersRoot}");
            return;
        }

        ConfigureTextures();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var characterDirs = Directory.GetDirectories(CharactersRoot)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var generatedCharacters = 0;
        var generatedClips = 0;

        foreach (var characterDir in characterDirs)
        {
            var framesDir = Path.Combine(characterDir, "Frames").Replace('\\', '/');
            if (!Directory.Exists(framesDir))
            {
                continue;
            }

            EnsureDirectory(Path.Combine(characterDir, "Animations"));
            EnsureDirectory(Path.Combine(characterDir, "Animator"));
            EnsureDirectory(Path.Combine(characterDir, "Prefabs"));

            var clips = BuildClips(characterDir, framesDir);
            if (clips.Count == 0)
            {
                continue;
            }

            var controller = BuildController(characterDir, clips);
            BuildPrefab(characterDir, controller, clips);

            generatedCharacters++;
            generatedClips += clips.Count;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"AutoCharacterAnimationBuilder generated {generatedClips} clips for {generatedCharacters} characters.");
    }

    private static void ConfigureTextures()
    {
        var pngs = Directory.GetFiles(CharactersRoot, "*.png", SearchOption.AllDirectories);
        foreach (var file in pngs)
        {
            var assetPath = NormalizeAssetPath(file);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static List<AnimationClip> BuildClips(string characterDir, string framesDir)
    {
        var clips = new List<AnimationClip>();
        var animationRoot = Path.Combine(characterDir, "Animations").Replace('\\', '/');
        var frameFolders = Directory.GetDirectories(framesDir)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var frameFolder in frameFolders)
        {
            var framePaths = Directory.GetFiles(frameFolder, "*.png")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(NormalizeAssetPath)
                .ToArray();

            if (framePaths.Length == 0)
            {
                continue;
            }

            var sprites = framePaths
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();

            if (sprites.Length == 0)
            {
                continue;
            }

            var clipName = Path.GetFileName(frameFolder);
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = GetFrameRate(clipName)
            };

            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = ShouldLoop(clipName);
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var clipPath = $"{animationRoot}/{clipName}.anim";
            if (File.Exists(clipPath))
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AssetDatabase.CreateAsset(clip, clipPath);
            clips.Add(clip);
        }

        return clips;
    }

    private static AnimatorController BuildController(string characterDir, List<AnimationClip> clips)
    {
        var characterName = Path.GetFileName(characterDir);
        var controllerPath = $"{characterDir.Replace('\\', '/')}/Animator/{characterName}.controller";
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = Array.Empty<ChildAnimatorState>();

        AnimatorState defaultState = null;
        foreach (var clip in clips.OrderBy(clip => clip.name, StringComparer.OrdinalIgnoreCase))
        {
            var state = stateMachine.AddState(clip.name);
            state.motion = clip;
            if (defaultState == null || clip.name.Equals("Idle_Down", StringComparison.OrdinalIgnoreCase))
            {
                defaultState = state;
            }
        }

        if (defaultState != null)
        {
            stateMachine.defaultState = defaultState;
        }

        return controller;
    }

    private static void BuildPrefab(string characterDir, AnimatorController controller, List<AnimationClip> clips)
    {
        var characterName = Path.GetFileName(characterDir);
        var prefabPath = $"{characterDir.Replace('\\', '/')}/Prefabs/{characterName}.prefab";
        var go = new GameObject(characterName);
        var renderer = go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var firstSprite = FindInitialSprite(clips);
        if (firstSprite != null)
        {
            renderer.sprite = firstSprite;
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static Sprite FindInitialSprite(List<AnimationClip> clips)
    {
        var preferredClip = clips.FirstOrDefault(clip => clip.name.Equals("Idle_Down", StringComparison.OrdinalIgnoreCase))
            ?? clips.FirstOrDefault(clip => clip.name.StartsWith("Idle_", StringComparison.OrdinalIgnoreCase))
            ?? clips.FirstOrDefault();

        if (preferredClip == null)
        {
            return null;
        }

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        var frames = AnimationUtility.GetObjectReferenceCurve(preferredClip, binding);
        return frames != null && frames.Length > 0 ? frames[0].value as Sprite : null;
    }

    private static float GetFrameRate(string clipName)
    {
        if (clipName.Contains("Idle") || clipName.Contains("Hurt") || clipName.Contains("Death"))
        {
            return 8f;
        }

        return 10f;
    }

    private static bool ShouldLoop(string clipName)
    {
        return clipName.Contains("Idle")
            || clipName.Contains("Walk")
            || clipName.Contains("Run");
    }

    private static void EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    private static string NormalizeAssetPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var index = normalized.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? normalized.Substring(index) : normalized;
    }
}
