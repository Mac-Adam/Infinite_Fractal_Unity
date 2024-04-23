using System;
using System.Collections.Generic;
using UnityEngine;
using FixedPointNumberSystem;
using CommonFunctions;
namespace CommonShaderRenderFunctions
{
    public struct ShaderInfo
    {
        public string internalName;
        public string outsideName;
        // Turns out different fractals need diferent ones for optimal rendering
        // For fixed point calculations the one int reserved for the whole part of a number is to small when the fractal escapes too quick
        public int bailoutRadius;

        public ShaderInfo(string internalName, string outsideName,int bailoutRadius)
        {
            this.internalName = internalName;
            this.outsideName = outsideName;
            this.bailoutRadius = bailoutRadius;
        }

    }
    public enum Precision { FLOAT = 0, DOUBLE = 1, INFINTE = 2 };
    struct PixelizationData //shortcut to keep everything condensed
    {
        public int reducedWidth;
        public int reducedHeight;
        public int lastReducedWidth;
        public int lastReducedHeight;
        public int pixelCount;
        public int lastPixelCount;
        public int pixelizationBase;
        public int register;
        public PixelizationData(int reducedWidth, int reducedHeight, int lastReducedWidth,int lastReducedHeight, int pixelCount, int lastPixelCount, int pixelizationBase, int register)
        {
            this.reducedWidth = reducedWidth;
            this.reducedHeight = reducedHeight;
            this.lastReducedWidth = lastReducedWidth;
            this.lastReducedHeight = lastReducedHeight;
            this.pixelCount = pixelCount;
            this.lastPixelCount = lastPixelCount;
            this.pixelizationBase = pixelizationBase;
            this.register = register;
        }

    }

    //In order to keep everythong condensed this stuct is used to keep tack of every value that describes the state of renderer
    // this takes into account only the values needed to describe equelibriun state (shiftX,etc. are excluded)
    public struct Settings
    {
        public bool upscaling;
        public int register;
        public bool zoomVideo;
        public int pixelizationBase;
        public int pixelizationLevel;
        public int lastPixelizationLevel;
        public uint currentSample;
        public int maxAntiAliasyncReruns;
        public Vector2[] antialiasLookupTable;
        public bool frankensteinRendering;
        public int frankensteinSteps;
        public int frankensteinX;
        public int frankensteinY;
        public Precision precision;
        public int precisionLevel;
        public int iterPerCycle;
        public bool doAntialasing;
        public int shaderNumber;
    
        //There are probably better ways to do this, but I want to initialize it to the same thing allways
        public Settings(bool _)
        {
            upscaling = false;
            register = 0;
            zoomVideo = false;
            pixelizationBase = 2;
            pixelizationLevel = 0;
            lastPixelizationLevel = 0;
            currentSample = 0;
            maxAntiAliasyncReruns = 9;
            antialiasLookupTable = Antialiasing.antialiasLookupTableSharp;
            frankensteinRendering = false;
            frankensteinSteps = 1;
            frankensteinX = 0;
            frankensteinY = 0;
            precision = Precision.FLOAT;
            precisionLevel = 1;
            iterPerCycle = 50;
            doAntialasing = false;
            shaderNumber = 0;
        }



        public int GetShaderPre()
        {
            return GPUCode.precisions[precisionLevel].precision;
        }
        public int GetShaderPixelSize(bool distance)
        {
            if (distance)
            {
                return 4 * GetShaderPre() + 5;
            }
            else
            {
                return 2 * GetShaderPre() + 5;
            }
            
        }


        public int FrankensteinCorrection()
        {
            if (frankensteinRendering)
            {
                return frankensteinSteps;
            }
            return 1;

        }
        public int PixelCount(bool frankenstein = true)
        {
            return ReducedHeight(frankenstein) * ReducedWidth(frankenstein);

        }
        public int ReducedWidth(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, pixelizationLevel);

        }
        public int ReducedHeight(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.height, pixelizationBase, pixelizationLevel);
        }

        public int LastPixelCount( bool frankenstein = true)
        {
            return LastReducedHeight( frankenstein) * LastReducedWidth( frankenstein);
        }
        public  int LastReducedWidth(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.width, pixelizationBase, lastPixelizationLevel);
        }
        public  int LastReducedHeight(bool frankenstein = true)
        {
            if (frankenstein)
            {
                return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel) / FrankensteinCorrection();
            }
            return OtherFunctions.Reduce(Screen.height, pixelizationBase, lastPixelizationLevel);
        }


        public int MaxPixelizationLevel(bool distance)
        {
            int max = 6; //This will allways be a valid level
            long pixelCount;
            long bufferSize = 0;
            long iterSize;
            do
            {
                max--;
                pixelCount = OtherFunctions.Reduce(Screen.width, pixelizationBase, max) * OtherFunctions.Reduce(Screen.height, pixelizationBase, max);
                iterSize = pixelCount * 3 * sizeof(int);
                switch (precision)
                {
                    case Precision.FLOAT:
                        bufferSize = 2 * pixelCount * PixelSizes.floatSize;
                        break;
                    case Precision.DOUBLE:
                        bufferSize = 2 * pixelCount * PixelSizes.doubleSize;
                        break;
                    case Precision.INFINTE:
                        bufferSize = 2 * pixelCount * sizeof(int) * GetShaderPixelSize(distance);
                        break;
                }

            } while (bufferSize <= PixelizedShaders.MAXBYTESPERBUFFER * FrankensteinCorrection() && iterSize <= PixelizedShaders.MAXBYTESPERBUFFER); ;
            return max + 1;
        }

    }
    //values keept in this struct change frequently
    //Every value is prone to changes so the devision is arbitrary
    public struct DynamicSettings 
    {
        public int currIter;
        public bool reset;
        public bool turboReset;
        public int shiftX;
        public int shiftY;
        public bool renderFinished; // Signals if the whole render is finished (all antialias steps are finished)
        public bool frameFinished; // Signals if the current antialiast step is finished
        public bool stepFinished; // Signals if the subrender is finished
        public float renderStatTime;
        public float renderTimeElapsed;
    }

    public struct PixelSizes
    {
        //since it's not that big of a deal for floats and doubles, the deriviative and distance will always be stored 
        public static int floatSize = sizeof(float) * 7 + sizeof(uint)* 2;
        // I don't get it. For some reason there has to be place for 4 floats eaven though only 3 are used
        public static int doubleSize = sizeof(double)* 4 + sizeof(uint)* 2 + sizeof(float)* 4;
        public static int iter = sizeof(int) * 2 + 3 * sizeof(float);
    }

    class PixelizedShaders
    {
        //When adding a new fractal you need to:
        // place Its info here
        // add edit iteration and potencial calculation in the shaders
        static public readonly ShaderInfo[] fractalInfos = new ShaderInfo[]
        {
            new("MANDELBROT","Mandelbrot",1000),
            new("BURNING_SHIP","Burning Ship",128),
            new("MANDEL3","Mandelbrot cube",32),
            new("MANDEL4","Mandelbrot 4th",12)
        };
        

        public const uint MAXBYTESPERBUFFER = 2147483648;
        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int reducedWidth, int reducedHeight, bool additionalCondition,Action callback,bool smallSize = false)
        {
            if (texture == null || texture.width != reducedWidth || texture.height != reducedHeight || additionalCondition)
            {

                if (texture != null)
                    texture.Release();

                texture = new RenderTexture(reducedWidth, reducedHeight, 0,
                   smallSize ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGBFloat
                   , RenderTextureReadWrite.Linear)
                {
                    enableRandomWrite = true
                };
                texture.Create();
                callback.Invoke();
            }
            return texture;
        }
        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int reducedWidth, int reducedHeight, bool smallSize = false)
        {
            return InitializePixelizedTexture(texture, reducedWidth, reducedHeight, false, () => { }, smallSize);
        }

        public static void Dispatch(ComputeShader shader, RenderTexture texture)
        {
            //TODO figure out how to fix the edge glithces when the size is not a multiple of 8
            int ThreadGrupsX = Mathf.CeilToInt((float)texture.width / 8);
            int ThreadGrupsY = Mathf.CeilToInt((float)texture.height / 8);
            shader.SetTexture(0, "Result", texture);
            shader.Dispatch(0, ThreadGrupsX, ThreadGrupsY, 1);

        }
    }
    class Antialiasing
    {

        static public Vector2[] antialiasLookupTableSmooth = {
        new Vector2(0,0),
        new Vector2(-2.0f/3,-2.0f/3),
        new Vector2(-2.0f/3,0),
        new Vector2(-2.0f/3,2.0f/3),
        new Vector2(0,2.0f/3),
        new Vector2(2.0f/3,2.0f/3),
        new Vector2(2.0f/3,0),
        new Vector2(2.0f/3,-2.0f/3),
        new Vector2(0,-2.0f/3),

    };
        static public Vector2[] antialiasLookupTableSharp = {
        new Vector2(0,0),
        new Vector2(-1.0f/3,-1.0f/3),
        new Vector2(-1.0f/3,0),
        new Vector2(-1.0f/3,1.0f/3),
        new Vector2(0,1.0f/3),
        new Vector2(1.0f/3,1.0f/3),
        new Vector2(1.0f/3,0),
        new Vector2(1.0f/3,-1.0f/3),
        new Vector2(0,-1.0f/3),

    };

    }





}