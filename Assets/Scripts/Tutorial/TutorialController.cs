using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable]
public class TutorialEntry
{
    public string id;
    public string text;
    public float priority;
}

[Serializable]
public class TutorialData
{
    public List<TutorialEntry> tutorials;
}

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance; 
    
    [Header("UI & Data")]
    public TextMeshProUGUI uiTextElement; 
    public TextAsset tutorialJsonFile;
    
    [Header("3D Visuals")]
    public GameObject tutorialPrefab; // holds rotating controller display
    public Transform tutorialAnchor; // where it physically spawns

    private Dictionary<string, string> tutorialDictionary = new Dictionary<string, string>();
    private GameObject currentTutorialModel;

    void Awake()
    {
         if (Instance == null)
        {
            Instance = this;
            LoadTutorialData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadTutorialData()
    {
        if (tutorialJsonFile != null)
        {
            TutorialData data = JsonUtility.FromJson<TutorialData>(tutorialJsonFile.text);
            
            foreach (TutorialEntry entry in data.tutorials)
            {
                if (!tutorialDictionary.ContainsKey(entry.id))
                {
                    tutorialDictionary.Add(entry.id, entry.text);
                }
            }
            Debug.Log("Tutorial data loaded successfully. Entries: " + tutorialDictionary.Count);
        }
        else
        {
            Debug.LogError("Tutorial JSON file is not assigned in the Inspector!");
        }
    }

    public void DisplayText(InteractableInterface interactableObject)
    {
        ShowText(interactableObject.GetTutorialID());
    }

    public void ShowText(string id)
    {
        if (tutorialDictionary.TryGetValue(id, out string textToDisplay))
        {
            uiTextElement.text = textToDisplay;

            if (currentTutorialModel == null && tutorialPrefab != null && tutorialAnchor != null)
            {
                currentTutorialModel = Instantiate(tutorialPrefab, tutorialAnchor);
                currentTutorialModel.transform.localPosition = Vector3.zero;
                currentTutorialModel.transform.localRotation = Quaternion.identity;
                currentTutorialModel.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                if (currentTutorialModel.TryGetComponent<TutorialControllerModelDisplay>(out var vrScript))
                {
                    vrScript.enabled = false;
                }

                Renderer[] renderers = currentTutorialModel.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.material.HasProperty("_UIFade"))
                    {
                        renderer.material.SetFloat("_UIFade", 1f);
                    }
                }
            }
        }
        else
        {
            ClearText(); 
            Debug.LogWarning("Tutorial ID not found in JSON: " + id);
        }
    }

    public void HideText()
    {
        ClearText();
    }

    public void ClearText()
    {
        uiTextElement.text = "";
        
        if (currentTutorialModel != null)
        {
            Destroy(currentTutorialModel);
        }
    }
}