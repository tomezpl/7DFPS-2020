using UnityEngine;

public class CannonBulletAudioController : MonoBehaviour
{
    public CannonBullet CannonBullet;
    bool WasRestingLastFrame = false;

    bool HitRoombaLastFrame = false;

    public AudioSource BulletCaseRollAudioSrc;
    public AudioSource BulletHitRoombaAudioSrc;

    private void Start()
    {
        CannonBullet ??= GetComponent<CannonBullet>();
    }

    private void Update()
    {
        if(!HitRoombaLastFrame && CannonBullet.Hit != null && BulletHitRoombaAudioSrc?.isPlaying == false)
        {
            BulletHitRoombaAudioSrc.Play();
        }

        if(!WasRestingLastFrame && CannonBullet.IsResting && BulletCaseRollAudioSrc?.isPlaying == false)
        {
            BulletCaseRollAudioSrc.Play();
        }

        HitRoombaLastFrame = CannonBullet.Hit != null;
        WasRestingLastFrame = CannonBullet.IsResting;
    }
}
