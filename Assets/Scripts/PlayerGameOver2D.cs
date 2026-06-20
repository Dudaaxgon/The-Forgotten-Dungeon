using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerGameOver2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float deathAnimationDelay = 0.7f;
    [SerializeField, Min(0.5f)] private float restartDelay = 2.5f;

    private PlayerHealth health;
    private bool handlingGameOver;
    private bool showOverlay;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<PlayerHealth>();
        }

        health.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= OnHealthChanged;
        }

        if (handlingGameOver)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0 && !handlingGameOver)
        {
            StartCoroutine(GameOverSequence());
        }
    }

    private IEnumerator GameOverSequence()
    {
        handlingGameOver = true;
        yield return new WaitForSecondsRealtime(deathAnimationDelay);
        showOverlay = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(restartDelay);
        Time.timeScale = 1f;
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private void OnGUI()
    {
        if (!showOverlay)
        {
            return;
        }

        var overlay = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.whiteTexture }
        };
        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlay);
        GUI.color = previousColor;

        var label = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.height / 10, 42, 96),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "GAME OVER", label);
    }
}
