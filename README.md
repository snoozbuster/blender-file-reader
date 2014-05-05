This program was created to read the .blend files that Blender outputs. It outputs them into a nice
HTML file for navigation. I think I fixed all the bugs, so feel free to use it however you like.

Notes:
------
This could be improved by detecting when a primitive points to a raw data block and rendering the value of
the block as that primitive somewhere, but this doesn't seem too necessary as those particular primitives are
generally Blender meta-information, such as console history and things like that. 
