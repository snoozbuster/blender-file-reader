This library was created to read the `.blend` files that Blender outputs. It includes an HTML "renderer" that
shows how to use the library and outputs `.blend` files into a nice HTML file with various niceties for navigation,
so you can puzzle out what each part of the file does and how to use it.

Also included is an unfinished XNA driver; this app will open a Blender file with the `o` key and render all the models
inside. It has layer support in normal Blender fashion; press a number key to switch to a layer, use `shift` + number key to
add/remove that layer from the currently active layers, and use `alt` + number key to target layers 11-20. Although the app
can load textures both packed into the `.blend` file and from disk, I haven't been able to work out how the UV coordinates are
ordered so the textures don't display correctly at all.
There's a few comments hanging around with todo notes or bug notes. You might want to read those before letting this app loose
on your most complex file.

**BE WARNED:** Use the HTML renderer *only* on small `.blend` files. Using it on larger `.blend` files, even `.blend`
files in the 10MB range, will cause HTML outputs exceeding 200MB. A 1.5MB `.blend` file (including packed
texture) generated a 20MB HTML file (which didn't even include the packed texture data). As such, this tool
should be used only as a guide to help figure out how to read `.blend` files and figure out how to use that
data for your own needs. This, of course, only applies to the HTML renderer. The library itself can be used to any purpose.
