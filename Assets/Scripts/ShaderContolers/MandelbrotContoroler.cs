using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FixedPointNumberSystem;
using Colors;
using CommonFunctions;
using CommonShaderRenderFunctions;

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
    ComputeBuffer LastMultiFrameRenderBuffer;
    ComputeBuffer PossionBuffer;
    int[] TestPosiotnArray = new int[3 * shaderPre];

    //RenderShader
    ComputeBuffer IterBuffer;
    ComputeBuffer OldIterBuffer;
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
    bool pixelized = false;



    //gui
    public GameObject gui;
    bool guiOn = false;
    bool renderFinished = false;
    int guiWidth = 300;
    public Toggle precisionToggle;
    public Toggle smoothGradientToggle;
    public Toggle antialiasingToggle;
    public Slider colorStrengthSlider;
    public Slider maxIterSlider;
    public TMPro.TMP_Dropdown colorPaletteDropdown;
    public progresBarContorler iterProgresBarControler;

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
    string toggleShaderControl = "t";
    string resetControl = "r";
    string maxIterContorl = "p";
    string togleInterpolationTypeContorl = "l";
    string colorPaletteTogleContorl = "c";
    string antialiasTogleContorl = "a";
    string upscaleControl = "u";
    string guiToggleControl = "g";


    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    FixedPointNumber MiddleX = new FixedPointNumber(fpPre);
    FixedPointNumber MiddleY = new FixedPointNumber(fpPre);
    FixedPointNumber Scale = new FixedPointNumber(fpPre);

    //precision
    int maxIter = 1000;
    const int IterPerDoubleCycle = 10;
    const int IterPerInfiniteCycle = 5;
    const int shaderPre = 8;
    const int fpPre = shaderPre * 2;
    const int shaderPixelSize = 2 * shaderPre + 3;
    int IterPecCycle;


    //Pixelization
    int pixelizationBase = 2;
    int pixelizationLevel = 0;
    int lastPixelizationLevel;
    int preUpscalePixLvl;


    public override void InitializeBuffers()
    {

        IterBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
        OldIterBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
        LastMultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
        FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
        PossionBuffer = new ComputeBuffer(3 * shaderPre, sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

    }
    public override void InitializeValues()
    {
        MiddleX.setDouble(middleX);
        MiddleY.setDouble(middleY);
        Scale.setDouble(MathFunctions.IntPow(pixelizationBase, pixelizationLevel) * length / Screen.width);

        IterPecCycle = infinitePre ? IterPerInfiniteCycle : IterPerDoubleCycle;


        
        addMaterial = new Material(AddShader);
        
    }
    public override void HandleLastValues()
    {
        lastPixelizationLevel = pixelizationLevel;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
    public override void ResetParams()
    {
        reset = true;
        currentSample = 0;
        currIter = 0;
        upscaling = false;
        renderFinished = false;
    }
    public override void InitializeGui()
    {
        precisionToggle.isOn = infinitePre;
        antialiasingToggle.isOn = doAntialasing;
        smoothGradientToggle.isOn = smoothGradient;
        colorStrengthSlider.maxValue = Mathf.Log10(ColorStrengthMax);
        colorStrengthSlider.minValue = Mathf.Log10(ColorStrengthMin);
        colorStrengthSlider.value = Mathf.Log10(colorStrength);

        colorPaletteDropdown.options.Clear();
        foreach (ColorPalette palete in MyColoringSystem.colorPalettes)
        {
            colorPaletteDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData() { text = palete.name });
        }
        colorPaletteDropdown.value = currColorPalette;
        maxIterSlider.value = Mathf.Log10(maxIter);
        SetGuiActive(guiOn);
        iterProgresBarControler.setProgres(0);
    }
    public override void DisposeBuffers()
    {
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
        LastMultiFrameRenderBuffer.Dispose();
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

        if (Input.GetKeyDown(guiToggleControl))
        {
            SetGuiActive(!guiOn);

        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Exit();
        }
        if (Input.GetKeyDown(pixelizationLevelUpControl))
        {
            preUpscalePixLvl = pixelizationLevel;
            pixelizationLevel += 1;
            pixelized = true;
        }
        if (Input.GetKeyDown(antialiasTogleContorl))
        {
            SetAnitialiasing(!doAntialasing);
            antialiasingToggle.isOn = doAntialasing;
        }
        if (Input.GetKeyDown(pixelizationLevelDownControl))
        {
            if (pixelizationLevel > 0)
            {
                lastPixelizationLevel = pixelizationLevel;
                pixelizationLevel -= 1;
            }
        }
        if (Input.GetKeyDown(toggleShaderControl))
        {
            SetPrecision(!infinitePre);
            precisionToggle.isOn = infinitePre;
        }
        if (Input.GetKeyDown(colorPaletteTogleContorl))
        {
            SetColorPalette(currColorPalette + 1);
            colorPaletteDropdown.value = currColorPalette;
        }
        if (Input.GetKeyDown(resetControl))
        {
            ResetParams();
            resetAntialias();
        }
        if (Input.GetKeyDown(togleInterpolationTypeContorl))
        {
            SetSmoothGradient(!smoothGradient);
            smoothGradientToggle.isOn = smoothGradient;
        }
        if (Input.GetKeyDown(upscaleControl))
        {

            if (pixelizationLevel > 0)
            {
                int[] arr = new int[Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2) * 3];
                IterBuffer.GetData(arr);
                OldIterBuffer.Dispose();
                OldIterBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
                OldIterBuffer.SetData(arr);
                IterBuffer.Dispose();


                preUpscalePixLvl = pixelizationLevel;
                pixelizationLevel -= 1;

                IterBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
                FixedPointNumber scaleFixer = new FixedPointNumber(fpPre);
                scaleFixer.setDouble((double)MathFunctions.IntPow(pixelizationBase, pixelizationLevel) / (double)MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel));
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
            reset = true;
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
            MultiFrameRenderBuffer.Dispose();
            MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
            LastMultiFrameRenderBuffer.Dispose();
            LastMultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
            if (lastPixelizationLevel != pixelizationLevel && !upscaling)
            {
                int[] oldArr = new int[Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel), 2) * shaderPixelSize]; ;
                int[] newArr;
                int dataWidth;
                int dataHeigth;
                int otherWidth;
                int otherHeigth;
                if (lastPixelizationLevel < pixelizationLevel)//zoom in
                {
                    newArr = new int[Screen.width * Screen.height / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2) * shaderPixelSize];
                    dataWidth = Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);
                    dataHeigth = Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);

                    otherWidth = Screen.width / MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel);
                    otherHeigth = Screen.height / MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel);

                }
                else //zoom out
                {

                    newArr = new int[Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2) * shaderPixelSize];


                    otherWidth = Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);
                    otherHeigth = Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);


                    dataWidth = Screen.width / MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel);
                    dataHeigth = Screen.height / MathFunctions.IntPow(pixelizationBase, lastPixelizationLevel);
                }

                FpMultiframeBuffer.GetData(oldArr);

                int cornerX = dataWidth * (pixelizationBase - 1) / 2;
                int cornerY = dataHeigth * (pixelizationBase - 1) / 2;
                for (int x = 0; x < dataWidth; x++)
                {
                    for (int y = 0; y < dataHeigth; y++)
                    {

                        int smallId = x + y * dataWidth;
                        int bigId = x + cornerX + (y + cornerY) * otherWidth;
                        bigId += otherWidth * otherHeigth * register;
                        if (lastPixelizationLevel > pixelizationLevel)
                        {
                            smallId += dataWidth * dataHeigth * register;
                        }

                        smallId *= shaderPixelSize;
                        bigId *= shaderPixelSize;
                        for (int i = 0; i < shaderPixelSize; i++)
                        {
                            if (lastPixelizationLevel > pixelizationLevel)
                            {
                                newArr[bigId + i] = oldArr[smallId + i];
                            }
                            else
                            {
                                newArr[smallId + i] = oldArr[bigId + i];
                            }
                        }
                    }
                }
                FpMultiframeBuffer.Dispose();
                FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);

                if (lastPixelizationLevel > pixelizationLevel)
                {
                    reset = false;
                    FpMultiframeBuffer.SetData(newArr);
                }
                else
                {
                    LastMultiFrameRenderBuffer.SetData(newArr);
                }

            }
            else
            {
                FpMultiframeBuffer.Dispose();
                FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / MathFunctions.IntPow(MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
            }



        }

    }
    public override void HandleMouseInput()
    {
        if(guiOn && Input.mousePosition.x > Screen.width - guiWidth)
        {
            return;
        }


        Vector2 mousePosPix = Input.mousePosition;
        int mouseTextureCoordinatesX = (int)mousePosPix.x / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);
        int mouseTextureCoordinatesY = (int)mousePosPix.y / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);


        FixedPointNumber mousePosRealX = new FixedPointNumber(fpPre);

        mousePosRealX.setDouble(mouseTextureCoordinatesX - Screen.width / (2 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));
        mousePosRealX = mousePosRealX * Scale + MiddleX;
        FixedPointNumber mousePosRealY = new FixedPointNumber(fpPre);

        mousePosRealY.setDouble(mouseTextureCoordinatesY - Screen.height / (2 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));
        mousePosRealY = mousePosRealY * Scale + MiddleY;
        FixedPointNumber multiplyer = new FixedPointNumber(fpPre);
        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(maxIterContorl))
            {
                maxIter -= maxIter * (int)Input.mouseScrollDelta.y / 4;
                ResetParams();
            }
            else
            {
                double scaleDifference = 1 - Input.mouseScrollDelta.y / scrollSlowness;
                multiplyer.setDouble(scaleDifference);
                Scale *= multiplyer;

                FixedPointNumber differenceX = mousePosRealX - MiddleX;
                FixedPointNumber differenceY = mousePosRealY - MiddleY;
                multiplyer.setDouble(1.0 - scaleDifference);
                MiddleX += differenceX * multiplyer;
                MiddleY += differenceY * multiplyer;
                ResetParams();
            }

        }
        if (mouseTextureCoordinatesX != oldMouseTextureCoordinatesX || mouseTextureCoordinatesY != oldMouseTextureCoordinatesY)
        {
            if (Input.GetMouseButton(0))
            {
                resetAntialias();

                shiftX = (int)(mouseTextureCoordinatesX - oldMouseTextureCoordinatesX);
                shiftY = (int)(mouseTextureCoordinatesY - oldMouseTextureCoordinatesY);

                if (register == 0)
                {
                    register = 1;
                }
                else
                {
                    register = 0;
                }
                multiplyer.setDouble(mouseTextureCoordinatesX - oldMouseTextureCoordinatesX);
                MiddleX -= multiplyer * Scale;
                multiplyer.setDouble(mouseTextureCoordinatesY - oldMouseTextureCoordinatesY);
                MiddleY -= multiplyer * Scale;
                ResetParams();

            }

        }
        oldMouseTextureCoordinatesX = mouseTextureCoordinatesX;
        oldMouseTextureCoordinatesY = mouseTextureCoordinatesY;


    }
    public override void HandleGuiUpdates()
    {
        if (renderFinished)
        {
            iterProgresBarControler.setProgres(1);
        }
        else if (doAntialasing)
        {
            if (frameFinished)
            {
                iterProgresBarControler.setProgres(((float)currentSample + 1) / (float)maxAntiAliasyncReruns + ((float)currIter) / ((float)maxIter * (float)maxAntiAliasyncReruns));
            }
            else
            {
                iterProgresBarControler.setProgres((float)currentSample / (float)maxAntiAliasyncReruns + (float)currIter / ((float)maxIter * (float)maxAntiAliasyncReruns));
            }

        }
        else
        {
            iterProgresBarControler.setProgres((float)currIter / (float)maxIter);
        }
    }
    public override void SetShadersParameters()
    {

        for (int i = 0; i < shaderPre; i++)
        {
            TestPosiotnArray[i] = MiddleX.digits[i];
            TestPosiotnArray[shaderPre + i] = MiddleY.digits[i];
            TestPosiotnArray[shaderPre * 2 + i] = Scale.digits[i];

        }

        IterPecCycle = infinitePre ? IterPerInfiniteCycle : IterPerDoubleCycle;

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = Scale.toDouble() * Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel);
        doubleDataArray[1] = MiddleX.toDouble();
        doubleDataArray[2] = MiddleY.toDouble();
        doubleDataBuffer.SetData(doubleDataArray);
        ColorBuffer.SetData(MyColoringSystem.colorPalettes[currColorPalette].colors);

        if (infinitePre)
        {
            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_LastMultiframeData", LastMultiFrameRenderBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", antialiasLookupTable[currentSample % antialiasLookupTable.Length]);
            InfiniteShader.SetInt("_MaxIter", maxIter);
            InfiniteShader.SetBool("_reset", reset);
            InfiniteShader.SetBool("_pixelized", pixelized);
            InfiniteShader.SetInt("_pixelizationBase", pixelizationBase);
            InfiniteShader.SetInt("_ShiftX", shiftX);
            InfiniteShader.SetInt("_ShiftY", shiftY);
            InfiniteShader.SetInt("_Register", register);
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
            DoubleShader.SetBuffer(0, "_IterBuffer", IterBuffer);


        }
        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetBuffer(0, "_OldIterBuffer", OldIterBuffer);
        RenderShader.SetInt("_MaxIter", maxIter);
        RenderShader.SetFloat("_ColorStrength", colorStrength);
        RenderShader.SetBool("_Smooth", smoothGradient);
        RenderShader.SetBool("_Upscaling", upscaling);
        RenderShader.SetInt("_Type", MyColoringSystem.colorPalettes[currColorPalette].gradientType);
        RenderShader.SetInt("_PixelWidth", MathFunctions.IntPow(pixelizationBase, pixelizationLevel));
        RenderShader.SetInt("_OldPixelWidth", MathFunctions.IntPow(pixelizationBase, preUpscalePixLvl));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer);
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[currColorPalette].length);
        reset = false;
        shiftX = 0;
        shiftY = 0;
        pixelized = false;

    }


    void resetAntialias()
    {
        currentSample = 0;
        currIter = 0;
        frameFinished = false;
        renderFinished = false;
    }
    public void SetPrecision(bool val)
    {
        reset = true;
        infinitePre = val;
        resetAntialias();

    }
    public void SetSmoothGradient(bool val)
    {
        smoothGradient = val;
    }
    public void SetAnitialiasing(bool val)
    {
        doAntialasing = val;
        resetAntialias();
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
        reset = true;
    }

    public void SetGuiActive(bool val)
    {
        guiOn = val;
        gui.SetActive(val);
    }
    public void SetColorStrenght(float val)
    {
        colorStrength = Mathf.Clamp(val, ColorStrengthMin, ColorStrengthMax);
    }



    public override void HandleAntialias()
    {

        if (!renderFinished)
        {
            currIter += IterPecCycle;
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
