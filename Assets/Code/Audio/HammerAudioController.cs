using UnityEngine;

public class HammerAudioController : MonoBehaviour
{
    public Knife Knife;
    public AudioSource[] HitSoundEffects = new AudioSource[0];
    public AudioSource HammerSwingAudioSrc;

    bool wasSwingingLastFrame = false;
    bool hitAPlayerLastFrame = false;

    private void Start()
    {
        Knife ??= GetComponent<Knife>();

        if (HammerSwingAudioSrc)
        {
            HammerSwingAudioSrc.spatialBlend = Knife.IsMine ? 0.25f : 1f;
        }
    }

    private void Update()
    {
        if(HitSoundEffects?.Length > 0 && Knife.HitAPlayer.Value && !hitAPlayerLastFrame)
        {
            HitSoundEffects[Random.Range(0, HitSoundEffects.Length)].Play();
        }

        if (HammerSwingAudioSrc && Knife.IsSwinging && !wasSwingingLastFrame)
        {
            HammerSwingAudioSrc.Play();
        }

        wasSwingingLastFrame = Knife.IsSwinging;
        hitAPlayerLastFrame = Knife.HitAPlayer.Value;
    }
}
