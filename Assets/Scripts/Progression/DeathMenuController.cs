// DeathMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text coinsText;    // или TMP_Text
    [SerializeField] private Text expText;      // или TMP_Text
    [SerializeField] private Button bossButton;

    [Header("Buttons per-upgrade")]
    [SerializeField] private UpgradeButton[] upgradeButtons;

    private void Awake()
    {
        if (panel) panel.SetActive(false);

        // привяжем onClick ко всем кнопкам апгрейдов
        if (upgradeButtons != null)
        {
            foreach (var ub in upgradeButtons)
            {
                if (ub != null && ub.buyButton != null)
                {
                    var capture = ub; // замыкание!
                    ub.buyButton.onClick.RemoveAllListeners();
                    ub.buyButton.onClick.AddListener(() =>
                    {
                        if (UpgradeManager.Instance != null && capture != null)
                        {
                            if (UpgradeManager.Instance.TryBuy(capture.type))
                                Refresh();
                            else
                                capture.Refresh(); // обновим надпись/доступность
                        }
                    });
                }
            }
        }
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
        panel.SetActive(true);
        Time.timeScale = 0f; // чтобы игра встала
        Refresh();
    }

    public void Hide()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnContinue() // «Продолжить» — начинаем новый забег
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnFightBoss()
    {
        // пока заглушка
        Debug.Log("Boss fight not implemented yet.");
    }

    private void Refresh()
    {
        if (MetaProgression.Instance)
        {
            if (coinsText) coinsText.text = $"Монеты: {MetaProgression.Instance.Coins}";
            if (expText)   expText.text   = $"Опыт: {MetaProgression.Instance.Exp} / {MetaProgression.Instance.BossUnlockExp}";
            if (bossButton) bossButton.interactable = MetaProgression.Instance.BossUnlocked;
        }

        foreach (var ub in upgradeButtons)
        {
            if (ub != null) ub.Refresh();   // <- вот так
        }
    }

}

[System.Serializable]
public class UpgradeButton
{
    public UpgradeType type;
    public Text label;  // "Скорость L3  (стоимость 25)" — или TMP_Text
    public Button buyButton;

public void Refresh()
{
    var um = UpgradeManager.Instance;
    var mp = MetaProgression.Instance;
    if (label == null || buyButton == null || um == null || mp == null)
    {
        if (label) label.text = $"{type}  — недоступно";
        if (buyButton) buyButton.interactable = false;
        return;
    }

    int lvl = um.GetLevel(type);
    bool unlocked = um.IsUnlocked(type);
    bool maxed = um.IsMaxed(type);

    string pricePart = "";
    if (!maxed && unlocked)
        pricePart = $" (цена {um.GetPrice(type)})";

    if (!unlocked) pricePart = "  [🔒 по опыту]";
    if (maxed)     pricePart = " (MAX)";

    label.text = $"{type}  L{lvl}{pricePart}";
    buyButton.interactable = unlocked && !maxed && mp.Coins >= um.GetPrice(type);
}


    public void Buy()
    {
        var um = UpgradeManager.Instance;
        if (um != null && um.TryBuy(type))
            Refresh();
    }
}
