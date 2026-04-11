using System;

[Serializable]
public class TutorialItem {
    public string id;
    public string text;
    public int priority;
}

[Serializable]
public class TutorialData {
    public TutorialItem[] tutorials;
}