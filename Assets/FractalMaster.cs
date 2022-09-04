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
    public int maxIter = 500;
    public int IterPecCycle = 3; 

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




    double[] doubleDataArray = new double[3];
    ComputeBuffer doubleDataBuffer;

    ComputeBuffer MultiFrameRenderBuffer;
    ComputeBuffer ExperimentalBuffer;


    private bool reset = false;
    private int shiftX = 0;
    private int shiftY = 0;
    private int register = 0;

    private Vector2 oldMousePosition = new Vector2();

    private int PrevScreenX;
    private int PrevScreenY;


  

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
        public void setFloat(float num)
        {
            float temp = num;
            for (int i = 0; i < precision; i++) {
                digits[i] = (int)temp;
                temp = (temp - digits[i]) * digitBase;
            }
        }
        public void setDouble(double num) {
            double temp = num;
            for (int i = 0; i < precision; i++)
            {
                digits[i] = (int)temp;
                temp = (temp - digits[i]) * digitBase;
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
            if (digits[0] >= 0) { 
            return true;
            }
            return false;
        }
        public void Negate() {
            digits[0] = -digits[0];
        }
        public void MultiplyByInt(int num) {
            for (int i = 0; i < precision; i++) {
                digits[i] *= num;
            }
            for (int i = precision - 1; i >= 0; i--) {

                if (i != 0)
                {
                    digits[i - 1] += digits[i] / digitBase;
                }
                digits[i] %= digitBase;
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
                if (b.digits[i] > a.digits[i])
                {
                    return true;
                }

            }

            return false;




        }
        public static FixedPointNumber operator +(FixedPointNumber a, FixedPointNumber b){
            if (a.precision < b.precision) {
                a.IncresePrecision(b.precision);
            }
            if (b.precision < a.precision) {
                b.IncresePrecision(a.precision);
            }            
            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                    a.Negate();
                    b.Negate();
                }
                else
                {
                    return b - a;
                }
            }
            else if (!b.IsPositive()) {
                return a - b;
            }                      
            FixedPointNumber res = new FixedPointNumber(a.precision);
            for (int i = res.precision - 1; i >= 0; i--) {
                res.digits[i] +=  a.digits[i] +  b.digits[i];
                if (i != 0)
                {
                    res.digits[i - 1] += res.digits[i] / digitBase;
                }                
                res.digits[i] %= digitBase;
            }
            if (!a.IsPositive())
            {
                if (!b.IsPositive())
                {
                   res.Negate();
                }
            }
            return res;
        }
        public static FixedPointNumber operator *(FixedPointNumber a, FixedPointNumber b)
        {
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
            FixedPointNumber[] multiplicationResults = new FixedPointNumber[b.precision];                       
            FixedPointNumber res = new FixedPointNumber(a.precision+b.precision);
            for (int i = b.precision - 1; i >= 0; i--) {                              
                multiplicationResults[i] = new FixedPointNumber(a.precision + b.precision);
                multiplicationResults[i].Set(a);
                multiplicationResults[i].MultiplyByInt(b.digits[i]);
                multiplicationResults[i].Shift(-i);              
            }           
            for(int  i =0; i < b.precision; i++)
            {
                res += multiplicationResults[i];                
            }                        
            if (negate) {
                res.Negate();
            }
            return res;
        }
        public static FixedPointNumber operator -(FixedPointNumber a, FixedPointNumber b)
        {
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
            for (int i = res.precision - 1; i >= 0; i--)
            {
                if (a.digits[i] >= b.digits[i] || i ==0)
                {
                    res.digits[i] = a.digits[i] - b.digits[i];
                }
                else {                    
                    int x = i-1;
                    while (x >= 0) {
                        Debug.Log(a.digits[x]);
                        if (a.digits[x] > 0) {
                            a.digits[x]--;
                            for (int t = x + 1; t <= i; t++) {
                                a.digits[t] += digitBase - 1;
                            }
                            a.digits[i] += 1;
                            break;
                        }
                        x--;
                    }
                    res.digits[i] = a.digits[i] - b.digits[i];
                }
            }
            return res;
        }
    }
    private void Awake()
    {
  
        doubleDataBuffer = new ComputeBuffer(3, sizeof(double));
        MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height*2, sizeof(double) * 2 + sizeof(int) * 2);
        ExperimentalBuffer = new ComputeBuffer(Screen.width * Screen.height, sizeof(int) * 10);
        _camera = GetComponent<Camera>();
        oldMousePosition = Input.mousePosition;
    
    }

    private void OnEnable()
    {
        ResetParams();
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
    private void ResetParams() {
        reset = true;
        _currentSample = 0;
        currIter = 0;
    }

    private void Update()
    {
     //   Debug.Log(maxIter);
        if (PrevScreenX != Screen.width || PrevScreenY != Screen.height) {
            MultiFrameRenderBuffer.Dispose();
            MultiFrameRenderBuffer = new ComputeBuffer(Screen.width * Screen.height *2, sizeof(double) * 2 + sizeof(int) * 2);
            ExperimentalBuffer.Dispose();
            ExperimentalBuffer = new ComputeBuffer(Screen.width * Screen.height, sizeof(int) * 10);
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
        double scale = length / Screen.width;
        double mousePosRealX = (mousePosPix.x - Screen.width/2) * scale + middleX;
        double mousePosRealY = (mousePosPix.y - Screen.height / 2) * scale + middleY;

        if (Input.mouseScrollDelta.y != 0)
        {
            length *=  1 - Input.mouseScrollDelta.y/ scrollSlowness;
            double differenceX = mousePosRealX - middleX;
            double differenceY = mousePosRealY - middleY;
            middleX += differenceX * lerpStrength * Input.mouseScrollDelta.y;
            middleY += differenceY * lerpStrength * Input.mouseScrollDelta.y;
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
                middleX -= (mousePosPix.x - oldMousePosition.x) * scale;
                middleY -= (mousePosPix.y - oldMousePosition.y) * scale;
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
       
    }


    private void SetShaderParameters()
    {
       
        doubleDataArray[0] = length;
        doubleDataArray[1] = middleX;
        doubleDataArray[2] = middleY;
        doubleDataBuffer.SetData(doubleDataArray);
        FractalShader.SetBuffer(0,"_DoubleDataBuffet", doubleDataBuffer);
        FractalShader.SetBuffer(0, "_MultiFrameData",MultiFrameRenderBuffer);
        FractalShader.SetBuffer(0, "_ExperimentalBuffer", ExperimentalBuffer);
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
        
        if ((_currentSample == 0 && !_frameFinished)||Input.GetMouseButton(0) )
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
