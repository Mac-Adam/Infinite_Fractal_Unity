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

public class CameraController : MonoBehaviour
{

    //Controlls Handleing
    int oldMouseTextureCoordinatesX;
    int oldMouseTextureCoordinatesY;
    int PrevScreenX;
    int PrevScreenY;
    //shouldn't be public in the long run
    public static int cpuPrecision = GPUCode.precisions[^1].precision + 5;
    //This have to be initialized,
    //At the moment they don't have a default value
    public FixedPointNumber MiddleX = new(cpuPrecision);
    public FixedPointNumber MiddleY = new(cpuPrecision);
    public FixedPointNumber Scale = new(cpuPrecision);


    //Controls
    float scrollSlowness = 10.0f;

    //other directions are not currenntly needed.
    public int deadZoneRight = 0;

    //flags
    public bool screenSizeChanged = false;
    public bool scrollMoved = false;
    public int shiftX = 0;
    public int shiftY = 0;

    //this script has the be aware of those values:
    public Settings settings;

    public void HandleMouseInput()
    {

        if (Input.mousePosition.x > Screen.width - deadZoneRight)
        {
            return;
        }
        if (Screen.width != PrevScreenX || Screen.height != PrevScreenY)
        {
            return;
        }
        Vector2 mousePosPix = Input.mousePosition;
        int mouseTextureCoordinatesX = OtherFunctions.Reduce((int)mousePosPix.x, settings.pixelizationBase, settings.pixelizationLevel);
        int mouseTextureCoordinatesY = OtherFunctions.Reduce((int)mousePosPix.y, settings.pixelizationBase, settings.pixelizationLevel);


        FixedPointNumber mousePosRealX = new(cpuPrecision);

        mousePosRealX.SetDouble(mouseTextureCoordinatesX - settings.ReducedWidth(false) / 2);
        mousePosRealX = mousePosRealX * Scale + MiddleX;
        FixedPointNumber mousePosRealY = new(cpuPrecision);

        mousePosRealY.SetDouble(mouseTextureCoordinatesY - settings.ReducedHeight(false) / 2);
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
            scrollMoved = true;


        }
        if (mouseTextureCoordinatesX != oldMouseTextureCoordinatesX || mouseTextureCoordinatesY != oldMouseTextureCoordinatesY)
        {
            if (Input.GetMouseButton(0))
            {
               

                shiftX = mouseTextureCoordinatesX - oldMouseTextureCoordinatesX;
                shiftY = mouseTextureCoordinatesY - oldMouseTextureCoordinatesY;

               

                multiplyer.SetDouble(mouseTextureCoordinatesX - oldMouseTextureCoordinatesX);
                MiddleX -= multiplyer * Scale;
                multiplyer.SetDouble(mouseTextureCoordinatesY - oldMouseTextureCoordinatesY);
                MiddleY -= multiplyer * Scale;


            }

        }
        oldMouseTextureCoordinatesX = mouseTextureCoordinatesX;
        oldMouseTextureCoordinatesY = mouseTextureCoordinatesY;


    }
    // Update is called once per frame
    void Update()
    {
        HandleMouseInput();

        screenSizeChanged = PrevScreenX != Screen.width || PrevScreenY != Screen.height;
        PrevScreenX = Screen.width;
        PrevScreenY = Screen.height;
    }
}
