using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonControler : MonoBehaviour
{
    Button button;
    TMPro.TMP_Text text;


    void Awake()
    {
        button = GetComponent<Button>();
        text = GetComponentInChildren<TMPro.TMP_Text>();
    }


    public void SetText(string newText)
    {
        text.text = newText;
    }
    public void AddListner(Action callback)
    {
        button.onClick.AddListener(delegate
        {
            callback();
        });
    }


}
