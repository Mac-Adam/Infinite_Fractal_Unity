using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControler : MonoBehaviour
{
    public GameObject backgroundPrefab;
    public GameObject buttonPrefab;
    public GameObject DropdownPrefab;
    public GameObject TogglePrefab;
    public GameObject SliderPrefab;

    private GameObject background;




    // Start is called before the first frame update
    void Start()
    {
        GenerateUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void GenerateUI()
    {
        background = Instantiate(backgroundPrefab);
        background.transform.SetParent(transform); 

    }
}
