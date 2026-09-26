using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZWGame : MonoBehaviour
{
    const int PointsPerStamp = 15, PerfectBonus = 50;
    static readonly Color HiddenSlot = new Color(0.55f, 0.42f, 0.3f, 0.45f);

    public ZWMenu menu;
    public ZWAudio sound;
    public Sprite[] sealSprites;          // zodiac order, Rat..Pig
    public Sprite starFill, starEmpty;

    [Header("Table")]
    public Button[] seals;                // ring slots, zodiac order clockwise from the top
    public Image[] scrollStamps;
    public Image[] retryTokens;
    public Text levelText, hintText;

    [Header("Target banner")]
    public Text bannerTitle;
    public Image[] bannerSlots;

    [Header("Panels")]
    public GameObject gamePanel, pausePanel, winPanel, losePanel;
    public RectTransform winScroll;
    public Image[] winStars;
    public Text winStats;
    public GameObject nextButton;

    int level, step, wrong, taps, retries;
    int[] order, ring;
    bool locked;
    Vector2[] sealHome;

    void Awake()
    {
        sealHome = new Vector2[seals.Length];
        for (int i = 0; i < seals.Length; i++) sealHome[i] = ((RectTransform)seals[i].transform).anchoredPosition;
    }

    public void Begin(int levelNumber)
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        level = levelNumber;
        var d = ZWLevels.All[level - 1];
        ZWLevels.Build(level, out int[] seq, out ring);
        order = (int[])seq.Clone();
        if (d.reversedOrder) System.Array.Reverse(order);
        step = wrong = taps = 0;
        retries = d.retryBudget;

        for (int i = 0; i < seals.Length; i++)
        {
            seals[i].image.sprite = sealSprites[ring[i]];
            seals[i].transform.localScale = SealScale(i);
            ((RectTransform)seals[i].transform).anchoredPosition = sealHome[i];
        }
        for (int i = 0; i < scrollStamps.Length; i++)
        {
            scrollStamps[i].gameObject.SetActive(false);
            scrollStamps[i].rectTransform.anchoredPosition = new Vector2((i - (seq.Length - 1) / 2f) * 88f, 0f);
        }
        for (int i = 0; i < bannerSlots.Length; i++)
            bannerSlots[i].rectTransform.anchoredPosition = new Vector2((i - (seq.Length - 1) / 2f) * 125f, bannerSlots[i].rectTransform.anchoredPosition.y);
        for (int i = 0; i < retryTokens.Length; i++) retryTokens[i].gameObject.SetActive(i < retries);

        levelText.text = "Level\n" + level.ToString("00");
        bannerTitle.text = d.reversedOrder ? "Stamp in reverse order" : "Stamp this sequence";
        hintText.text = Hint(level);
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        gamePanel.SetActive(true);
        StartCoroutine(ShowTarget(seq, d.targetVisibility));
    }

    IEnumerator ShowTarget(int[] seq, ZWVisibility vis)
    {
        bool partial = vis == ZWVisibility.Partial;
        SetBanner(seq, i => !partial || i % 2 == 0);
        if (vis == ZWVisibility.Visible) { locked = false; yield break; }

        locked = true;
        yield return new WaitForSeconds(1.5f + 0.6f * seq.Length);
        SetBanner(seq, i => false);
        sound.Play(sound.bannerHide);
        locked = false;
    }

    void SetBanner(int[] seq, System.Func<int, bool> shown)
    {
        for (int i = 0; i < bannerSlots.Length; i++)
        {
            var slot = bannerSlots[i];
            slot.gameObject.SetActive(i < seq.Length);
            if (i >= seq.Length) continue;
            bool show = shown(i);
            slot.sprite = show ? sealSprites[seq[i]] : null;
            slot.color = show ? Color.white : HiddenSlot;
            slot.GetComponentInChildren<Text>(true).gameObject.SetActive(!show);
        }
    }

    public void OnSeal(int slot)
    {
        if (locked) return;
        taps++;
        if (slot == order[step])
        {
            var stamp = scrollStamps[step];
            stamp.sprite = sealSprites[slot];
            stamp.gameObject.SetActive(true);
            sound.Play(sound.stamp);
            StartCoroutine(Punch(stamp.transform, Vector3.one, 1.5f));
            StartCoroutine(Punch(seals[slot].transform, SealScale(slot), 0.85f));
            if (++step == order.Length) StartCoroutine(Win());
        }
        else
        {
            wrong++;
            retryTokens[--retries].gameObject.SetActive(false);
            sound.Play(sound.wrongStamp);
            StartCoroutine(Shake(slot));
            if (retries == 0) StartCoroutine(Lose());
        }
    }

    Vector3 SealScale(int slot) => new Vector3(ring[slot] != slot ? -1f : 1f, 1f, 1f); // counterfeits are carved mirrored

    IEnumerator Punch(Transform t, Vector3 baseScale, float from)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.18f)
        {
            t.localScale = baseScale * Mathf.Lerp(from, 1f, k);
            yield return null;
        }
        t.localScale = baseScale;
    }

    IEnumerator Shake(int slot)
    {
        var rt = (RectTransform)seals[slot].transform;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.3f)
        {
            rt.anchoredPosition = sealHome[slot] + new Vector2(Mathf.Sin(k * 40f) * 12f * (1f - k), 0f);
            yield return null;
        }
        rt.anchoredPosition = sealHome[slot];
    }

    IEnumerator Win()
    {
        locked = true;
        yield return new WaitForSeconds(0.6f);
        bool perfect = wrong == 0;
        int stars = perfect ? 3 : wrong <= 1 ? 2 : 1;
        int score = order.Length * PointsPerStamp + (perfect ? PerfectBonus : 0);
        ZWLevels.RecordClear(level, stars, perfect);

        for (int i = 0; i < winStars.Length; i++) winStars[i].sprite = i < stars ? starFill : starEmpty;
        winStats.text = $"Stamps Used   {taps}\nPerfect Sequence   {(perfect ? "Yes" : "No")}\nScore   {score}";
        nextButton.SetActive(level < ZWLevels.Count);
        winPanel.SetActive(true);
        sound.Play(sound.win);

        // Unroll the win scroll.
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.5f)
        {
            winScroll.localScale = new Vector3(k, 1f, 1f);
            yield return null;
        }
        winScroll.localScale = Vector3.one;
    }

    IEnumerator Lose()
    {
        locked = true;
        yield return new WaitForSeconds(0.5f);
        losePanel.SetActive(true);
        sound.Play(sound.lose);
    }

    static string Hint(int level)
    {
        switch (level)
        {
            case 1: return "Tap the seals in the order shown on the banner to stamp the scroll.";
            case 8: return "The banner hides after a moment. Beware counterfeit seals: a true seal sits in its own place on the ring.";
            case 14: return "Only every other seal is shown. The seals run in zodiac order around the ring, so fill in the gaps.";
            default: return "";
        }
    }

    public void Pause() { Time.timeScale = 0f; pausePanel.SetActive(true); }
    public void Resume() { Time.timeScale = 1f; pausePanel.SetActive(false); }
    public void Restart() => Begin(level);
    public void Next() => Begin(level + 1);

    public void MainMenu()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        gamePanel.SetActive(false);
        menu.ShowMain();
    }
}
