using CommonFunctions;
using UnityEngine;
using System;
namespace CommonShaderRenderFunctions
{
    struct PixelizationData //shortcut to keep everything condensed
    {
        int pixelsPerPixel;
        int lastPixelsPerPixel;
        int pixelCount;
        int lastPixelCount;
        int pixelizationBase;
        public PixelizationData(int pixelsPerPixel,int lastPixelsPerPixel, int pixelCount, int lastPixelCount, int pixelizationBase)
        {
            this.pixelsPerPixel = pixelsPerPixel;
            this.lastPixelsPerPixel = lastPixelsPerPixel;
            this.pixelCount = pixelCount;
            this.lastPixelCount = lastPixelCount;
            this.pixelizationBase = pixelizationBase;
        }
        

    }
    class PixelizedShaders
    {

        public static RenderTexture InitializePixelizedTexture(RenderTexture texture, int pixelizationBase, int pixelizationLevel, bool additionalCondition = false)
        {

            if (texture == null || texture.width != Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel) || texture.height != Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel) || additionalCondition)
            {

                if (texture != null)
                    texture.Release();

                texture = new RenderTexture(Screen.width / MathFunctions.IntPow(pixelizationBase, pixelizationLevel), Screen.height / MathFunctions.IntPow(pixelizationBase, pixelizationLevel), 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                {
                    enableRandomWrite = true
                };
                texture.Create();


            }
            return texture;
        }

        public static void Dispatch(ComputeShader RenderShader, ComputeShader DummyShader, RenderTexture targetTexture, RenderTexture dummyTexture, int pixelizationBase, int pixelizationLevel)
        {
            int RenderThreadGrupsX = Mathf.CeilToInt(Screen.width / 8);
            int RenderThreadGrupsY = Mathf.CeilToInt(Screen.height / 8);
            int CalculatethreadGroupsX = Mathf.CeilToInt(Screen.width / (8 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));
            int CalculatethreadGroupsY = Mathf.CeilToInt(Screen.height / (8 * MathFunctions.IntPow(pixelizationBase, pixelizationLevel)));


            DummyShader.SetTexture(0, "Result", dummyTexture);
            DummyShader.Dispatch(0, CalculatethreadGroupsX, CalculatethreadGroupsY, 1);


            RenderShader.SetTexture(0, "Result", targetTexture);
            RenderShader.Dispatch(0, RenderThreadGrupsX, RenderThreadGrupsY, 1);

        }

        public static void HandleZoomPixelization<T>(ComputeBuffer Buffer, int sizeofT, bool zoomIn, PixelizationData pixelizationData, int register, Action<bool> resetSetCallback, Action<ComputeBuffer> setBuffers, int arrayCount = 1)
        {

        }

    }
    class Antialiasing
    {
        public static void BlitWitthAntialiasing(uint currentSample,bool frameFinished,RenderTexture destination,RenderTexture renderedTexture,Material addMaterial,Action NewFrameCallback)
        {
            if ((currentSample == 0 && !frameFinished) || Input.GetMouseButton(0))
            {
                Graphics.Blit(renderedTexture, destination);


            }
            else if (frameFinished)
            {


                addMaterial.SetFloat("_Sample", currentSample);
                Graphics.Blit(renderedTexture, destination, addMaterial);
                NewFrameCallback.Invoke();
               
            }

        }


    }





}