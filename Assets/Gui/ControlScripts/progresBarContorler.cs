using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgresBarContorler : MonoBehaviour
{
    public Image mask;
    TMPro.TMP_Text text;


    void Awake()
    {
        text = GetComponentInChildren<TMPro.TMP_Text>();
    }


    public void SetText(string newText)
    {
        text.text = newText;
    }
    public void SetProgres(float progres)
    {
        mask.fillAmount = progres;
    }
}
