using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea] public string tutorialMessage = "Hold [SHIFT] to Sprint";
    public float displayDuration = 4f;
    public TextMeshProUGUI tutorialTextUI;
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player") && tutorialTextUI != null)
        {
            hasTriggered = true;
            StopAllCoroutines();
            StartCoroutine(ShowTutorial());
        }
    }

    IEnumerator ShowTutorial()
    {
        tutorialTextUI.text = tutorialMessage;
        yield return new WaitForSeconds(displayDuration);
        tutorialTextUI.text = "";
    }
}