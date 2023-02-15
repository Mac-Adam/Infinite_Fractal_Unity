using System;
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
    private List<GameObject> toggleList = new();
    private List<GameObject> sliderList = new();

    private float currentY = 0;

    private float margin = 10; 

    struct ToggleTemplate
    {
        public string text;
        public bool startingValue;
        public Action<bool> callback;

        public ToggleTemplate(string t,bool s, Action<bool> c)
        {
            text = t;
            startingValue = s;
            callback = c;
        }
    }
    struct SliderTemplate
    {
        public string text;
        public float startingValue;
        public float min;
        public float max;
        public bool log;
        public Action<float> callback;

        public SliderTemplate(string t, float s, float minval,float maxval,bool l,Action<float> c)
        {
            text = t;
            startingValue = s;
            min = minval;
            max = maxval;
            log = l;
            callback = c;

        }
    }

    struct Sizes
    {
        public float width;
        public Vector2 toggleSize;
        public Vector2 sliderSize;

        public Sizes (float w, Vector2 tog,Vector2 slid)
        {
            width = w;
            toggleSize = tog;
            sliderSize = slid;
        }
    }

    struct UITemplate
    {
        public Sizes sizes;
        public List<ToggleTemplate> toggleTemplates;
        public List<SliderTemplate> sliderTemplates;


        public UITemplate(Sizes s,List<ToggleTemplate> tt,List<SliderTemplate> st)
        {
            sizes = s;
            toggleTemplates = tt;
            sliderTemplates = st;
        }  

    }
    



    // Start is called before the first frame update
    void Start()
    {
        currentY -= margin;
        GenerateUI(new UITemplate(
            new Sizes(300, new Vector2(250, 40), new Vector2(250,40)),
            new List<ToggleTemplate>()
            {
                new ToggleTemplate(
                "On", true, (bool b) => {
                    Debug.Log($"fist one is now {b}");

                }

                ), new ToggleTemplate(
                "Off", false, (bool b) => {
                    Debug.Log($"second one is now {b}");

                }

                )

            },
            new List<SliderTemplate>() {
                new SliderTemplate(
                    "Non log",
                    0.4f,
                    0,1,
                    false,
                    (float f) =>
                    {
                        Debug.Log($"Non log is now {f}");
                    }

                    ),
                new SliderTemplate(
                    "log",
                    100,
                    1,100000,
                    true,
                    (float f) =>
                    {
                        Debug.Log($"log is now {f}");
                    }

                    )
            }


            )); ;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void GenerateUI(UITemplate template)
    {
        background = GenerateComponentFromPrefab(backgroundPrefab, transform, new Vector2(template.sizes.width,0),Vector2.zero);
        foreach(ToggleTemplate toggleTemplate in template.toggleTemplates)
        {
            GameObject newToggle = GenerateComponentFromPrefab(TogglePrefab, background.transform, template.sizes.toggleSize, new Vector2(0, currentY));

            ToggleControler toggleConroler = newToggle.GetComponent<ToggleControler>();
            toggleConroler.SetText(toggleTemplate.text);
            toggleConroler.SetValue(toggleTemplate.startingValue);
            toggleConroler.AddListner(toggleTemplate.callback);

            toggleList.Add(newToggle);

        }
        foreach (SliderTemplate sliderTemplate in template.sliderTemplates)
        {
            GameObject newSlider = GenerateComponentFromPrefab(SliderPrefab, background.transform, template.sizes.sliderSize, new Vector2(0, currentY));

            SliderControler sliderControler = newSlider.GetComponent<SliderControler>();
            sliderControler.SetUp(sliderTemplate.min, sliderTemplate.max, sliderTemplate.log);
            sliderControler.SetText(sliderTemplate.text);
            sliderControler.SetValue(sliderTemplate.startingValue);
            sliderControler.AddListner(sliderTemplate.callback);

            sliderList.Add(newSlider);

        }





    }


    GameObject GenerateComponentFromPrefab(GameObject prefab,Transform parent,Vector2 size,Vector2 pos )
    {
        GameObject component = Instantiate(prefab);
        component.transform.SetParent(parent);
        RectTransform componentRectTransform = component.GetComponent<RectTransform>();
        componentRectTransform.anchoredPosition = pos;
        componentRectTransform.sizeDelta = size;

        currentY -= size.y + margin;

        return component;



    }
}
