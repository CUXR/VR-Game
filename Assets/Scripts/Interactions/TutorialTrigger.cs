using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialTrigger  : MonoBehaviour
{
    public string tutorialID; //manual id settings for now

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TutorialController.Instance.ShowText(tutorialID);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TutorialController.Instance.HideText(tutorialID);
        }
    }
}
