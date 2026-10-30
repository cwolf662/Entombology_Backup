//A bunch of simple playing sounds functions for other scripts to use.

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundEffects : MonoBehaviour {
    [Header("Death Sounds")]
    [SerializeField] private AudioClip deathSound;

    [Header("Attack Sounds")]
    [SerializeField] private AudioClip biteSound;
    [SerializeField] private AudioClip specialAttackSound;

    [Header("Damage Sounds")]
    [SerializeField] private AudioClip zombieHitSound;

    private AudioSource audioSource;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayDeath(){
        PlaySound(deathSound);
    }

    public void PlayBite(){
        PlaySound(biteSound);
    }

    public void PlaySpecialAttack(){
        PlaySound(specialAttackSound);
    }

    public void PlayZombieHit(){
        PlaySound(zombieHitSound);
    }

    private void PlaySound(AudioClip clip){
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
