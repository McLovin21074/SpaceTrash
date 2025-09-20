// DeathMenuController.cs
using UnityEngine;
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

    [Header("Buttons per-upgrade")]
    [SerializeField] private UpgradeButton[] upgradeButtons;

    private void Awake()
    {
        if (panel) panel.SetActive(false);

            if (upgradeButtons != null)
                foreach (var ub in upgradeButtons)
                    if (ub != null) ub.Bind();
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
        Time.timeScale = 0f; 
        Refresh();
    }

    public void Hide()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnContinue() 
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnFightBoss()
    {
        Debug.Log("Boss fight not implemented yet.");
    }

    public void Refresh()
    {
        var mp = MetaProgression.Instance;

        if (mp != null)
        {
            if (coinsText) coinsText.text = $"Монеты: {mp.Coins}";
            if (expText)   expText.text   = $"Опыт: {mp.Exp} / {mp.BossUnlockExp}";
            if (bossButton) bossButton.interactable = mp.BossUnlocked;

            if (expSlider)
            {
                expSlider.minValue = 0f;
                expSlider.maxValue = Mathf.Max(1, mp.BossUnlockExp);
                expSlider.value    = Mathf.Clamp(mp.Exp, 0, mp.BossUnlockExp);
            }
        }

        if (upgradeButtons != null)
            foreach (var ub in upgradeButtons)
                if (ub != null) ub.Refresh();
    }


}

[System.Serializable]
public class UpgradeButton
{
    public UpgradeType type;

    // Ссылки
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
            if (!unlocked)       priceText.text = "🔒";
            else if (maxed)      priceText.text = "MAX";
            else                 priceText.text = price.ToString();
        }
        if (coinIcon) coinIcon.enabled = unlocked && !maxed;

        buyButton.interactable = unlocked && !maxed && mp.Coins >= price;
    }
}

