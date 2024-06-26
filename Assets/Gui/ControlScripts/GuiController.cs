using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Colors;
using CommonFunctions;
using CommonShaderRenderFunctions;
using GuiTemplates;

public class GuiController : MonoBehaviour
{
    /*
     * This script handles all the gui related stuff, plus all the visual settings
     * This script can't do all the necesary things to make the controls work, therefore
     * if sets public flags if somethging is requested by the user, for example ScreenShot.
     * Other conroll script should hande the request and set the flag back to false
     * 
     * Not yet sure if this is the right aproach but this script keeps track of the values
     * getters and setters might be a good idea, this will bloat the code tho, so I am not too sure about that.
     */

    //gui
    public bool guiOn = true;
    public UIControler guiControler;
    //probably should not be pubic in the long run, right now its the easiest fix
    public UITemplate guiTemplate;
    string generalInfo;
    const string tooltip = @"Controls:
    J - Julia Set
    F - Fractal change
    I/O - Zoom In/Out
    U/D - Up/Donwscale Image
    Z - Make a zoom in video
    S - Make a Screenshot
    L - Smooth Gradient
    P - Interpolation Type
    C - Cycle Color Palette
    A - Toggle Antialiasing
    G - Toggle GUI";



    //Anti-Alias
    public bool changedAntialias = false;
 
    string resetKey = "r";
    string togleInterpolationTypeKey = "l";
    string colorPaletteTogleKey = "c";
    string antialiasTogleKey = "a";

    string guiToggleKey = "g";
    string scrennShotKey = "s";
    string zoomVideoKey = "z";

    string upscaleKey = "u";
    string downscaleKey = "d";
    string pixelizationLevelUpKey = "i";
    string pixelizationLevelDownKey = "o";
    string interpoaltionKey = "p";

    string fractalCycleKey = "f";



    //precision
    public int maxIter = 1000;
    public bool changedMaxIter = false;
    public int bailoutRadius = 128;
    public bool changedBailoutRadius = false;

    //Pixelization
    public int pixelizationChange = 0; // by how much should the pixelization be changed
 

    public bool requestedUpscale = false;
    public bool requestedDownscale = false;

    public float colorStrength = 5;
    const float ColorStrengthMax = 1000;
    const float ColorStrengthMin = 1;
    public bool smoothGradient = true;
    public bool changedSmoothGradient = false;
    public int currColorPalette = 0;
    public int lastColorPalette = 0;
    public int currFractal = 0;
    public int lastFractal = 0;


    public int requestedFrankensteinLevel = 1;
    public bool changedFrankenstein = false;

    public bool requestingSS = false;

    public bool requestedZoomVid = false;

    public bool resetRequested = false;

    public int interpolationType = 2;
    int interpoalationTypeN = 4;

    public float angle = 4;




    //this module is not responsible for keeping track of it, it simply displays it
    public Settings settings;
    public DynamicSettings dynamicSettings;


    public void SetSmoothGradient(bool val)
    {
        changedSmoothGradient = true;
        smoothGradient = val;
    }
    public void SetColorPalette(int val)
    {
        if (val % MyColoringSystem.colorPalettes.Length == currColorPalette)
        {
            return;
        }

        lastColorPalette = currColorPalette;
        currColorPalette = val % MyColoringSystem.colorPalettes.Length;
    }

    public void SetFractal(int val)
    {
        if (val % PixelizedShaders.fractalInfos.Length == currFractal)
        {
            return;
        }

        lastFractal = currFractal;
        currFractal = val % PixelizedShaders.fractalInfos.Length;
    }
    public void SetMaxIter(int iter)
    {
        changedMaxIter = true;
        maxIter = iter;
    }
    public void SetBailoutRadius(int radius)
    {
        changedBailoutRadius = true;
        bailoutRadius = radius;
    }

    void SetAntialias(bool val)
    {
        if (val != settings.doAntialasing)
        {
            changedAntialias = true;
        } 
    }

    public void SetGuiActive(bool val)
    {
        guiOn = val;
        guiControler.SetEnable(val);
    }
    public void SetColorStrenght(float val)
    {
        colorStrength = Mathf.Clamp(val, ColorStrengthMin, ColorStrengthMax);
    }

    public void SetFrankensteinLevel(int level)
    {
        int steps = OtherFunctions.IntPow(2, level);
        if (steps == settings.frankensteinSteps)
        {
            return;
        }
        requestedFrankensteinLevel = level;
        changedFrankenstein = true;
    }

    public void SetInterpolation(int inter)
    {
        interpolationType = inter % interpoalationTypeN;
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void InitializeGui()
    {
        guiTemplate = new UITemplate(
        DefaultTemlates.sizes,
        new List<ToggleTemplate>(){
            new ToggleTemplate(
                "Antialiasing",
                settings.doAntialasing,
                (bool b) => SetAntialias(b)
                ),
            new ToggleTemplate(
                "Smooth Gradient",
                smoothGradient,
                (bool b) => SetSmoothGradient(b)
                )
            },
        new List<SliderTemplate>()
        {
            new SliderTemplate(
                "Color Strenght",
                colorStrength,
                ColorStrengthMin,
                ColorStrengthMax,
                true,
                (float f)=> SetColorStrenght(f)
                ),
            new SliderTemplate(
                "Max Iterations",
                maxIter,
                1,
                100000,
                true,
                (float f)=> SetMaxIter(Mathf.FloorToInt(f))
                ),
             new SliderTemplate(
                "Tiles",
                (float)Math.Log(settings.frankensteinSteps)/(float)Math.Log(2.0),//basicly log2
                0,
                6,
                false,
                (float f)=> SetFrankensteinLevel(Mathf.FloorToInt(f))
                ),
        },
        new List<DropdownTemplate>() {
            new DropdownTemplate(
                "Fractal",
                currFractal,
                PixelizedShaders.fractalInfos.Select(sn => sn.outsideName).ToList(),
                (int i)=> SetFractal(i)
                ),
            new DropdownTemplate(
                "Color Palette",
                currColorPalette,
                MyColoringSystem.colorPalettes.Select(palette => palette.name).ToList(),
                (int i)=> SetColorPalette(i)
                )
        },
        new List<ProgressBarTemplate>()
        {
            new ProgressBarTemplate(
                "Frame progress",
                0
                ),
            new ProgressBarTemplate(
                "Render progress",
                0
                )
        },
        new List<ButtonTemplate>()
        {
            new ButtonTemplate(
                "Hide GUI",
                ()=>SetGuiActive(false)
                ),
            new ButtonTemplate(
                "Exit",
                ()=>Exit()
                )
        },
        new List<TextTemplate>()
        {
            new TextTemplate(tooltip),
            new TextTemplate("")
        }

        );
        guiControler.GenerateUI(guiTemplate);
        SetGuiActive(guiOn);

    }


    public void HandleKeyInput()
    {
        if (Input.GetKeyDown(scrennShotKey))
        {
            requestingSS = true;
        }

        if (Input.GetKeyDown(zoomVideoKey))
        {
            requestedZoomVid = true;
        }

        if (Input.GetKeyDown(guiToggleKey))
        {
            SetGuiActive(!guiOn);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }

        if (Input.GetKeyDown(antialiasTogleKey))
        {
            SetAntialias(!settings.doAntialasing);
        }

 

        if (Input.GetKeyDown(colorPaletteTogleKey))
        {
            SetColorPalette(currColorPalette + 1);
        }
        if (Input.GetKeyDown(resetKey))
        {
            resetRequested = true;           
        }
        if (Input.GetKeyDown(togleInterpolationTypeKey))
        {
            SetSmoothGradient(!smoothGradient);
        }
        if (Input.GetKeyDown(pixelizationLevelUpKey))
        {
            pixelizationChange += 1;
        }
        if (Input.GetKeyDown(pixelizationLevelDownKey))
        {
            pixelizationChange -= 1;

        }
        if (Input.GetKeyDown(upscaleKey))
        {
            requestedUpscale = true;
            
        }
        if (Input.GetKeyDown(downscaleKey))
        {
            requestedDownscale = true;

        }
        if (Input.GetKeyDown(fractalCycleKey))
        {
            SetFractal(currFractal + 1);
        }
        if (Input.GetKeyDown(interpoaltionKey))
        {
            SetInterpolation(interpolationType + 1);
        }

    }

    public void HandleGuiUpdates()
    {
        //TODO make it more acessable
        if (Input.GetKey("v"))
        {
            angle = (angle + Time.deltaTime*1.5f) % (2 * 3.1415f);
        }
        



        string precisionText = settings.precision == Precision.FLOAT ? "float" : settings.precision == Precision.DOUBLE ? "double" : $"infine with precision {settings.precisionLevel}";
        string timeElapsed = String.Format("{0:0.000}", dynamicSettings.renderTimeElapsed);
        float RenderComplete = (float)(settings.frankensteinY * settings.frankensteinSteps + settings.frankensteinX) / (settings.frankensteinSteps * settings.frankensteinSteps);
        if (settings.doAntialasing)
        {
            RenderComplete += settings.currentSample;
        }
        float wholeRender = settings.doAntialasing ? settings.maxAntiAliasyncReruns : 1.0f;
        //Debug.Log($"RC:{RenderComplete} WR:{wholeRender}, CI: {dynamicSettings.currIter} MI:{maxIter}, RF:{dynamicSettings.renderFinished}");
        generalInfo = @$"
Rendering {Screen.width} x {Screen.height}
Calculating {settings.ReducedWidth(false)} x {settings.ReducedHeight(false)}
Numer System: {precisionText}
Last Frame rendered in: {timeElapsed}s";
        guiControler.UpdateUI(
            new List<bool>() {
                settings.doAntialasing,
                smoothGradient 
            },
            new List<float>()
            {
                colorStrength,
                maxIter,
                (float)Math.Log(settings.frankensteinSteps)/(float)Math.Log(2.0)
            },
            new List<int>()
            {
                currFractal,
                currColorPalette
            },
            new List<float>()
            {
                dynamicSettings.renderFinished ? 1 : dynamicSettings.currIter/(float)maxIter,
                dynamicSettings.renderFinished ? 1 : RenderComplete/wholeRender
            },
            new List<string>()
            {
                tooltip,
                generalInfo
            }
         );



    }

    void Start()
    {
        guiControler = GameObject.Find("BlankCanvas").GetComponent<UIControler>();
        InitializeGui();
    }

    void Update()
    {
        HandleKeyInput();
        HandleGuiUpdates();
    }
}
