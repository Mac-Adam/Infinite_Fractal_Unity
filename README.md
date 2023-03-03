# Fractal Generator

![Main](./ScreenShots/2.png)

## What Are Fractals?

    To put it simply fractals are mathematical shapes containing infinite detail.
    This detail can be achieved In many different ways. Some fractals are self similar.
    Some never repeat. Mandelbrot set is Quasi self similar.
    This means that after you zoom in far enough you will find copies of the original set,
    they will never be identical thought, this can be seen in the image above
    (this is one of the copies found in the "tip" on the negative real number line)

## Mandelbrot set

    Mandelbrot set is a fractal that lives in the realm of [Complex Numbers] (https://en.wikipedia.org/wiki/Complex_number, "complex numbers").

## How is it generated?

    each pixel on the screen is assigned a number based on its position.
    Horizontal axis is responsible for the real part while Vertical axis is responsible for imaginary part.
    Once a pixel is assigned it's number $c$ We define a series
    $$z_{n+1} = z_{n}^{2} + c$$
    $$z_{0} = 0$$
    If this series diverges to infinity this pixel is colored if it stays low it is colored black.

## Where do the colors come from?

    If the |z| > 4 we can be sure that the series diverges, we can therefore stop the computation and get the iteration count.
    In order to smooth out the gradient the |z| is taken into account.
    Using this data, color is calculated using a CIELAB or CIELCh color space
