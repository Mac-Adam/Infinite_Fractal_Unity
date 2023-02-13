using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownControler : MonoBehaviour
{
    TMPro.TMP_Dropdown dropdown;

    public MandelbrotContoroler GameMasterComponent;

    void Start()
    {
        dropdown = GetComponent<TMPro.TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(delegate
        {
            DropdownValueChanged(dropdown);
        });
    }
    void DropdownValueChanged(TMPro.TMP_Dropdown change)
    {
        GameMasterComponent.SetColorPalette(change.value);

    }
}
