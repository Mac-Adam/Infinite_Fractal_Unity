## How to use this app properly

I tried to make this app as intuitive as possible.
That said, it might be not be clear how to take advantage of the implemented features.
Sometimes you might break the app by messing with the options. The fix is simple.
**CLICK THE "R" BUTTON** to start the render correctly. It has always worked for me so if you found a bug not fixed by it, good job.

### Basic Functions

Basic functionality including panning and zooming can be controlled via your mouse.
In order to pan just press the left mouse button and move your mouse.
In order to zoom in scroll up and in order to zoom out scroll down.

### Proper Navigation

Zooming with the scroll wheel is inefficient, because after each scale change whole image has to be rerendered.
In order to navigate smoothly use those keys:

- I immediately zooms in while decreasing render resolution
- U starts upcasing the image behind the scene and gradually puts it on screen
- O immediately zooms out increasing render resolution
- D downcases the image
  If you ever get lost what resolution you are rendering in check the GUI that can be opened by pressing G

### Visual Settings

Before you get the perfect image, tweak the render setting to your liking:

- color theme allows you to change the colors
- max Iterations increases precision ( but it also increases render time )
- color strength controls how wide a color stripe is ( increase this setting if the image is "glitchy" )
- Antyaliasing smooths the image on the cost of computation time (9 times more time)
- Any color palette that has "Normal" or "Distance" in its name uses additional computation in order to get more info. Any image rendered using those palettes will take about 2-3 times as much time

### Screenshots and Video

Pressing S key allows you to take a screenshot.
Use it instead of print screen or any other way for those reasons:

- GUI will always be hidden on screenshots taken this way
- You can increase the resolution above your screen resolution and render an image in 4K or more

Pressing Z key starts a video capture procedure, start it at the final image of the desired video.

the procedure is automatic and doesn't require any other input.
if you insert a move command like zoom or pan the procedure stops!
It will capture a set of frames later used to render a video out of it.
Make sure that the render resolution is double the resolution of the video you want to have

Every screenshot and frame will be saved in /renders folder

### Other Fractals and Julia Sets

You can change the fractal with GUI, or by pressing F.
In order to view a Julia set of some point, press J while hovering over that point. To exit the julia set, press J again.

### A few words about resolution

I have not made any limitations on how much load you put on the GPU this means that if you set your render resolution to high, the program will crash. Some measures were taken so If you are not able to either increase the resolution or decrease the tiling, It's because of those limitations.
For this reason the Tiling slider exists. It splits the screen Into smaller chunks that are much easier on the GPU, This can also reduce the computation time. To much tiles will increase the time.
Your milage may vary, but try to keep resolution of a single tile at:

- 4K for floats
- 4K/fullHD for double
- half less for each precision increase.

### Automatic video rendering with blender

After you captured your frames you can compose them into a video with any video editing software you'd like.
I created a simple blender macro that allows to create this video in blender with just a few clicks.

- Open Blender Video Editor
- Open the text editor window (shift F11)
- Open the /blenderMacro/createVideo.py file in it
- Make sure that every image in the /renders folder is meant for the video ( delete or move old ones )
- ( optional ) Tweak setting to your linking by altering the two variables fps and frames per image
- ( optional ) Open console to see the progress
- run the macro with the run script button ( alt P )

finished video will be saved to /renders folder

You can comment the last line in the gen_video function to only set up the render, you can then tweak some settings and use the build in render functionality.

In short the video will be comprised of still images made at double the zoom. Zooming in at constant speed and then swapping the image for the new one. The swap is mostly seamless though you might see some small artifacts.
Unfortunately images made using the "Distance" color palettes are not compatible with the video generation.

### Some known "bugs"

In this section I will describe a few known "bugs" that you might encounter.
Most of them are features that could cause problems if abused.

- The maxIter slider is log scale. This means that if you move it just slightly the computation time will not increase linearly, it will increase exponentially. For this reason if you slide it to far the render might never finish. Use it with caution.
- Changing colors when using antyaliasing, sometimes will yeld weird results. Click R to fix it
- Super high tiling will slow down the render if unnecessary (this will occur when you do a video render. After some frames have been generated, and the zoom decreased) you can just change the tiling to speed it up
- Increasing the resolution too much without sufficient tiling may crash the program (Precision level > 2 with distance estimation or normal map)
