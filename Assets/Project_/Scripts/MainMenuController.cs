
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject missionSelectionPanel;

    [Header("Main Menu Elements")]
    [SerializeField] private GameObject[] mainMenuElements;

    private void Awake()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (missionSelectionPanel != null)
            missionSelectionPanel.SetActive(false);

        SetMainMenuVisible(true);
    }

    private void SetMainMenuVisible(bool visible)
    {
        foreach (GameObject element in mainMenuElements)
        {
            if (element != null)
                element.SetActive(visible);
        }
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    // الانتقال إلى شاشة اختيار المهمات
    public void OpenMissionSelection()
    {
        if (missionSelectionPanel == null)
        {
            Debug.LogError("Mission Selection Panel is not assigned!");
            return;
        }

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        SetMainMenuVisible(false);
        missionSelectionPanel.SetActive(true);
    }
}
