using System.Collections.Generic;
using UnityEngine;

public class FractalMaster : MonoBehaviour
{
    public ComputeShader FractalShader;

    private Camera _camera;
    private RenderTexture _target;
    private Material _addMaterial;
    private uint _currentSample = 0;
    private bool _frameFinished = false;

    Texture testTexture;

    float scrollSlowness = 10.0f;
    float lerpStrength = 0.2f;

    public double length = 4.0f;
    public double middleX = -1.0f;
    public double middleY = 0.0f;




    public int maxIter = 100;
    public int IterPecCycle = 4; 

    private int currIter = 0;

    public bool Antialas = true;
    private int maxAntiAliasyncReruns = 9;
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


    const int fpPre =4;

    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;

    ComputeBuffer MultiFrameRenderBuffer;
    ComputeBuffer ExperimentalBuffer;
    ComputeBuffer PossionBuffer;

    int[] TestPosiotnArray = new int[3*fpPre];

    private bool reset = false;
    private int shiftX = 0;
    private int shiftY = 0;
    private int register = 0;

    

    private Vector2 oldMousePosition = new Vector2();

    private int PrevScreenX;
    private int PrevScreenY;

    FixedPointNumber MiddleX = new FixedPointNumber(fpPre);
    FixedPointNumber MiddleY = new FixedPointNumber(fpPre);
    FixedPointNumber Scale = new FixedPointNumber(fpPre);


    struct Pixel {
        public double x;
        public double y;
        public int iter;
        public int finished;

    }
    public struct FixedPointNumber {
        public static int digitBase = 10000;
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
                res += digits[i].ToString("0000");
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
    
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height*2, sizeof(double) * 2 + sizeof(int) * 2);
        ExperimentalBuffer = new ComputeBuffer(Screen.width * Screen.height*2, sizeof(int) * 10);
        PossionBuffer = new ComputeBuffer(3*fpPre,sizeof(int));
        _camera = GetComponent<Camera>();
        oldMousePosition = Input.mousePosition;
    

    }

    private void OnEnable()
    {
        ResetParams();
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
        MiddleX.setDouble(middleX);
        MiddleY.setDouble(middleY);
        Scale.setDouble(length / Screen.width);

    }
    private void ResetParams() {
        reset = true;
        _currentSample = 0;
        currIter = 0;
    }

    private void Update()
    {
     
        
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height) {
            MultiFrameRenderBuffer.Dispose();
            MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height *2, sizeof(double) * 2 + sizeof(int) * 2);
            ExperimentalBuffer.Dispose();
            ExperimentalBuffer = new ComputeBuffer(Screen.width * Screen.height * 2, sizeof(int) * (fpPre * 2 + 2  ));
            reset = true;
        }
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;




        if (Input.GetKeyDown("r")) {
            ResetParams();
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
                else
                {
                    currIter += IterPecCycle;
                }

            }
        }
       




        Vector2 mousePosPix = Input.mousePosition;
        //Transform pixel coardinates to position;
        FixedPointNumber mousePosRealX = new FixedPointNumber(fpPre);
        mousePosRealX.setDouble(mousePosPix.x - Screen.width / 2);
        mousePosRealX = mousePosRealX*Scale+MiddleX;
        FixedPointNumber mousePosRealY = new FixedPointNumber(fpPre);
        mousePosRealY.setDouble(mousePosPix.y - Screen.height / 2);
        mousePosRealY = mousePosRealY * Scale + MiddleY;
        FixedPointNumber multiplyer = new FixedPointNumber(fpPre);
        if (Input.mouseScrollDelta.y != 0)
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
        if (Input.mouseScrollDelta.x != 0) { 
            maxIter -= maxIter * (int)Input.mouseScrollDelta.x /4;
            ResetParams();
        }
        if (oldMousePosition != mousePosPix) {
            if (Input.GetMouseButton(0)) {


                shiftX = (int)(mousePosPix.x - oldMousePosition.x);
                shiftY = (int)(mousePosPix.y - oldMousePosition.y);
                
                if (register == 0)
                {
                    register = 1;
                }
                else { 
                    register = 0;
                }
                multiplyer.setDouble(mousePosPix.x - oldMousePosition.x);
                MiddleX -= multiplyer * Scale;
                multiplyer.setDouble(mousePosPix.y - oldMousePosition.y);
                MiddleY -= multiplyer * Scale;
                ResetParams();
            }
            
        }
        oldMousePosition = mousePosPix;




    }
    void OnDestroy()
    {
        Destroy(_target);
        doubleDataBuffer.Dispose();
        MultiFrameRenderBuffer.Dispose();
        ExperimentalBuffer.Dispose();
        PossionBuffer.Dispose();
    }


    private void SetShaderParameters()
    {
      
        for (int i = 0; i <fpPre; i++) {
            TestPosiotnArray[i] = MiddleX.digits[i];
            TestPosiotnArray[fpPre+i] = MiddleY.digits[i];
            TestPosiotnArray[fpPre*2+i] = Scale.digits[i];

        }
      

        PossionBuffer.SetData(TestPosiotnArray);
        doubleDataArray[0] = length;
        doubleDataArray[1] = middleX;
        doubleDataArray[2] = middleY;
        doubleDataBuffer.SetData(doubleDataArray);
        FractalShader.SetBuffer(0,"_DoubleDataBuffet", doubleDataBuffer);
        FractalShader.SetBuffer(0, "_MultiFrameData",MultiFrameRenderBuffer);
        FractalShader.SetBuffer(0, "_ExperimentalBuffer", ExperimentalBuffer);
        FractalShader.SetBuffer(0, "_PossitionBuffer", PossionBuffer);
        FractalShader.SetVector("_PixelOffset", AntiAliasLookupTable[_currentSample%AntiAliasLookupTable.Length]);
        FractalShader.SetInt("_MaxIter",maxIter);
        FractalShader.SetInt("_IterPerCycle", IterPecCycle);
        FractalShader.SetBool("_reset", reset);
        FractalShader.SetInt("_ShiftX", shiftX);
        FractalShader.SetInt("_ShiftY", shiftY);
        FractalShader.SetInt("_Register", register);
        reset = false;
        shiftX = 0;
        shiftY = 0;
        
        
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
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
    }
  
    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        FractalShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        FractalShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

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
