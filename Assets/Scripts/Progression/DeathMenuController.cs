// DeathMenuController.cs
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text expText;
    [SerializeField] private Button bossButton;
    [SerializeField] private UnityEngine.UI.Slider expSlider;

    [Header("Boss fight")]
    [SerializeField] private string bossSceneName = "BossScene";
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private GameObject bossLockedMessageRoot;
    [SerializeField] private Text bossLockedMessageText;
    [SerializeField] private float bossLockedMessageDuration = 2f;

    private Coroutine bossLockedRoutine;

    [Header("New record popup")]
    [SerializeField] private GameObject newRecordPanel;
    [SerializeField] private Text newRecordText;
    [SerializeField] private Button newRecordCloseButton;


    [Header("Buttons per-upgrade")]
    [SerializeField] private UpgradeButton[] upgradeButtons;

    private void Awake()
    {
        if (panel) panel.SetActive(false);
        if (newRecordPanel) newRecordPanel.SetActive(false);
        if (newRecordCloseButton)
        {
            newRecordCloseButton.onClick.RemoveAllListeners();
            newRecordCloseButton.onClick.AddListener(HideNewRecordPopup);
        }

        if (upgradeButtons != null)
        {
            foreach (var ub in upgradeButtons)
                if (ub != null) ub.Bind();
        }
        HideBossLockedMessageImmediate();
    }


    private void OnEnable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.AddListener(Refresh);
    }

    private void OnDisable()
    {
        if (MetaProgression.Instance)
            MetaProgression.Instance.onValuesChanged.RemoveListener(Refresh);
    }

    public void Show()
    {
        if (panel) panel.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
        HideBossLockedMessageImmediate();
        ShowNewRecordPopupIfNeeded();
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
        Time.timeScale = 1f;
        HideNewRecordPopup();
        HideBossLockedMessageImmediate();
    }


    private void ShowNewRecordPopupIfNeeded()
    {
        var mp = MetaProgression.Instance;
        if (mp != null)
        {
            int record = mp.MaxWaveReached;
            if (mp.ConsumeNewBestWaveFlag())
            {
                if (newRecordText) newRecordText.text = $"Новый рекорд: {record}";
                if (newRecordPanel) newRecordPanel.SetActive(true);
                return;
            }
        }

        HideNewRecordPopup();
    }

    private void HideNewRecordPopup()
    {
        if (newRecordPanel && newRecordPanel.activeSelf)
            newRecordPanel.SetActive(false);
    }

    public void OnContinue()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnFightBoss()
    {
        var mp = MetaProgression.Instance;
        if (mp == null)
        {
            Debug.LogWarning("[DeathMenuController] MetaProgression instance not found.");
            return;
        }

        if (mp.BossUnlocked)
        {
            if (string.IsNullOrWhiteSpace(bossSceneName))
            {
                Debug.LogError("[DeathMenuController] Boss scene name is not set.");
                return;
            }

            Time.timeScale = 1f;
            HideBossLockedMessageImmediate();
            SceneManager.LoadScene(bossSceneName);
            return;
        }

        ShowBossLockedMessage(mp);
    }

    public void OnReturnToMenu()
    {
        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("[DeathMenuController] Menu scene name is not set.");
            return;
        }

        Time.timeScale = 1f;
        HideBossLockedMessageImmediate();
        SceneManager.LoadScene(menuSceneName);
    }

    public void Refresh()
    {
        var mp = MetaProgression.Instance;

        if (mp != null)
        {
            if (coinsText) coinsText.text = $"Монет: {mp.Coins}";
            if (expText) expText.text = $"Опыт: {mp.Exp} / {mp.BossUnlockExp}";
            if (bossButton) bossButton.interactable = true;

            if (expSlider)
            {
                expSlider.minValue = 0f;
                expSlider.maxValue = Mathf.Max(1, mp.BossUnlockExp);
                expSlider.value = Mathf.Clamp(mp.Exp, 0, mp.BossUnlockExp);
            }
        }

        if (upgradeButtons != null)
            foreach (var ub in upgradeButtons)
                if (ub != null) ub.Refresh();
    }





    private void ShowBossLockedMessage(MetaProgression mp)
    {
        if (bossLockedRoutine != null)
        {
            StopCoroutine(bossLockedRoutine);
            bossLockedRoutine = null;
        }

        var messageTarget = bossLockedMessageRoot != null
            ? bossLockedMessageRoot
            : bossLockedMessageText != null
                ? bossLockedMessageText.gameObject
                : null;

        if (messageTarget != null && !messageTarget.activeSelf)
        {
            messageTarget.SetActive(true);
        }

        if (bossLockedMessageText != null)
        {
            int requiredExp = mp != null ? mp.BossUnlockExp : 0;
            int currentExp = mp != null ? mp.Exp : 0;
            int missing = Mathf.Max(0, requiredExp - currentExp);

            bossLockedMessageText.text = missing > 0
                ? $"Нужно ещё {missing} опыта для битвы с боссом"
                : "Нужно больше опыта для битвы с боссом";
        }

        bossLockedRoutine = StartCoroutine(HideBossLockedMessageRoutine());
    }

    private IEnumerator HideBossLockedMessageRoutine()
    {
        if (bossLockedMessageDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(bossLockedMessageDuration);
        }
        else
        {
            yield return null;
        }

        bossLockedRoutine = null;
        HideBossLockedMessageImmediate();
    }

    private void HideBossLockedMessageImmediate()
    {
        if (bossLockedRoutine != null)
        {
            StopCoroutine(bossLockedRoutine);
            bossLockedRoutine = null;
        }

        var messageTarget = bossLockedMessageRoot != null
            ? bossLockedMessageRoot
            : bossLockedMessageText != null
                ? bossLockedMessageText.gameObject
                : null;

        if (messageTarget != null && messageTarget.activeSelf)
        {
            messageTarget.SetActive(false);
        }

        if (bossLockedMessageText != null)
        {
            bossLockedMessageText.text = string.Empty;
        }
    }

    [System.Serializable]
    public class UpgradeButton
    {
        public UpgradeType type;

        // РЎСЃС‹Р»РєРё
        public Button buyButton;
        public UnityEngine.UI.Text titleText;
        public UnityEngine.UI.Text priceText;
        public UnityEngine.UI.Image coinIcon;

        public void Bind()
        {
            if (buyButton == null) return;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                var um = UpgradeManager.Instance;
                if (um != null && um.TryBuy(type))
                {

                    var ctrl = GameObject.FindFirstObjectByType<DeathMenuController>(FindObjectsInactive.Include);
                    if (ctrl) ctrl.Refresh();
                }
                else
                {
                    Refresh();
                }
            });
        }

        public void Refresh()
        {
            var um = UpgradeManager.Instance;
            var mp = MetaProgression.Instance;
            if (um == null || mp == null || buyButton == null) return;

            string niceTitle = um.GetTitle(type);
            if (titleText) titleText.text = $"{niceTitle}";

            int lvl = um.GetLevel(type);
            bool unlocked = um.IsUnlocked(type);
            bool maxed = um.IsMaxed(type);
            int price = um.GetPrice(type);

            if (priceText)
            {
                if (!unlocked) priceText.text = "A”’";
                else if (maxed) priceText.text = "MAX";
                else priceText.text = price.ToString();
            }
            if (coinIcon) coinIcon.enabled = unlocked && !maxed;

            buyButton.interactable = unlocked && !maxed && mp.Coins >= price;
        }
    }

}