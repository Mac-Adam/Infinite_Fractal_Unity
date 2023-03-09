using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FixedPointNumberSystem;
using Colors;
using CommonFunctions;
using CommonShaderRenderFunctions;
using GuiTemplates;

public class MandelbrotContoroler : ShadeContoler
{
    //Shaders
    public ComputeShader InfiniteShader;
    public ComputeShader DoubleShader;
    public ComputeShader RenderShader;
    public Shader AddShader;

    RenderTexture dummyTexture;
    Material addMaterial;

    //DoubleShader
    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;
    ComputeBuffer MultiFrameRenderBuffer;
 

    //InfiShader
    ComputeBuffer FpMultiframeBuffer;
    ComputeBuffer PossionBuffer;
    int[] TestPosiotnArray;

    //RenderShader
    ComputeBuffer IterBuffer;
    ComputeBuffer OldIterBuffer; //used for upscaling
    float colorStrength = 5;
    const float ColorStrengthMax = 1000;
    const float ColorStrengthMin = 1;
    bool smoothGradient = true;
    ComputeBuffer ColorBuffer;
    bool upscaling;
    int currColorPalette = 0;


    //Shader control
    bool reset = false;
    int shiftX = 0;
    int shiftY = 0;
    int register = 0;



    //gui
    bool guiOn = true;
    bool renderFinished = false;
    public UIControler guiControler;
    UITemplate guiTemplate;
    string tooltip;
    const string DoubleTooltip = @"Controls:
I - Zoom In
O - Zoom Out
L - Change Gradient Type
C - Cycle Color Palette
A - Toggle Antialiasing
G - Toggle GUI";
    const string InfiniteTooltip = @"Controls:
I - Zoom In And Pixelize Image
O - Zoom Out Without Rerendering
U - Upscale Image
L - Change Gradient Type
C - Cycle Color Palette
A - Toggle Antialiasing
G - Toggle GUI";




    //shader settings
    bool infinitePre = false;



    //Anti-Alias
    uint currentSample = 0;
    bool frameFinished = false;
    int currIter = 0;
    bool doAntialasing = false;
    int maxAntiAliasyncReruns = 9;
    private Vector2[] antialiasLookupTable = {
        new Vector2(0,0),
        new Vector2((float)-2/3,(float)-2/3),
        new Vector2((float)-2/3,0),
        new Vector2((float)-2/3,(float)2/3),
        new Vector2(0,(float)2/3),
        new Vector2((float)2/3,(float)2/3),
        new Vector2((float)2/3,0),
        new Vector2((float)2/3,(float)-2/3),
        new Vector2(0,(float)-2/3),

    };


    //Controls
    float scrollSlowness = 10.0f;
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;

    string pixelizationLevelUpControl = "i";
    string pixelizationLevelDownControl = "o";
    string resetControl = "r";
    string togleInterpolationTypeContorl = "l";
    string colorPaletteTogleContorl = "c";
    string antialiasTogleContorl = "a";
    string upscaleControl = "u";
    string guiToggleControl = "g";
    string tempKey = "s";


    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    static int cpuPrecision = GPUCode.precisions[^1].precision+5;
    FixedPointNumber MiddleX = new(cpuPrecision);
    FixedPointNumber MiddleY = new(cpuPrecision);
    FixedPointNumber Scale = new(cpuPrecision);

    //precision
    int maxIter = 1000;
    //constanst are a starting point the other one is dynamicly set based on hardware capabilities
    const int IterPerDoubleCycle = 10;
    const int IterPerInfiniteCycle = 3;
    int IterPerCycle;
    const float minTargetFramerate = 60;
    const float maxTagretFramerate = 200;


    int precisionLevel = 1;
    int shaderPre;
    int shaderPixelSize;


    //Pixelization
    int pixelizationBase = 2;
    int pixelizationLevel = 0;
    int lastPixelizationLevel;
    int preUpscalePixLvl;


    //more descriptive name would be RealScreenPixelsPerRenderPixel
    int PixelsPerPixel()
    {
        return MathFunctions.IntPow(pixelizationBase, pixelizationLevel);
    }
    int LastPixelsPerPixel()
    {
        return MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel);
    }
    int PixelCount()
    {
        return Screen.width * Screen.height / MathFunctions.IntPow(PixelsPerPixel(),2);
    }
    int LastPixelCount()
    {
        return Screen.width * Screen.height / MathFunctions.IntPow(LastPixelsPerPixel(), 2);
    }
    PixelizationData GetPixelizationData()
    {
        return new(PixelsPerPixel(), LastPixelsPerPixel(), PixelCount(), LastPixelCount(), pixelizationBase);
    }

    void ResetIterPerCycle()
    {
        IterPerCycle = infinitePre ? IterPerInfiniteCycle : IterPerDoubleCycle;
    }
    void SetSPrecision(int val)
    {
        val = Math.Clamp(val, 0, GPUCode.precisions.Length - 1);
        precisionLevel = val;
        ResetPrecision();
        DisposeBuffers();
        InitializeBuffers();
        ResetParams();
    }
    void ResetPrecision()
    {

        shaderPre = GPUCode.precisions[precisionLevel].precision;
        shaderPixelSize = 2 * shaderPre + 3;
        TestPosiotnArray = new int[3 * shaderPre];

    }

    void ResetAntialias()
    {
        currentSample = 0;
        currIter = 0;
        frameFinished = false;
        renderFinished = false;
    }
    public void SetPrecision(bool val)
    {
        infinitePre = val;
        tooltip = infinitePre ? InfiniteTooltip : DoubleTooltip;
        ResetParams();
        ResetAntialias();
    }
    public void SetSmoothGradient(bool val)
    {
        smoothGradient = val;
    }
    public void SetAnitialiasing(bool val)
    {
        doAntialasing = val;
        ResetAntialias();
    }
    public void SetColorPalette(int val)
    {
        currColorPalette = val % MyColoringSystem.colorPalettes.Length;
        ColorBuffer.Dispose();
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

    }
    public void SetMaxIter(int iter)
    {
        maxIter = iter;
        ResetParams();
        ResetAntialias();
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



    public override void InitializeBuffers()
    {

        IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
        OldIterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(PixelCount()*2, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
        FpMultiframeBuffer = new ComputeBuffer(PixelCount()*2, sizeof(int) * shaderPixelSize);
        PossionBuffer = new ComputeBuffer(3 * shaderPre, sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

    }
    public override void InitializeValues()
    {
        ResetPrecision();

        MiddleX.SetDouble(middleX);
        MiddleY.SetDouble(middleY);
        Scale.SetDouble(PixelsPerPixel() * length / Screen.width);

        ResetIterPerCycle();
        
        
        addMaterial = new Material(AddShader);
    }
    public override void HandleLastValues()
    {
        lastPixelizationLevel = pixelizationLevel;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
   
    public override void InitializeGui()
    {
        tooltip = infinitePre ? InfiniteTooltip : DoubleTooltip;
        guiTemplate = new UITemplate(
        DefaultTemlates.sizes,
        new List<ToggleTemplate>(){
            new ToggleTemplate(
                "Antialiasing",
                doAntialasing,
                (bool b) => SetAnitialiasing(b)
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
                1,
                10000,
                true,
                (float f)=> SetColorStrenght(f)
                ),
            new SliderTemplate(
                "Max Iterations",
                maxIter,
                1,
                1000000,
                true,
                (float f)=> SetMaxIter(Mathf.FloorToInt(f))
                )
        },
        new List<DropdownTemplate>() {
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
            new TextTemplate(tooltip)
        }

        );
        guiControler.GenerateUI(guiTemplate);
        SetGuiActive(guiOn);

    }
    public override void DisposeBuffers()
    {
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
        ColorBuffer.Dispose();
        FpMultiframeBuffer.Dispose();
        PossionBuffer.Dispose();
    }
    public override void AdditionalCleanup()
    {
        Destroy(dummyTexture);
    }

    public override void HandleKeyInput()
    {
        if (Input.GetKeyDown(tempKey))
        {
            SetSPrecision(precisionLevel + 1);
        }


        if (Input.GetKeyDown(guiToggleControl))
        {
            SetGuiActive(!guiOn);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }
       
        if (Input.GetKeyDown(antialiasTogleContorl))
        {
            SetAnitialiasing(!doAntialasing);
        }
      
        if (Input.GetKeyDown(pixelizationLevelUpControl))
        {
            preUpscalePixLvl = pixelizationLevel;
            pixelizationLevel += 1;
            upscaling = false;
        }
        if (Input.GetKeyDown(pixelizationLevelDownControl))
        {
            if (pixelizationLevel > 0)
            {
                lastPixelizationLevel = pixelizationLevel;
                pixelizationLevel -= 1;
                upscaling = false;
            }
           
        }
        
        if (Input.GetKeyDown(colorPaletteTogleContorl))
        {
            SetColorPalette(currColorPalette + 1);
        }
        if (Input.GetKeyDown(resetControl))
        {
            ResetParams();
            ResetAntialias();
        }
        if (Input.GetKeyDown(togleInterpolationTypeContorl))
        {
            SetSmoothGradient(!smoothGradient);
        }
        if (Input.GetKeyDown(upscaleControl))
        {

            if (pixelizationLevel > 0)
            {
                int[] arr = new int[PixelCount() * 3];
                IterBuffer.GetData(arr);
                OldIterBuffer.Dispose();
                OldIterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
                OldIterBuffer.SetData(arr);
                IterBuffer.Dispose();


                preUpscalePixLvl = pixelizationLevel;
                pixelizationLevel -= 1;

                IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
                FixedPointNumber scaleFixer = new(cpuPrecision);
                scaleFixer.SetDouble((double)PixelsPerPixel() / LastPixelsPerPixel());
                Scale *= scaleFixer;
                upscaling = true;
                renderFinished = false;
                currIter = 0;
            }

        }
    }
    public override void HandleScreenSizeChange()
    {
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height || lastPixelizationLevel != pixelizationLevel)
        {
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(PixelCount(), sizeof(int) * 2 + sizeof(float));
            if (lastPixelizationLevel != pixelizationLevel && !upscaling)
            {
               
                PixelizedShaders.HandleZoomPixelization<int>(FpMultiframeBuffer, sizeof(int), lastPixelizationLevel < pixelizationLevel, GetPixelizationData(), register, (ComputeBuffer buffer) => { FpMultiframeBuffer = buffer; }, shaderPixelSize);
                PixelizedShaders.HandleZoomPixelization<DoublePixelPacket>(MultiFrameRenderBuffer, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2, lastPixelizationLevel < pixelizationLevel, GetPixelizationData(), register, (ComputeBuffer buffer) => { MultiFrameRenderBuffer = buffer; });
                
            }
            else
            {
                MultiFrameRenderBuffer.Dispose();
                MultiFrameRenderBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
                FpMultiframeBuffer.Dispose();
                FpMultiframeBuffer = new ComputeBuffer(PixelCount() * 2, sizeof(int) * shaderPixelSize);
                reset = true;
            }

           
            
           
           



        }

    }
    public override void HandleMouseInput()
    {
        if(guiOn &&Input.mousePosition.x > Screen.width - guiTemplate.sizes.width)
        {
            return;
        }
        if(Screen.width != PrevScreenX || Screen.height != PrevScreenY)
        {
            return;
        }


        Vector2 mousePosPix = Input.mousePosition;
        int mouseTextureCoordinatesX = (int)mousePosPix.x / PixelsPerPixel();
        int mouseTextureCoordinatesY = (int)mousePosPix.y / PixelsPerPixel();


        FixedPointNumber mousePosRealX = new(cpuPrecision);

        mousePosRealX.SetDouble(mouseTextureCoordinatesX - Screen.width / (2 * PixelsPerPixel()));
        mousePosRealX = mousePosRealX * Scale + MiddleX;
        FixedPointNumber mousePosRealY = new(cpuPrecision);

        mousePosRealY.SetDouble(mouseTextureCoordinatesY - Screen.height / (2 * PixelsPerPixel()));
        mousePosRealY = mousePosRealY * Scale + MiddleY;
        FixedPointNumber multiplyer = new(cpuPrecision);
        if (Input.mouseScrollDelta.y != 0)
        {
           
            double scaleDifference = 1 - Input.mouseScrollDelta.y / scrollSlowness;
            multiplyer.SetDouble(scaleDifference);
            Scale *= multiplyer;

            FixedPointNumber differenceX = mousePosRealX - MiddleX;
            FixedPointNumber differenceY = mousePosRealY - MiddleY;
            multiplyer.SetDouble(1.0 - scaleDifference);
            MiddleX += differenceX * multiplyer;
            MiddleY += differenceY * multiplyer;
            ResetParams();
            

        }
        if (mouseTextureCoordinatesX != oldMouseTextureCoordinatesX || mouseTextureCoordinatesY != oldMouseTextureCoordinatesY)
        {
            if (Input.GetMouseButton(0))
            {
                ResetAntialias();

                shiftX = mouseTextureCoordinatesX - oldMouseTextureCoordinatesX;
                shiftY = mouseTextureCoordinatesY - oldMouseTextureCoordinatesY;

                register = (register + 1) % 2;
                
                multiplyer.SetDouble(mouseTextureCoordinatesX - oldMouseTextureCoordinatesX);
                MiddleX -= multiplyer * Scale;
                multiplyer.SetDouble(mouseTextureCoordinatesY - oldMouseTextureCoordinatesY);
                MiddleY -= multiplyer * Scale;
                ResetParams();

            }

        }
        oldMouseTextureCoordinatesX = mouseTextureCoordinatesX;
        oldMouseTextureCoordinatesY = mouseTextureCoordinatesY;


    }
    public override void HandleGuiUpdates()
    {
        guiControler.UpdateUI(
            new List<bool>() {
                doAntialasing,
                smoothGradient
            },
            new List<float>()
            {
                colorStrength,
                maxIter
            },
            new List<int>()
            {
                currColorPalette
            },
            new List<float>()
            {
                renderFinished ? 1 : currIter/(float)maxIter,
                renderFinished ? 1 : currentSample/(float)maxAntiAliasyncReruns
            },
            new List<string>()
            {
                tooltip
            }
         );



    }
    public override void SetShadersParameters()
    {

        for (int i = 0; i < shaderPre; i++)
        {
            TestPosiotnArray[i] = MiddleX.digits[i];
            TestPosiotnArray[shaderPre + i] = MiddleY.digits[i];
            TestPosiotnArray[shaderPre * 2 + i] = Scale.digits[i];

        }

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = Scale.ToDouble() * Screen.width / PixelsPerPixel();
        doubleDataArray[1] = MiddleX.ToDouble();
        doubleDataArray[2] = MiddleY.ToDouble();
        doubleDataBuffer.SetData(doubleDataArray);
        ColorBuffer.SetData(MyColoringSystem.colorPalettes[currColorPalette].colors);

        if (infinitePre)
        {
            GPUCode.ResetAllKeywords();
            Shader.EnableKeyword(GPUCode.precisions[precisionLevel].name);

            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
            InfiniteShader.SetInt("_MaxIter", maxIter);
            InfiniteShader.SetBool("_reset", reset);
            InfiniteShader.SetInt("_pixelizationBase", pixelizationBase);
            InfiniteShader.SetInt("_ShiftX", shiftX);
            InfiniteShader.SetInt("_ShiftY", shiftY);
            InfiniteShader.SetInt("_Register", register);
            InfiniteShader.SetInt("_IterPerCycle", IterPerCycle);
            InfiniteShader.SetBuffer(0, "_IterBuffer", IterBuffer);
            
        }
        else
        {
            DoubleShader.SetBuffer(0, "_DoubleDataBuffer", doubleDataBuffer);
            DoubleShader.SetBuffer(0, "_MultiFrameData", MultiFrameRenderBuffer);
            DoubleShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
            DoubleShader.SetInt("_MaxIter", maxIter);
            DoubleShader.SetBool("_reset", reset);
            DoubleShader.SetInt("_ShiftX", shiftX);
            DoubleShader.SetInt("_ShiftY", shiftY);
            DoubleShader.SetInt("_Register", register);
            DoubleShader.SetInt("_IterPerCycle", IterPerCycle);
            DoubleShader.SetBuffer(0, "_IterBuffer", IterBuffer);


        }
        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetBuffer(0, "_OldIterBuffer", OldIterBuffer);
        RenderShader.SetInt("_MaxIter", maxIter);
        RenderShader.SetFloat("_ColorStrength", colorStrength);
        RenderShader.SetBool("_Smooth", smoothGradient);
        RenderShader.SetBool("_Upscaling", upscaling);
        RenderShader.SetInt("_Type", MyColoringSystem.colorPalettes[currColorPalette].gradientType);
        RenderShader.SetInt("_PixelWidth", PixelsPerPixel());
        RenderShader.SetInt("_OldPixelWidth", MathFunctions.IntPow(pixelizationBase, preUpscalePixLvl));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer);
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[currColorPalette].length);
        reset = false;
        shiftX = 0;
        shiftY = 0;

    }

    public override void ResetParams()
    {
        reset = true;
        currentSample = 0;
        currIter = 0;
        upscaling = false;
        renderFinished = false;
        renderFinished = false;
        ResetIterPerCycle();
    }

    public override void AutomaticParametersChange()
    {
        if (!frameFinished && !renderFinished)
        {
            if (1 / Time.deltaTime > maxTagretFramerate)
            {
                IterPerCycle++;
            }
            else if (1 / Time.deltaTime < minTargetFramerate)
            {
                if (IterPerCycle > 1)
                {
                    IterPerCycle--;
                }

            }
        }
        int tagretPrecison = 0;
        foreach(int digit in Scale.digits)
        {
            if (digit == 0)
            {
                tagretPrecison++;
            }
            else
            {
                break;
            }
        }
        if(tagretPrecison + 1>= GPUCode.precisions[precisionLevel].precision)
        {
            SetSPrecision(precisionLevel + 1);
            if (!infinitePre)
            {
                SetPrecision(true);
            }
        }
        else if(precisionLevel != 0)
        {
            if(tagretPrecison + 1 < GPUCode.precisions[precisionLevel - 1].precision)
            {
                SetSPrecision(precisionLevel - 1);
            }
        }
    }

    public override void HandleAntialias()
    {

        if (!renderFinished)
        {
            currIter += IterPerCycle;
        }
        

        if (doAntialasing)
        {
            if (currentSample < maxAntiAliasyncReruns)
            {
                if (currIter > maxIter)
                {

                    frameFinished = true;
                    currIter = 0;
                }

            }
            else
            {
                renderFinished = true;
            }
        }
        else if (currIter > maxIter)
        {
            renderFinished = true;
        }

    }

    public override void AddiitionalTextureRegenerationHandeling()
    {
        currentSample = 0;
  
    }
    public override bool ShouldRegerateTexture()
    {
        return lastPixelizationLevel != pixelizationLevel;
    }
    public override void InitializeOtherTextures()
    {
        dummyTexture = PixelizedShaders.InitializePixelizedTexture(dummyTexture, pixelizationBase, pixelizationLevel, ShouldRegerateTexture());
    }
    public override void DispatchShaders()
    {

        PixelizedShaders.Dispatch(RenderShader, infinitePre ? InfiniteShader : DoubleShader, targetTexture, dummyTexture, pixelizationBase, pixelizationLevel);
    }

    public override void BlitTexture(RenderTexture destination)
    {
        Antialiasing.BlitWitthAntialiasing(currentSample, frameFinished, destination, targetTexture, addMaterial,
            () =>
            {
                frameFinished = false;
                currentSample++;
                reset = true;
            });

    }

}
