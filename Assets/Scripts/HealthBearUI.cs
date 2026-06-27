using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider slider;
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth != null)
        {
            slider.maxValue = playerHealth.MaxHealth;
            slider.value = playerHealth.CurrentHealth;
            
            // Conecta ao evento do Diogo
            playerHealth.HealthChanged += OnHealthChanged;
        }
    }

    void OnHealthChanged(int current, int max)
    {
        slider.value = current;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;
    }
}