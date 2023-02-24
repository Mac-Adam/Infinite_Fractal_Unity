using UnityEngine;

public class TextControler : MonoBehaviour
{
    
    TMPro.TMP_Text text;


    void Awake()
    {
        text = GetComponentInChildren<TMPro.TMP_Text>();
    }


    public void SetText(string newText)
    {
        text.text = newText;
    }
}
