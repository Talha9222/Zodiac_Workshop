using UnityEngine;

public class ZWAudio : MonoBehaviour
{
    public AudioSource sfx, bgm;          // bgm loops and plays on awake once a clip is assigned
    public AudioClip click, stamp, wrongStamp, bannerHide, win, lose;

    public void Play(AudioClip clip)
    {
        if (clip != null) sfx.PlayOneShot(clip);
    }

    public void Click() => Play(click);
}
