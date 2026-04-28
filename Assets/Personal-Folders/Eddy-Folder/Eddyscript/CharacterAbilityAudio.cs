using UnityEngine;
using PurrNet;

public enum AbilitySoundSlot
{
    BasicAttack = 0,
    Ability1 = 1,
    Ability2 = 2,
    Ability3 = 3,
    Ultimate = 4
}

public abstract class CharacterAbilityAudio : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip basicAttackClip;
    [SerializeField] private AudioClip ability1Clip;
    [SerializeField] private AudioClip ability2Clip;
    [SerializeField] private AudioClip ability3Clip;
    [SerializeField] private AudioClip ultimateClip;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    protected virtual void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>(true);
    }

    protected void PlayLocalAbilitySfx(AbilitySoundSlot slot)
    {
        PlayClip(slot);
    }

    protected void PlayAbilitySfxForEveryone(AbilitySoundSlot slot)
    {
        if (isServer)
        {
            PlayAbilitySfxObserversRpc((int)slot);
            return;
        }

        PlayAbilitySfxServerRpc((int)slot);
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayAbilitySfxServerRpc(int slotIndex)
    {
        PlayAbilitySfxObserversRpc(slotIndex);
    }

    [ObserversRpc]
    private void PlayAbilitySfxObserversRpc(int slotIndex)
    {
        PlayClip((AbilitySoundSlot)slotIndex);
    }

    private void PlayClip(AbilitySoundSlot slot)
    {
        if (audioSource == null)
            return;

        AudioClip clip = GetClip(slot);
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private AudioClip GetClip(AbilitySoundSlot slot)
    {
        return slot switch
        {
            AbilitySoundSlot.BasicAttack => basicAttackClip,
            AbilitySoundSlot.Ability1 => ability1Clip,
            AbilitySoundSlot.Ability2 => ability2Clip,
            AbilitySoundSlot.Ability3 => ability3Clip,
            AbilitySoundSlot.Ultimate => ultimateClip,
            _ => null
        };
    }
}