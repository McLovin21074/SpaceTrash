using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>
/// Runtime manager for the boss fight scene. Updates boss HP bar, awards victory, and shows defeat/victory popups.
/// </summary>
public class BossFightManager : MonoBehaviour
{
    public static BossFightManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private int coinRewardOnWin = 100;
    [FormerlySerializedAs("pauseOnVictory")]
    [SerializeField] private bool pauseOnResult = false;

    [Header("Runtime UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Text victoryTitle;
    [SerializeField] private Text victoryDescription;
    [SerializeField] private Button menuButton;

    private BossEnemyAI trackedBoss;
    private Health trackedHealth;
    private Health playerHealth;
    private bool rewardsGranted;
    private bool resultShown;

    public static BossFightManager EnsureExists()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<BossFightManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BossEnemyAI.OnBossSpawned += HandleBossSpawned;
        BossEnemyAI.OnBossDefeated += HandleBossDefeated;

        EnsureEventSystem();
        BindMenuButton();
        InitializeHealthBar();
        HideResultPanel();
        SubscribeToPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        BossEnemyAI.OnBossSpawned -= HandleBossSpawned;
        BossEnemyAI.OnBossDefeated -= HandleBossDefeated;

        if (playerHealth != null)
        {
            playerHealth.onDeath.RemoveListener(OnPlayerDeath);
        }
    }

    private void Update()
    {
        if (bossHealthSlider == null || trackedHealth == null || resultShown) return;
        if (trackedHealth.Max <= 0) return;

        float ratio = Mathf.Clamp01((float)trackedHealth.Current / trackedHealth.Max);
        bossHealthSlider.value = ratio;
    }

    private void HandleBossSpawned(BossEnemyAI boss)
    {
        trackedBoss = boss;
        trackedHealth = boss != null ? boss.Health : null;
        rewardsGranted = false;

        SubscribeToPlayer();

        if (uiCanvas != null && !uiCanvas.gameObject.activeSelf)
        {
            uiCanvas.gameObject.SetActive(true);
        }

        if (bossHealthSlider != null)
        {
            bossHealthSlider.minValue = 0f;
            bossHealthSlider.maxValue = 1f;
            bossHealthSlider.value = 1f;
            bossHealthSlider.gameObject.SetActive(true);
        }
    }

    private void HandleBossDefeated(BossEnemyAI boss)
    {
        if (boss != trackedBoss) return;

        GrantVictoryRewards();
        ShowVictoryPanel();

        trackedBoss = null;
        trackedHealth = null;
    }

    private void GrantVictoryRewards()
    {
        if (rewardsGranted) return;
        rewardsGranted = true;

        if (MetaProgression.Instance != null)
        {
            MetaProgression.Instance.UnlockMirrorFire();
            if (coinRewardOnWin > 0)
            {
                MetaProgression.Instance.AddCoins(coinRewardOnWin);
            }
        }
        else if (coinRewardOnWin > 0)
        {
            Debug.LogWarning("[BossFightManager] MetaProgression not found. Coins reward skipped.");
        }

        var shooter = FindPlayerShooter();
        if (shooter != null)
        {
            shooter.EnableMirrorFire(true);
        }
    }

    private void ShowVictoryPanel()
    {
        ShowResultPanel(
            "Победа!",
            $"Ты одолел босса. Теперь стреляешь в обе стороны и получил {coinRewardOnWin} монет."
        );
    }

    private void ShowDefeatPanel()
    {
        ShowResultPanel(
            "Поражение",
            "Ты пал в бою. Попробуй ещё раз и будь осторожнее."
        );
    }

    private void ShowResultPanel(string title, string description)
    {
        if (resultShown) return;
        resultShown = true;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(false);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (victoryTitle != null)
        {
            victoryTitle.text = title;
        }

        if (victoryDescription != null)
        {
            victoryDescription.text = description;
        }

        if (pauseOnResult)
        {
            Time.timeScale = 0f;
        }
    }

    private void HideResultPanel()
    {
        if (pauseOnResult)
        {
            Time.timeScale = 1f;
        }

        resultShown = false;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (victoryTitle != null) victoryTitle.text = string.Empty;
        if (victoryDescription != null) victoryDescription.text = string.Empty;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = 1f;
        }
    }

    private void InitializeHealthBar()
    {
        if (bossHealthSlider == null) return;
        bossHealthSlider.minValue = 0f;
        bossHealthSlider.maxValue = 1f;
        bossHealthSlider.value = 1f;
    }

    private void BindMenuButton()
    {
        if (menuButton == null) return;
        menuButton.onClick.RemoveListener(OnMenuButtonPressed);
        menuButton.onClick.AddListener(OnMenuButtonPressed);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        es.transform.SetParent(transform, false);
    }

    private void SubscribeToPlayer()
    {
        if (playerHealth != null) return;

        var player = FindPlayer();
        if (player == null) return;

        playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.onDeath.AddListener(OnPlayerDeath);
        }
    }

    private void OnPlayerDeath()
    {
        if (resultShown) return;
        ShowDefeatPanel();
    }

    private Player FindPlayer()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<Player>();
#else
        return FindObjectOfType<Player>();
#endif
    }

    private PlayerShooting FindPlayerShooter()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<PlayerShooting>();
#else
        return FindObjectOfType<PlayerShooting>();
#endif
    }

    private void OnMenuButtonPressed()
    {
        if (pauseOnResult)
        {
            Time.timeScale = 1f;
        }

        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
