Triton Game Engine
=====================
This might become a game engine sometime in the future, but right now a lot of core functionality is missing.

## Dependencies
* OpenTK
* nVidia dds command lines tools
* ServiceStack.Text

## Building
Open up `Triton.sln` and hit compile.

## Samples
Currently there is only the test application. The test resources are not available for the public at the moment. But it should run perfectly fine if you get some resources of your own.

## Content pipeline
The content processor will process all media files in the specified folder and write optimized versions to the specified output folder. The `nVidia command lines tools` has to be installed and present in the PATH in order for the content processor to function properly.

Example: `ContentProcessor.exe in=..\Media out=..Data'

### Supported content file formats

* Ogre .mesh.xml, .skeleton.xml 
* .tga, .png, .bmp, .jpg, .dds

### Texture naming convention
* `_d` => diffuse map
* `_n` => normal map
* `_s` => specular map, gloss in alpha channel

## Future development
* Audio subsystem
* Entity management
* Physics subsystem
* Editor