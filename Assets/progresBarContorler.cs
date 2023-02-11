using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class progresBarContorler : MonoBehaviour
{
    public int maxWidth;
    public RectTransform rectTransform;
   

    public void setProgres(float progres)
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progres * maxWidth);
        //Debug.Log(progres);
    }
}
