using UnityEngine;

public class LithiumAudioController : MonoBehaviour
{
    public Phone Phone;

    public AudioSource CountdownBeepLoopAudioSrc;
    public AudioSource DetonationAudioSrc;

    bool wasExplodingLastFrame;

    private void Start()
    {
        Phone ??= GetComponent<Phone>();
    }

    private void Update()
    {
        if (CountdownBeepLoopAudioSrc) 
        {
            CountdownBeepLoopAudioSrc.spatialBlend = Phone.IsOwner ? 0.5f : 1f;
        }
        if (DetonationAudioSrc) 
        {
            DetonationAudioSrc.spatialBlend = Phone.IsOwner ? 0.5f : 1f;
        }

        if(Phone.isExploding && !wasExplodingLastFrame && DetonationAudioSrc?.isPlaying == false)
        {
            CountdownBeepLoopAudioSrc?.Stop();
            DetonationAudioSrc.Play();
        }
        else if(!Phone.isExploding && CountdownBeepLoopAudioSrc?.isPlaying == false)
        {
            CountdownBeepLoopAudioSrc.Play();
        }

        wasExplodingLastFrame = Phone.isExploding;
    }
}
