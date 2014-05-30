Triton Game Engine
=====================
This might become a game engine sometime in the future, but right now a lot of core functionality is missing.

## Dependencies
* OpenTK
* nVidia dds command lines tools
* ServiceStack.Text
* JitterPhysics
* SharpFileSystem

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

## Assemblies
* ContentProcessor - Front end executable for the content processor.
* Test - Test application
* Triton.Common - Virtual file system, resource manager, logging and more.
* Triton.Content - Contains all content compilers (ie meshes, textures and skeletons).
* Triton.Game - Game base system.
* Triton.Graphics - Contains the high level renderers and the core graphics backend class, the backend class can be used to feed the rendering thread with commands. There are also various utilities and resource loaders in here.
* Triton.Input - Input system
* Triton.Math - Math classes including Vector2, Vector3, Matrix4 and Quaternion.
* Triton.Physics - Simple wrapper for the JitterPhysics library.
* Triton.Renderer - Core renderer, contains some enumerations and helper classes for vertex formats, all sub systems are exposed in the `RenderSystem` facade. This is the low level renderer implementation, use the rendering backend in `Triton.Graphics` to issue commands.

## Future development
* Audio subsystem
* Entity management
* Physics subsystem
* Editor