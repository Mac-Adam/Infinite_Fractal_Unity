## How It works?

In this file you can check a short explanation of how the code works.
This is not a comprehensive documentation.

### General Overview

MandelbrotController takes care of connecting all the pieces together. It acts when other components (for example one responsible for camera movements or GUI) requests some action. It dispatches shaders, controls the buffers and shows the graphic on screen.

### Crunching the numbers

After all the calculation settings have been set (position scale, number of iterations etc.) Render buffers are cleared (with a shader to save time). IterBuffer is a buffer for the finished render data that is later used for coloring. It is always the size of the generated frame. MultiFrameBuffer is a buffer for saving calculation data, it stores current values of z (and its derivative). When a fractal shader is dispatched it gets the current values and makes some iterations. If the pixel is finished (we are sure it diverges) it is skipped. The calculation can be scheduled only for a smaller part (tiling). This is necessary for high zoom since the numbers take a lot of space and the buffers have a limited size

### Salvaging the data

When panning a special copy shader is called. This shader moves the data into the correct place in the buffer. This saves a ton of time. Since the shader is executed for pixels in random order, the coping is done to another buffer that is then switched at the end(this is implemented as a single buffer with twice the necessary space)

Similar method is used then zooming in/out a special shader is called that calculates where the data should be moved to in order to preserve the image.

### Coloring the fractal

The way the fractals are colored is nicely described [here](https://www.math.univ-toulouse.fr/~cheritat/wiki-draw/index.php/Mandelbrot_set).

#### Basic methods

While iterating the series starting from a specific point, two situations are possible:

- It diverges to infinity
- It converges to a single point or to a cycle of some length.
  If it converges (or we are not yet sure due to insufficient number of iterations) It is colored black.
  If it diverges we can color it based on how many iterations it took (If $|z| > 2$ we can be sure the series will diverge).
  The problem with that approach is that the image is stripey then (you can see this if you turn of smooth gradient) To get a smooth color gradient we can iterate until $|z|$ is bigger and then take into account the exact value of $|z|$ for example two points may diverge after 100 iterations but $|z_1|=1500$ while $|z_2|=2000$. Using this value you can get a smooth gradient (getOffset function in floatMandelbrot shader implements this functionality)

#### Other Methods

This is not the only way to color the fractal. One of the ways is to use a texture and map the pixels onto that texture based on the escape angle. You can do that in few different ways. You can use square tilings that have a special geometry illustrated in[M.C. Escher Square Limit, 1964](https://www.nga.gov/collection/art-object-page.135604.html).
Or you can use images of some kinds of rings that will be "unrolled" into a strip.
Those methods are quite interesting although they don't look very good because the details near the sets are to fine and the image looks glitchy.

Another, much more visually pleasing way is to use additional information hidden in the derivative of the series. You can either estimate the distance from the point to the set, or you can use it to find the direction away from the set, which can be later used as a normal map for a simple light simulation. This makes the render look 3d.

If the coloring palettes provided be me are not what you are looking for, feel free to add your own in the Colors.cs file.

### Calculate to any precision

As described in the README.md numbers are represented by an array of ints. Typical long addition and multiplication algorithms can be applied. C# version only is able to use fixed point numbers. It is easier to use since you can overload operators, but it is less optimized than the GPU version. It doesn't matter since CPU only needs to calculate the middle of the screen to this precision.

GPU has two versions of the numbering system, fixed point and floating point. The fixed point is faster, although Its use is limited. Couldn't quite figure out how to write the code in such a way that the compile times are reasonable.
For extreme precision ($10^{-100}$ and so on) the shaders can compile for tens of minutes.

If you have any questions about how some part of the code works, have a good idea or anything else, let me know.
