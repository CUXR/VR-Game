using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance; 
    
    public TextMeshProUGUI uiTextElement; 

    // Initializes this instance
    void Awake()
    {
         if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Displays tutorial text
    public void DisplayText(InteractableInterface interactableObject)
    {
        Debug.Log("display");
        uiTextElement.text = interactableObject.GetTutorialText();
    }

    // Clears current tutorial text
    public void ClearText()
    {
        uiTextElement.text = "";
    }

}