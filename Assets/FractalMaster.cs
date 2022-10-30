using System.Collections.Generic;
using UnityEngine;

public class FractalMaster : MonoBehaviour
{
    //Shaders
    public ComputeShader InfiniteShader;
    public ComputeShader DoubleShader;
    public ComputeShader RenderShader;
    Camera _camera;
    RenderTexture _target;
    RenderTexture _dummyTexture;
    Material _addMaterial;
    bool doublePre = false;
    //Anti-Alias
    uint _currentSample = 0;
    bool _frameFinished = false;
    int currIter = 0;
    public bool Antialas = false;
    int maxAntiAliasyncReruns = 9;
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
    float lerpStrength = 0.2f;
    double length = 4.0f;
    double middleX = -1.0f;
    double middleY = 0.0f;
    string pixelizationLevelUpControl = "i";
    string pixelizationLevelDownControl = "o";
    string toggleShaderControl = "t";
    string resetControl = "r";
    string maxIterContorl = "p";
    string logContorl = "l";
    string colorStrengthContorlUp = "k";
    string colorStrengthContorlDown = "j";

    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    FixedPointNumber MiddleX = new FixedPointNumber(fpPre);
    FixedPointNumber MiddleY = new FixedPointNumber(fpPre);
    FixedPointNumber Scale = new FixedPointNumber(fpPre);


    //precision
    int maxIter = 100;
    int IterPecCycle = 5;
    const int shaderPre = 4;
    const int fpPre = shaderPre * 2;

    //Pixelization
    int pixelizationBase = 2;
    int pixelizationLevel = 0;
    int lastPixelizationLevel;

    //DoubleShader
    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;
    ComputeBuffer MultiFrameRenderBuffer;

    //InfiShader
    ComputeBuffer FpMultiframeBuffer;
    ComputeBuffer PossionBuffer;
    int[] TestPosiotnArray = new int[3 * shaderPre];

    //RenderShader
    ComputeBuffer IterBuffer;
    int colorStrength = 5;
    bool log = true;


    //Shader control
    bool reset = false;
    int shiftX = 0;
    int shiftY = 0;
    int register = 0;




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

    struct Pixel {
        public double x;
        public double y;
        public int iter;
        public int finished;

    }
    public struct FixedPointNumber {
        public static int digitBase = 46300;
        public int precision;
        public int[] digits;
        public FixedPointNumber(int pre)
        {
            precision = pre;    
            digits = new int[pre];
            for (int i = 0; i<precision;i++) {
                digits[i] = 0;
            }
        }
        public void setDouble(double num) {
            bool negate = false;
            if (num < 0)
            {
                negate = true;
                num = -num;
            }
            double temp = num;
            for (int i = 0; i < precision; i++)
            {
                digits[i] = (int)temp;
                temp = (temp - digits[i]) * digitBase;
            }
            if (negate)
            {
                Negate();
            }
        }
        public override string ToString() {
            string res = "";
            res += digits[0];
            res += ".";
            for (int i = 1; i < precision; i++) {
                res += digits[i].ToString("00000$");
            }
            return res;
        }    
        public void IncresePrecision(int newPrecision) {
            int[] temp = digits;
            digits = new int[newPrecision];
            for (int i = 0; i < newPrecision; i++) {
                digits[i] = i < precision ? temp[i] : 0;
            }
            precision = newPrecision;
        }
        public bool IsPositive() {
            bool res = true;
            for (int i = 0; i < precision; i++)
            {
                if (digits[i] < 0)
                {
                    res = false;
                }
            }
            return res;
        }
        public void Negate() {
            for (int i = 0; i < precision; i++)
            {
                if (digits[i] != 0)
                {
                    digits[i] = -digits[i];
                    return;

                }

            }
        }
        public void MultiplyByInt(int num) {
            bool negate = false;

            if (!IsPositive())
            {
                if (num < 0)
                {
                    Negate();
                    num = -num;
                }
                else
                {
                    Negate();
                    negate = true;
                }
            }
            else if (num < 0)
            {
                num = -num;
                negate = true;
            }
    
            for (int i = 0; i < precision; i++)
            {
                digits[i] *= num;
            }
        
            for (int x = precision - 1; x >= 0; x--)
            {

                if (x != 0)
                {
                    digits[x - 1] += digits[x] / digitBase;
                }
                digits[x] %= digitBase;
            }


            if (negate)
            {
                Negate();
            }
        }
        public void Shift(int num) {
            int[] newDigits = new int[precision];
            for (int i = 0; i < precision; i++)
            {                
                if (i + num >= 0 && i + num < precision)
                {
                    newDigits[i] = digits[i + num];
                }
                else {
                    newDigits[i] = 0;
                }
            }
            digits = newDigits;        
        }
        public void Set(FixedPointNumber fpnum)
        {
            for (int i = 0; i < precision; i++)
            {
                if (i < fpnum.precision)
                {
                    digits[i] = fpnum.digits[i];
                }
                else
                {
                    digits[i] = 0;
                }
            }
        }
        public double toDouble()
        {
            double res = 0;
            bool negate = !IsPositive();
            if (negate)
            {
                Negate();
            }
            for (int i = 0; i < precision; i++)
            {
                double multpiplyer = 1;
                for(int x = 0; x < i; x++)
                {
                    multpiplyer /= digitBase;
                }
                res += (double)digits[i] * multpiplyer;
            }
            if (negate)
            {
                Negate();
                res *= -1;
            }
            return res;
        }
        public static bool operator >(FixedPointNumber a, FixedPointNumber b)
        {
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            for (int i = 0; i < a.precision; i++) {
                if (a.digits[i] > b.digits[i])
                {
                    return true;
                }
                if(a.digits[i] < b.digits[i])
                {
                    return false;
                }
            }
            return false;
        }
        public static bool operator <(FixedPointNumber a, FixedPointNumber b)
        {
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            for (int i = 0; i < a.precision; i++)
            {
                if (a.digits[i] < b.digits[i])
                {
                    return true;
                }
                if (a.digits[i]> b.digits[i])
                {
                    return false;
                }
            }
            return false;
        }
        public static FixedPointNumber operator +(FixedPointNumber aPassed, FixedPointNumber bPassed){
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision) {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision) {
                b.IncresePrecision(a.precision);
            }
            bool negate = false;
      
            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                    a.Negate();
                    b.Negate();
                    negate = true;
                }
                else
                {
                    a.Negate();
                    return b - a;
                }
            }
            else if (!b.IsPositive()) {
                b.Negate();
                return a - b;
            }                      
            FixedPointNumber res = new FixedPointNumber(a.precision);
            res = a;
            for (int i = 0; i < a.precision; i++)
            {
                res.digits[i] += b.digits[i];

            }
            for (int x = a.precision - 1; x >= 0; x--)
            {

                if (x != 0)
                {
                    res.digits[x - 1] += res.digits[x] / digitBase;
                }
                res.digits[x] %= digitBase;
            }
            if (negate)
            {
                res.Negate();
            

            }
            return res;
        }
        public static FixedPointNumber operator *(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }
            bool negate = false;
            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                    a.Negate();
                    b.Negate();
                }
                else
                {
                    a.Negate();
                    negate = true;
                }
            }
            else if (!b.IsPositive()) {
                b.Negate();
                negate = true;            
            }
            FixedPointNumber multiplicationResults = new FixedPointNumber(a.precision + b.precision);
            FixedPointNumber temp = new FixedPointNumber(a.precision + b.precision);
            FixedPointNumber res = new FixedPointNumber(a.precision);
            for (int i = 0; i < a.precision; i++)
            {
                for (int k = 0; k < a.precision * 2; k++)
                {
                    if (k < a.precision)
                    {
                        temp.digits[k] = a.digits[k];
                    }
                    else
                    {
                        temp.digits[k] = 0;
                    }

                }
                temp.Shift(-i);
                temp.MultiplyByInt(b.digits[i]);
                multiplicationResults += temp;
 
            }
      
            for (int x = 0; x < a.precision; x++)
            {
                res.digits[x] = multiplicationResults.digits[x];
            }
            res.digits[a.precision - 1] += multiplicationResults.digits[fpPre] / (digitBase / 2);
            if (negate)
            {
                res.Negate();
            }
            return res;

        }
        public static FixedPointNumber operator -(FixedPointNumber aPassed, FixedPointNumber bPassed)
        {
            FixedPointNumber a = aPassed.Replicate();
            FixedPointNumber b = bPassed.Replicate();
            if (a.precision < b.precision)
            {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision)
            {
                b.IncresePrecision(a.precision);
            }          
            FixedPointNumber res = new FixedPointNumber(a.precision);
            if (!b.IsPositive()) {
                b.Negate();
                return a + b;
            }
            else if (!a.IsPositive())
            {
                a.Negate();
                res = a + b;
                res.Negate();
                return res;
            }
            if (b > a) {
                res = b - a;
                res.Negate();
                return res;                
            }
            for (int i = 0; i < a.precision; i++)
            {
                res.digits[i] = a.digits[i] - b.digits[i];
            }
            res.digits[0]--;

            for (int j = 1; j < a.precision; j++)
            {
                res.digits[j] += digitBase - 1;
            }
            res.digits[fpPre - 1]++;
            for (int k = fpPre - 1; k >= 0; k--)
            {

                if (k != 0)
                {
                    res.digits[k - 1] += res.digits[k] / digitBase;
                }
                res.digits[k] %= digitBase;
            }
            return res;
        }
        public FixedPointNumber Replicate()
        {
            FixedPointNumber res = new FixedPointNumber(precision);
            for(int i = 0; i < precision; i++)
            {
                res.digits[i] = digits[i];
            }
            return res;
        }
     
    }
    private void Awake()
    {
        Application.targetFrameRate = -1;
       
        IterBuffer = new ComputeBuffer(Screen.width * Screen.height  / Pow(Pow(pixelizationBase, pixelizationLevel),2),sizeof(int)*2);
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2);
        FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * (shaderPre * 2 + 2));
        PossionBuffer = new ComputeBuffer(3*shaderPre,sizeof(int));
        
        _camera = GetComponent<Camera>();

        MiddleX.setDouble(middleX);
        MiddleY.setDouble(middleY);
        Scale.setDouble(Pow(pixelizationBase, pixelizationLevel) * length / Screen.width);
        
        handleLastValues();
      
        ResetParams();
    }
    void handleLastValues() {
    
        lastPixelizationLevel = pixelizationLevel;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
    void handleKeyInput()
    {
        if (Input.GetKeyDown(pixelizationLevelUpControl))
        {

            pixelizationLevel += 1;
        }
        if (Input.GetKeyDown(pixelizationLevelDownControl))
        {
            if (pixelizationLevel > 0)
            {

                pixelizationLevel -= 1;
            }
        }
        if (Input.GetKeyDown(toggleShaderControl))
        {
            reset = true;
            doublePre = !doublePre;
        }


        if (Input.GetKeyDown(resetControl))
        {
            ResetParams();
        }
        if (Input.GetKeyDown(logContorl))
        {
            log = !log;
        }
        if (Input.GetKeyDown(colorStrengthContorlUp))
        {
            colorStrength++;
        }
        if (Input.GetKeyDown(colorStrengthContorlDown))
        {
            if (colorStrength > 2)
            {
                colorStrength--;
            }
        }


    }
    void handleScreenSizeChange() {
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height || lastPixelizationLevel != pixelizationLevel)
        {
            FixedPointNumber scaleFixer = new FixedPointNumber(fpPre);
            scaleFixer.setDouble((double)Pow(pixelizationBase, pixelizationLevel) / (double)Pow(pixelizationBase, lastPixelizationLevel));
            Scale *= scaleFixer;
            IterBuffer.Dispose();
            IterBuffer = new ComputeBuffer(Screen.width * Screen.height / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int)*2);
            MultiFrameRenderBuffer.Dispose();
            MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(double) * 2 + sizeof(int) * 2);
            FpMultiframeBuffer.Dispose();
            FpMultiframeBuffer = new ComputeBuffer(Screen.width * Screen.height * 2 / Pow(Pow(pixelizationBase, pixelizationLevel), 2), sizeof(int) * (shaderPre * 2 + 2));
           
            reset = true;
        }

    }
    void handleAntialias()
    {

        if (Antialas)
        {
            if (_currentSample < maxAntiAliasyncReruns)
            {
                if (currIter > maxIter)
                {
                    _frameFinished = true;
                    currIter = 0;
                }
                else
                {
                    currIter += IterPecCycle;
                }

            }
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
                multiplyer.setDouble(1 - Input.mouseScrollDelta.y / scrollSlowness);
                Scale *= multiplyer;

                FixedPointNumber differenceX = mousePosRealX - MiddleX;
                FixedPointNumber differenceY = mousePosRealY - MiddleY;
                multiplyer.setDouble(lerpStrength * Input.mouseScrollDelta.y);
                MiddleX += differenceX * multiplyer;
                MiddleY += differenceY * multiplyer;
                ResetParams();
            }

        }
        if (mouseTextureCoordinatesX != oldMouseTextureCoordinatesX || mouseTextureCoordinatesY != oldMouseTextureCoordinatesY)
        {
            if (Input.GetMouseButton(0))
            {


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
    }

    private void Update()
    {   

        handleLastValues();

        handleKeyInput();
        handleMouseInput();

        handleScreenSizeChange();
        
        handleAntialias();

    }
    void OnDestroy()
    {
        Destroy(_target);
        IterBuffer.Dispose();
        doubleDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
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
      

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = Scale.toDouble() * Screen.width / Pow(pixelizationBase, pixelizationLevel);
        doubleDataArray[1] = MiddleX.toDouble();
        doubleDataArray[2] = MiddleY.toDouble();
        doubleDataBuffer.SetData(doubleDataArray);
        if (doublePre)
        {
            DoubleShader.SetBuffer(0,"_DoubleDataBuffer", doubleDataBuffer);
            DoubleShader.SetBuffer(0, "_MultiFrameData",MultiFrameRenderBuffer);
            DoubleShader.SetVector("_PixelOffset", AntiAliasLookupTable[_currentSample % AntiAliasLookupTable.Length]);
            DoubleShader.SetInt("_MaxIter", maxIter);
            DoubleShader.SetBool("_reset", reset);
            DoubleShader.SetInt("_ShiftX", shiftX);
            DoubleShader.SetInt("_ShiftY", shiftY);
            DoubleShader.SetInt("_Register", register);
            DoubleShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        }
        else
        {
            InfiniteShader.SetBuffer(0, "_FpMultiframeBuffer", FpMultiframeBuffer);
            InfiniteShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
            InfiniteShader.SetVector("_PixelOffset", AntiAliasLookupTable[_currentSample%AntiAliasLookupTable.Length]);
            InfiniteShader.SetInt("_MaxIter",maxIter);
            InfiniteShader.SetBool("_reset", reset);
            InfiniteShader.SetInt("_ShiftX", shiftX);
            InfiniteShader.SetInt("_ShiftY", shiftY);
            InfiniteShader.SetInt("_Register", register);
            InfiniteShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        }
        RenderShader.SetBuffer(0, "_IterBuffer", IterBuffer);
        RenderShader.SetInt("_MaxIter", maxIter);
        RenderShader.SetInt("_ColorStrength", colorStrength);
        RenderShader.SetBool("_Log", log);
        RenderShader.SetInt("_PixelWidth", Pow(pixelizationBase, pixelizationLevel));
        reset = false;
        shiftX = 0;
        shiftY = 0;
        
        
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

        if (doublePre)
        {
            DoubleShader.SetTexture(0, "Result", _dummyTexture);
            DoubleShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);
        }
        else
        {
            InfiniteShader.SetTexture(0, "Result", _dummyTexture);
            InfiniteShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);
        }

        RenderShader.SetTexture(0, "Result", _target);
        RenderShader.Dispatch(0, RenderThreadGrupsX, RenderThreadGrupsY, 1);
        // Blit the result texture to the screen

        if (true||(_currentSample == 0 && !_frameFinished)||Input.GetMouseButton(0) )
        {
            Graphics.Blit(_target, destination);


        }
        else if (_frameFinished)
        {
            if (_addMaterial == null)
            {
                _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
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
