using System;
using UnityEngine;

[RequireComponent(typeof(KMSelectable))]
public class NumberedButton : MonoBehaviour
{
    public int Digit;
    
    private CubeSynchronizationModule module;

    private Action OnStrike = null;
    
    void Start()
    {
        module = transform.parent.parent.parent.GetComponent<CubeSynchronizationModule>();
        GetComponent<KMSelectable>().OnInteract += () =>
        {
            module.GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            GetComponent<KMSelectable>().AddInteractionPunch(.5f);
            bool correct = module.SubmitDigit(Digit.ToString());
            gameObject.SetActive(correct);
            if (!correct && OnStrike != null) OnStrike();
            return false;
        };
        gameObject.SetActive(false);
    }

    public void Click(Action onStrike)
    {
        OnStrike = onStrike;
        GetComponent<KMSelectable>().OnInteract();
        OnStrike = null;
    }
}