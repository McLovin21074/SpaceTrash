using UnityEngine;

public class PlayerDeathUI : MonoBehaviour
{
    [SerializeField] private DeathMenuController deathMenu;
    [SerializeField] private Health playerHealth;

    private void Awake()
    {
        // Найти меню, даже если на другой сцене/в выключенном Canvas
        if (deathMenu == null)
            deathMenu = FindFirstObjectByType<DeathMenuController>(FindObjectsInactive.Include);

        // Если скрипт висит НЕ на игроке — найдем здоровье игрока по тегу
        if (playerHealth == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) playerHealth = go.GetComponent<Health>();
        }

        // Если всё ещё null, попробуем взять с этого же объекта (кейс: скрипт повесили на игрока)
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        if (playerHealth != null)
        {
            playerHealth.onDeath.AddListener(OnPlayerDeath);
        }
        else
        {
            Debug.LogWarning("[PlayerDeathUI] Не найден Health игрока — меню смерти не покажется.");
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.onDeath.RemoveListener(OnPlayerDeath);
    }

    private void OnPlayerDeath()
    {
        Debug.Log("[PlayerDeathUI] Player died → show death menu");
        if (deathMenu != null) deathMenu.Show();
        else Debug.LogWarning("[PlayerDeathUI] DeathMenuController не найден.");
    }
}
