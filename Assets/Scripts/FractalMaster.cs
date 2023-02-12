using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FixedPointNumberSystem;
using Colors;
public class FractalMaster : MonoBehaviour
{
    //Shaders
    public ComputeShader InfiniteShader;
    public ComputeShader DoubleShader;
    public ComputeShader RenderShader;
    public Shader AddShader;


    Camera _camera;
    RenderTexture _target;
    RenderTexture _dummyTexture;
    Material _addMaterial;
    public GameObject gui;
    bool infinitePre = false;
    //Anti-Alias
    uint _currentSample = 0;
    bool _frameFinished = false;
    int currIter = 0;
    bool Antialas = false;
    int maxAntiAliasyncReruns = 9;

    bool guiOn = true;
    private Vector2[] AntiAliasLookupTable = {
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

    bool renderFinished = false;

    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    FixedPointNumber MiddleX = new FixedPointNumber(fpPre);
    FixedPointNumber MiddleY = new FixedPointNumber(fpPre);
    FixedPointNumber Scale = new FixedPointNumber(fpPre);

    //gui
    int guiWidth = 300;
    public Toggle precisionToggle;
    public Toggle smoothGradientToggle;
    public Toggle antialiasingToggle;
    public Slider colorStrengthSlider;
    public Slider maxIterSlider;
    public  TMPro.TMP_Dropdown colorPaletteDropdown;
    public progresBarContorler iterProgresBarControler;

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



  
    int Pow(int baseNum, int exponent)
    {
        int res;
        if (exponent == 0)
        {
            res = 1;
        }
        else
        {
            res = baseNum;
        }
        for(int i = 1; i < exponent; i++)
        {
            res *= baseNum;
        }
        return res;
    }
   
    private void Awake()
    {
        Application.targetFrameRate = -1;
       
        IterBuffer = new ComputeBuffer(Screen.width * Screen.height  / Pow(Pow(pixelizationBase, pixelizationLevel),2),sizeof(int)*2+sizeof(float));
        OldIterBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float)*2);
        LastMultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
        FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
        PossionBuffer = new ComputeBuffer(3*shaderPre,sizeof(int));
        ColorBuffer = new ComputeBuffer(MyColoringSystem.colorPalettes[currColorPalette].length, 4 * sizeof(float));

        _camera = GetComponent<Camera>();

        MiddleX.setDouble(middleX);
        MiddleY.setDouble(middleY);
        Scale.setDouble(Pow(pixelizationBase, pixelizationLevel) * length / Screen.width);
        
        IterPecCycle = infinitePre ? IterPerInfiniteCycle : IterPerDoubleCycle;

        handleLastValues();
      
        ResetParams();
        InitializeGui();
    }
    void handleLastValues() {    
        lastPixelizationLevel = pixelizationLevel;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
    void resetAntialias()
    {
        _currentSample = 0;
        currIter = 0;
        _frameFinished = false;
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
        Antialas = val;
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
    void InitializeGui()
    {
        precisionToggle.isOn = infinitePre;
        antialiasingToggle.isOn = Antialas;
        smoothGradientToggle.isOn = smoothGradient;
        colorStrengthSlider.maxValue = Mathf.Log10(ColorStrengthMax);
        colorStrengthSlider.minValue = Mathf.Log10(ColorStrengthMin);
        colorStrengthSlider.value = Mathf.Log10(colorStrength);

        colorPaletteDropdown.options.Clear();
        foreach (ColorPalette palete in MyColoringSystem.colorPalettes)
        {
            colorPaletteDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData() {text = palete.name });
        }
        colorPaletteDropdown.value = currColorPalette;
        maxIterSlider.value = Mathf.Log10(maxIter);
        SetGuiActive(guiOn);
        iterProgresBarControler.setProgres(0);

    }
    public void Exit()
    {
        Application.Quit();
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

    void handleKeyInput()
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
            SetAnitialiasing(!Antialas);
            antialiasingToggle.isOn = Antialas; 
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
                int[] arr = new int[Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2) * 3];
                IterBuffer.GetData(arr);
                OldIterBuffer.Dispose();
                OldIterBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
                OldIterBuffer.SetData(arr);
                IterBuffer.Dispose();


                preUpscalePixLvl = pixelizationLevel;
                pixelizationLevel -= 1;

                IterBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * 2 + sizeof(float));
                FixedPointNumber scaleFixer = new FixedPointNumber(fpPre);
                scaleFixer.setDouble((double)Pow(pixelizationBase, pixelizationLevel) / (double)Pow(pixelizationBase, lastPixelizationLevel));
                Scale *= scaleFixer;
                upscaling = true;
                renderFinished = false;
                currIter = 0;
            }
           
        }

    }
    void handleScreenSizeChange() {
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height || lastPixelizationLevel != pixelizationLevel)
        {
            reset = true;
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int)*2 + sizeof(float));
            MultiFrameRenderBuffer.Dispose();
            MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2 + sizeof(float) * 2);
            LastMultiFrameRenderBuffer.Dispose();
            LastMultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
            if(lastPixelizationLevel != pixelizationLevel && !upscaling)
            {
                int[] oldArr = new int[Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, lastPixelizationLevel), 2) * shaderPixelSize]; ;
                int[] newArr;
                int dataWidth;
                int dataHeigth;
                int otherWidth;
                int otherHeigth;
                if (lastPixelizationLevel < pixelizationLevel)//zoom in
                {
                    newArr = new int[Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2) * shaderPixelSize];                   
                    dataWidth = Screen.width / Pow(pixelizationBase, pixelizationLevel);
                    dataHeigth = Screen.height / Pow(pixelizationBase, pixelizationLevel);
                    
                    otherWidth = Screen.width / Pow(pixelizationBase, lastPixelizationLevel);
                    otherHeigth = Screen.height / Pow(pixelizationBase, lastPixelizationLevel);

                }
                else //zoom out
                {
                   
                    newArr = new int[Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2) * shaderPixelSize];


                    otherWidth = Screen.width / Pow(pixelizationBase, pixelizationLevel);
                    otherHeigth = Screen.height / Pow(pixelizationBase, pixelizationLevel);


                    dataWidth = Screen.width / Pow(pixelizationBase, lastPixelizationLevel);
                    dataHeigth = Screen.height / Pow(pixelizationBase, lastPixelizationLevel);
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
                FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);

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
                FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * shaderPixelSize);
            }



        }

    }
    void handleAntialias()
    {
        if (!renderFinished)
        {
            currIter += IterPecCycle;
        }


        if (Antialas)
        {
            if (_currentSample < maxAntiAliasyncReruns)
            {
                if (currIter > maxIter)
                {
               
                    _frameFinished = true;
                    currIter = 0;
                }

            }
            else
            {
                renderFinished=true;
            }
        }else if (currIter > maxIter)
        {
            renderFinished = true;
        }
      
    }
    void handleMouseInput()
    {

        Vector2 mousePosPix = Input.mousePosition;
        int mouseTextureCoordinatesX = (int)mousePosPix.x / Pow(pixelizationBase, pixelizationLevel);
        int mouseTextureCoordinatesY = (int)mousePosPix.y / Pow(pixelizationBase, pixelizationLevel);


        FixedPointNumber mousePosRealX = new FixedPointNumber(fpPre);

        mousePosRealX.setDouble(mouseTextureCoordinatesX - Screen.width / (2 * Pow(pixelizationBase, pixelizationLevel)));
        mousePosRealX = mousePosRealX * Scale + MiddleX;
        FixedPointNumber mousePosRealY = new FixedPointNumber(fpPre);

        mousePosRealY.setDouble(mouseTextureCoordinatesY - Screen.height / (2 * Pow(pixelizationBase, pixelizationLevel)));
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
    private void ResetParams() {
        reset = true;
        _currentSample = 0;
        currIter = 0;
        upscaling = false;
        renderFinished = false;
    }
    void handleGuiUpdates()
    {
        if (renderFinished)
        {
            iterProgresBarControler.setProgres(1);
        }
        else if (Antialas)
        {
            if (_frameFinished)
            {
                iterProgresBarControler.setProgres(((float)_currentSample+1) / (float)maxAntiAliasyncReruns + ((float)currIter) / ((float)maxIter * (float)maxAntiAliasyncReruns));
            }
            else
            {
                iterProgresBarControler.setProgres((float)_currentSample / (float)maxAntiAliasyncReruns + (float)currIter / ((float)maxIter * (float)maxAntiAliasyncReruns));
            }
            
        }
        else
        {
            iterProgresBarControler.setProgres((float)currIter / (float)maxIter);
        }

    }
    private void Update()
    {
        handleLastValues();

        handleKeyInput();
        if (!guiOn || Input.mousePosition.x < Screen.width - guiWidth)
        {
            handleMouseInput();
        }

        handleScreenSizeChange();
        
        handleAntialias();
        handleGuiUpdates();
        
    }
    void OnDestroy()
    {
        Destroy(_target);
        IterBuffer.Dispose();
        OldIterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
        LastMultiFrameRenderBuffer.Dispose();
        ColorBuffer.Dispose();
        FpMultiframeBuffer.Dispose();
        PossionBuffer.Dispose();
    }


    private void SetShaderParameters()
    {
      
        for (int i = 0; i <shaderPre; i++) {
            TestPosiotnArray[i] = MiddleX.digits[i];
            TestPosiotnArray[shaderPre + i] = MiddleY.digits[i];
            TestPosiotnArray[shaderPre * 2+i] = Scale.digits[i];

        }

        IterPecCycle = infinitePre ? IterPerInfiniteCycle : IterPerDoubleCycle;

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = Scale.toDouble() * Screen.width / Pow(pixelizationBase, pixelizationLevel);
        doubleDataArray[1] = MiddleX.toDouble();
        doubleDataArray[2] = MiddleY.toDouble();
        doubleDataBuffer.SetData(doubleDataArray);
        ColorBuffer.SetData(MyColoringSystem.colorPalettes[currColorPalette].colors);

        if (infinitePre)
        {
            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_LastMultiframeData", LastMultiFrameRenderBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", AntiAliasLookupTable[_currentSample % AntiAliasLookupTable.Length]);
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
            DoubleShader.SetVector("_PixelOffset", AntiAliasLookupTable[_currentSample % AntiAliasLookupTable.Length]);
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
        RenderShader.SetInt("_PixelWidth", Pow(pixelizationBase, pixelizationLevel));
        RenderShader.SetInt("_OldPixelWidth", Pow(pixelizationBase, preUpscalePixLvl));
        RenderShader.SetBuffer(0, "_Colors", ColorBuffer); 
        RenderShader.SetInt("_ColorArrayLength", MyColoringSystem.colorPalettes[currColorPalette].length);
        reset = false;
        shiftX = 0;
        shiftY = 0;
        pixelized = false;

    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height||lastPixelizationLevel != pixelizationLevel)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();

            // Get a render target for Fractal
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();

            // Reset sampling
            _currentSample = 0;
        }
        if (_dummyTexture == null || _dummyTexture.width != Screen.width / Pow(pixelizationBase, pixelizationLevel) || _dummyTexture.height != Screen.height / Pow(pixelizationBase, pixelizationLevel) || lastPixelizationLevel != pixelizationLevel)
        {
         
            if (_dummyTexture != null)
                _dummyTexture.Release();


            _dummyTexture = new RenderTexture(Screen.width / Pow(pixelizationBase, pixelizationLevel), Screen.height / Pow(pixelizationBase, pixelizationLevel), 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _dummyTexture.enableRandomWrite = true;
            _dummyTexture.Create();

         
        }
    }
  
    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        int RenderThreadGrupsX = Mathf.CeilToInt(Screen.width / 8);
        int RenderThreadGrupsY = Mathf.CeilToInt(Screen.height / 8);
        int CalculatethreadGroupsX = Mathf.CeilToInt(Screen.width / (8* Pow(pixelizationBase, pixelizationLevel)));
        int CalculatethreadGroupsY = Mathf.CeilToInt(Screen.height / (8* Pow(pixelizationBase, pixelizationLevel)));

        if (infinitePre)
        {

            InfiniteShader.SetTexture(0, "Result", _dummyTexture);
            InfiniteShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);

        }
        else
        {

            DoubleShader.SetTexture(0, "Result", _dummyTexture);
            DoubleShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);
        }

        RenderShader.SetTexture(0, "Result", _target);
        RenderShader.Dispatch(0, RenderThreadGrupsX, RenderThreadGrupsY, 1);
        // Blit the result texture to the screen
       
        if ((_currentSample == 0 && !_frameFinished)||Input.GetMouseButton(0) )
        {
            Graphics.Blit(_target, destination);


        }
        else if (_frameFinished)
        {

            if (_addMaterial == null)
            {
                _addMaterial = new Material(AddShader);
            }

            _addMaterial.SetFloat("_Sample", _currentSample);
            Graphics.Blit(_target, destination, _addMaterial);
            _frameFinished = false;
            
            _currentSample++;
            
            
            reset = true;
        } 
         
       
        



    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
}
