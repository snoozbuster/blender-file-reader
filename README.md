This library was created to read the `.blend` files that Blender outputs. It includes an HTML "renderer" that
shows how to use the library and outputs `.blend` files into a nice HTML file with various niceties for navigation,
so you can puzzle out what each part of the file does and how to use it.

**BE WARNED:** Use the HTML renderer *only* on small `.blend` files. Using it on larger `.blend` files, even `.blend`
files in the 10MB range, will cause HTML outputs exceeding 200MB. A 1.5MB `.blend` file (including packed
texture) generated a 20MB HTML file (which didn't even include the packed texture data). As such, this tool
should be used only as a guide to help figure out how to read `.blend` files and figure out how to use that
data for your own needs. This, of course, only applies to the HTML renderer. The library itself can be used to any purpose.
