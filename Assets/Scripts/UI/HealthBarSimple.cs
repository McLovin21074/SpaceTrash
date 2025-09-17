using UnityEngine;
using UnityEngine.UI;

public class HealthBarSimple : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!health)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) health = p.GetComponent<Health>() ?? p.GetComponentInChildren<Health>();
        }
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
        slider.value = 1f;
    }

    private void Update()
    {
        if (!health) return;
        slider.value = Mathf.Clamp01(health.Max > 0 ? (float)health.Current / health.Max : 0f);
    }
}
