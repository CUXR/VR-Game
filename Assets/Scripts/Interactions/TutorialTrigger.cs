using UnityEngine;

public class TutorialTrigger  : MonoBehaviour
{
    public string tutorialID; //manual id settings for now

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TutorialManager.Instance.ShowItem(tutorialID);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TutorialManager.Instance.HideItem(tutorialID);
        }
    }
}
