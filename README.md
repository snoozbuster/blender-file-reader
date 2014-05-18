This program was created to read the `.blend` files that Blender outputs. It outputs them into a nice
HTML file for navigation. I think I fixed all the bugs, so feel free to use it however you like.

**BE WARNED:** Use this tool *only* on small `.blend` files. Using it on larger `.blend` files, even `.blend`
files in the 10MB range, will cause HTML outputs exceeding 200MB. A 1.5MB `.blend` file (including packed
texture) generated a 20MB HTML file (which didn't even include the packed texture data). As such, this tool
should be used only as a guide to help figure out how to read `.blend` files and figure out how to use that
data for your own needs.

Notes:
------
This could be improved by detecting when a primitive points to a raw data block and rendering the value of
the block as that primitive somewhere, but this doesn't seem too necessary as those particular primitives are
generally Blender meta-information, such as console history and things like that. 
