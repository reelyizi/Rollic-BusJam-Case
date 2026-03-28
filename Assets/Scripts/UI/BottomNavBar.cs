using UnityEngine;
using UnityEngine.UI;

public class BottomNavBar : MonoBehaviour
{
    [SerializeField] private BottomNavTab[] tabs;
    [SerializeField] private int defaultTab;

    [Header("Animation Settings")]
    [SerializeField] private float bgSlideUp = 50f;
    [SerializeField] private float iconSlideUp = 15f;
    [SerializeField] private float textSlideUp = 10f;
    [SerializeField] private float animDuration = 0.25f;

    private int currentTab = -1;

    private void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            var button = tabs[i].GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectTab(index));

            tabs[i].SetSelectedImmediate(i == defaultTab, bgSlideUp, iconSlideUp, textSlideUp);
        }

        currentTab = defaultTab;
    }

    public void SelectTab(int index)
    {
        if (index == currentTab || index < 0 || index >= tabs.Length) return;

        if (currentTab >= 0)
            tabs[currentTab].SetSelected(false, bgSlideUp, iconSlideUp, textSlideUp, animDuration);

        tabs[index].SetSelected(true, bgSlideUp, iconSlideUp, textSlideUp, animDuration);
        currentTab = index;
    }
}
