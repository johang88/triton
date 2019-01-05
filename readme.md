Triton Game Engine
=====================
This might become a game engine sometime in the future, but right now a lot of core functionality is missing.

## Features

### Renderer
* Tiled deferred rendering
* IBL
* PBR
* FXAA, SMAA
* Shadow Maps
    * Point & spot light shadows
    * CSM for directional lights
    * Poisson tap PCF
* HDR
* Filmic tonemapping (Reinard / Unhcarted / Filmic / ASEC)
* Bloom
* Eye adaptation
* SSAO

## Dependencies
* OpenTK
* ImageMagick
* Bullet physics
* SharpFileSystem
* Dear ImGui

## Building
Open up `Triton.sln` and hit compile.

## Samples
There are several samples available in the samples folder. Actually ... just the one.

## Content pipeline
The content processor will process all media files in the specified folder and write optimized versions to the specified output folder. The `nVidia command lines tools` has to be installed and present in the PATH in order for the content processor to function properly.

Example: `ContentProcessor.exe in=..\Media out=..Data'

### Supported content file formats
#### Meshes
* Ogre mesh format
* FBX + Whatever Assimp supports, probably

#### Textures
* tga
* png
* bmp
* dds

#### Audio
* ogg

### Texture naming convention
* `_d` => diffuse map
* `_n` => normal map

## Assemblies
* ContentProcessor - Front end executable for the content processor.
* Triton.Common - Virtual file system, resource manager, logging and more.
* Triton.Content - Contains all content compilers (ie meshes, textures and skeletons).
* Triton.Game - Game base system.
* Triton.Audio - Audio subsystem using ogg and OpenAL.
* Triton.Graphics - Contains the high level renderers and the core graphics backend class, the backend class can be used to feed the rendering thread with commands. There are also various utilities and resource loaders in here.
* Triton.Input - Input system
* Triton.Math - Math classes including Vector2, Vector3, Matrix4 and Quaternion.
* Triton.Physics - Simple wrapper for the JitterPhysics library.
* Triton.Renderer - Core renderer, manages all GL state and provides wrapper functions for all common functionality. This is the low level renderer implementation, use the rendering backend in `Triton.Graphics` to issue the actual commands as thread safety is not guaranteed by the core renderer.

![Cewl screenshot](screenshot.jpg?raw=true "Basic Scene Sample")
![Cewl screenshot](screenshot2.jpg?raw=true "Tiled deferred lighting")
![Cewl screenshot](screenshot_modelviewer.jpg?raw=true "Model viewer")
