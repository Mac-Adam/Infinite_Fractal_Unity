using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShadeContoler : MonoBehaviour
{
    new Camera camera;
    protected RenderTexture targetTexture;
    private void Awake()
    {
        camera = GetComponent<Camera>();
        InitializeBuffers();
        InitializeValues();
        HandleLastValues();
        ResetParams();
        InitializeGui();

    }
    public abstract void InitializeBuffers();
    public abstract void InitializeValues();
    public abstract void HandleLastValues();
    public abstract void ResetParams();
    public abstract void InitializeGui();
    public abstract void HandleKeyInput();
    public abstract void HandleMouseInput();
    public abstract void HandleScreenSizeChange();
    public abstract void HandleGuiUpdates();
    public abstract void DisposeBuffers();
    public abstract void AdditionalCleanup();
    //TODO figure out antialias;
    public abstract void SetShadersParameters();
    public abstract bool ShouldRegerateTexture();
    public abstract void AddiitionalTextureRegenerationHandeling();
    public abstract void InitializeOtherTextures();
    public abstract void DispatchShaders();
    public abstract void HandleAntialias();

    public abstract void BlitTexture(RenderTexture destination);
    public void Exit()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        HandleLastValues();
        HandleKeyInput();
        HandleMouseInput();
        HandleScreenSizeChange();
        HandleGuiUpdates();
        HandleAntialias();

    }
    private void OnDestroy()
    {
        Destroy(targetTexture);
        DisposeBuffers();
        AdditionalCleanup();
    }

    private void InitializeRenderTextures()
    {
        if(targetTexture == null || targetTexture.width != Screen.width || targetTexture.height != Screen.height || ShouldRegerateTexture())
        {
            if(targetTexture != null)
            {
                targetTexture.Release();
            }

            targetTexture = new RenderTexture(Screen.width,Screen.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            targetTexture.enableRandomWrite = true;
            targetTexture.Create();
            AddiitionalTextureRegenerationHandeling();

        }
        InitializeOtherTextures();
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitializeRenderTextures();


        DispatchShaders();

        BlitTexture(destination);


    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShadersParameters();
        Render(destination);
    }


}
