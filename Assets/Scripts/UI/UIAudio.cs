using UnityEngine;

public class UIAudio : MonoBehaviour
{
    public static UIAudio I;
    public AudioSource src;
    public AudioClip pick, drop, craft;

    void Awake() { I = this; if (!src) src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; }

    public static void PlayPick()  { if (I?.pick)  I.src.PlayOneShot(I.pick);  }
    public static void PlayDrop()  { if (I?.drop)  I.src.PlayOneShot(I.drop);  }
    public static void PlayCraft() { if (I?.craft) I.src.PlayOneShot(I.craft); }
}
