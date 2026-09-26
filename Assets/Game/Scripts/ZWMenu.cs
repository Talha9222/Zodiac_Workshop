using UnityEngine;
using UnityEngine.UI;

public class ZWMenu : MonoBehaviour
{
    public ZWGame game;
    public GameObject mainPanel, levelPanel;
    public Text statsText, progressText;
    public Transform levelList;
    public GameObject rowTemplate;

    void Start()
    {
        Application.targetFrameRate = 60;
        ShowMain();
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        statsText.text =
            $"Scrolls Completed   {ZWLevels.Stat(ZWLevels.ScrollsCompleted)}\n" +
            $"Perfect Sequences   {ZWLevels.Stat(ZWLevels.PerfectSequences)}\n" +
            $"Levels Cleared   {ZWLevels.Stat(ZWLevels.LevelsCleared)} / {ZWLevels.Count}";
    }

    public void Play() => StartLevel(ZWLevels.Unlocked);

    public void ShowLevels()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        progressText.text = $"Levels Cleared   {ZWLevels.Stat(ZWLevels.LevelsCleared)} / {ZWLevels.Count}";

        foreach (Transform row in levelList)
            if (row.gameObject != rowTemplate) Destroy(row.gameObject);

        for (int n = 1; n <= ZWLevels.Count; n++)
        {
            var row = Instantiate(rowTemplate, levelList).transform;
            row.gameObject.SetActive(true);
            bool open = n <= ZWLevels.Unlocked;
            int stars = ZWLevels.Stars(n);

            row.Find("Number").GetComponent<Text>().text = n.ToString("00");
            row.Find("Icon").GetComponent<Image>().sprite = game.sealSprites[(n - 1) % 12];
            for (int s = 0; s < 3; s++)
            {
                var star = row.Find("Star" + s).GetComponent<Image>();
                star.gameObject.SetActive(open);
                star.sprite = s < stars ? game.starFill : game.starEmpty;
            }
            row.Find("Lock").gameObject.SetActive(!open);

            var button = row.GetComponent<Button>();
            button.interactable = open;
            int level = n;
            button.onClick.AddListener(() => StartLevel(level));
        }
    }

    public void StartLevel(int n)
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(false);
        game.Begin(n);
    }
}
