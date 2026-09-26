using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ZWSplash : MonoBehaviour
{
    const float MinDuration = 3f;

    static readonly string[] Steps =
    {
        "Opening the workshop shutters...",
        "Grinding brown ink...",
        "Carving the twelve seals...",
        "Unrolling the ceremonial scroll...",
    };

    static readonly string[] Tips =
    {
        "Twelve carved seals rest on the craftsman's ring, from Rat to Pig.",
        "A true seal always sits in its own place on the ring.",
        "Every correct stamp is pressed into the scroll for good.",
        "A wrong stamp costs a retry token, not the whole scroll.",
        "The Ox is patient. Study the banner before you stamp.",
        "Clear a scroll without a single wrong stamp for a Perfect Sequence.",
    };

    public RectTransform barFill, ring;
    public Text stepText, percentText, tipText;

    IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        tipText.text = Tips[Random.Range(0, Tips.Length)];
        var op = SceneManager.LoadSceneAsync("ZWMain");
        op.allowSceneActivation = false;

        for (float t = 0f; ; t += Time.deltaTime)
        {
            // The bar follows whichever is slower: the real load or the minimum splash time.
            float p = Mathf.Min(op.progress / 0.9f, t / MinDuration);
            barFill.anchorMax = new Vector2(p, 1f);
            percentText.text = Mathf.RoundToInt(p * 100f) + "%";
            stepText.text = Steps[Mathf.Min((int)(p * Steps.Length), Steps.Length - 1)];
            ring.Rotate(0f, 0f, -12f * Time.deltaTime);
            if (p >= 1f) break;
            yield return null;
        }
        op.allowSceneActivation = true;
    }
}
