#define READALL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.DDS
{
    static class LoaderDDS
    {
        #region Constants
        private const byte HeaderSizeInBytes = 128; // all non-image data together is 128 Bytes
        private const uint BitMask = 0x00000007; // bits = 00 00 01 11


        private static readonly NotImplementedException Unfinished = new NotImplementedException("ERROR: Only 2 Dimensional DXT1/3/5 compressed images for now. 1D/3D Textures may not be compressed according to spec.");
        #endregion Constants

        #region Simplified In-Memory representation of the Image
        private static bool _isCompressed;
        private static int _width, _height, _depth, _mipMapCount;
        private static int _bytesForMainSurface; // must be handled with care when implementing uncompressed formats!
        private static byte _bytesPerBlock;
        private static OpenTK.Graphics.OpenGL.PixelInternalFormat _pixelInternalFormat;
        #endregion Simplified In-Memory representation of the Image

        #region Flag Enums
        [Flags] // Surface Description
        private enum eDDSD : uint
        {
            CAPS = 0x00000001, // is always present
            HEIGHT = 0x00000002, // is always present
            WIDTH = 0x00000004, // is always present
            PITCH = 0x00000008, // is set if the image is uncompressed
            PIXELFORMAT = 0x00001000, // is always present
            MIPMAPCOUNT = 0x00020000, // is set if the image contains MipMaps
            LINEARSIZE = 0x00080000, // is set if the image is compressed
            DEPTH = 0x00800000 // is set for 3D Volume Textures
        }

        [Flags] // Pixelformat 
        private enum eDDPF : uint
        {
            NONE = 0x00000000, // not part of DX, added for convenience
            ALPHAPIXELS = 0x00000001,
            FOURCC = 0x00000004,
            RGB = 0x00000040,
            RGBA = 0x00000041
        }

        /// <summary>This list was derived from nVidia OpenGL SDK</summary>
        [Flags] // Texture types
        private enum eFOURCC : uint
        {
            UNKNOWN = 0,
#if READALL
            R8G8B8 = 20,
            A8R8G8B8 = 21,
            X8R8G8B8 = 22,
            R5G6B5 = 23,
            X1R5G5B5 = 24,
            A1R5G5B5 = 25,
            A4R4G4B4 = 26,
            R3G3B2 = 27,
            A8 = 28,
            A8R3G3B2 = 29,
            X4R4G4B4 = 30,
            A2B10G10R10 = 31,
            A8B8G8R8 = 32,
            X8B8G8R8 = 33,
            G16R16 = 34,
            A2R10G10B10 = 35,
            A16B16G16R16 = 36,

            L8 = 50,
            A8L8 = 51,
            A4L4 = 52,

            D16_LOCKABLE = 70,
            D32 = 71,
            D24X8 = 77,
            D16 = 80,

            D32F_LOCKABLE = 82,
            L16 = 81,

            // s10e5 formats (16-bits per channel)
            R16F = 111,
            G16R16F = 112,
            A16B16G16R16F = 113,

            // IEEE s23e8 formats (32-bits per channel)
            R32F = 114,
            G32R32F = 115,
            A32B32G32R32F = 116,
#endif
            DXT1 = 0x31545844,
            DXT2 = 0x32545844,
            DXT3 = 0x33545844,
            DXT4 = 0x34545844,
            DXT5 = 0x35545844,

            BC5_UNORM = 843666497
        }

        [Flags] // dwCaps1
        private enum eDDSCAPS : uint
        {
            NONE = 0x00000000, // not part of DX, added for convenience
            COMPLEX = 0x00000008, // should be set for any DDS file with more than one main surface
            TEXTURE = 0x00001000, // should always be set
            MIPMAP = 0x00400000 // only for files with MipMaps
        }

        [Flags]  // dwCaps2
        private enum eDDSCAPS2 : uint
        {
            NONE = 0x00000000, // not part of DX, added for convenience
            CUBEMAP = 0x00000200,
            CUBEMAP_POSITIVEX = 0x00000400,
            CUBEMAP_NEGATIVEX = 0x00000800,
            CUBEMAP_POSITIVEY = 0x00001000,
            CUBEMAP_NEGATIVEY = 0x00002000,
            CUBEMAP_POSITIVEZ = 0x00004000,
            CUBEMAP_NEGATIVEZ = 0x00008000,
            CUBEMAP_ALL_FACES = 0x0000FC00,
            VOLUME = 0x00200000 // for 3D Textures
        }
        #endregion Flag Enums

        #region Private Members
        private static string _idString; // 4 bytes, must be "DDS "
        private static UInt32 _dwSize; // Size of structure is 124 bytes, 128 including all sub-structs and the header
        private static UInt32 _dwFlags; // Flags to indicate valid fields.
        private static UInt32 _dwHeight; // Height of the main image in pixels
        private static UInt32 _dwWidth; // Width of the main image in pixels
        private static UInt32 _dwPitchOrLinearSize; // For compressed formats, this is the total number of bytes for the main image.
        private static UInt32 _dwDepth; // For volume textures, this is the depth of the volume.
        private static UInt32 _dwMipMapCount; // total number of levels in the mipmap chain of the main image.
#if READALL
        private static UInt32[] _dwReserved1; // 11 UInt32s
#endif
        // Pixelformat sub-struct, 32 bytes
        private static UInt32 _pfSize; // Size of Pixelformat structure. This member must be set to 32.
        private static UInt32 _pfFlags; // Flags to indicate valid fields.
        private static UInt32 _pfFourCc; // This is the four-character code for compressed formats.
#if READALL
        private static UInt32 _pfRgbBitCount; // For RGB formats, this is the total number of bits in the format. dwFlags should include DDpf_RGB in this case. This value is usually 16, 24, or 32. For A8R8G8B8, this value would be 32.
        private static UInt32 _pfRBitMask; // For RGB formats, these three fields contain the masks for the red, green, and blue channels. For A8R8G8B8, these values would be 0x00ff0000, 0x0000ff00, and 0x000000ff respectively.
        private static UInt32 _pfGBitMask; // ..
        private static UInt32 _pfBBitMask; // ..
        private static UInt32 _pfABitMask; // For RGB formats, this contains the mask for the alpha channel, if any. dwFlags should include DDpf_ALPHAPIXELS in this case. For A8R8G8B8, this value would be 0xff000000.
#endif
        // Capabilities sub-struct, 16 bytes
        private static UInt32 _dwCaps1; // always includes DDSCAPS_TEXTURE. with more than one main surface DDSCAPS_COMPLEX should also be set.
        private static UInt32 _dwCaps2; // For cubic environment maps, DDSCAPS2_CUBEMAP should be included as well as one or more faces of the map (DDSCAPS2_CUBEMAP_POSITIVEX, DDSCAPS2_CUBEMAP_NEGATIVEX, DDSCAPS2_CUBEMAP_POSITIVEY, DDSCAPS2_CUBEMAP_NEGATIVEY, DDSCAPS2_CUBEMAP_POSITIVEZ, DDSCAPS2_CUBEMAP_NEGATIVEZ). For volume textures, DDSCAPS2_VOLUME should be included.
#if READALL
        private static UInt32[] _dwReserved2; // 3 = 2 + 1 UInt32
#endif
        #endregion Private Members

        /// <summary>
        /// This function will generate, bind and fill a Texture Object with a DXT1/3/5 compressed Texture in .dds Format.
        /// MipMaps below 4x4 Pixel Size are discarded, because DXTn's smallest unit is a 4x4 block of Pixel data.
        /// It will set correct MipMap parameters, Filtering, Wrapping and EnvMode for the Texture. 
        /// The only call inside this function affecting OpenGL State is GL.BindTexture();
        /// </summary>
        /// <param name="filename">The name of the file you wish to load, including path and file extension.</param>
        /// <param name="texturehandle">0 if invalid, otherwise a Texture Object usable with GL.BindTexture().</param>
        /// <param name="dimension">0 if invalid, will output what was loaded (typically Texture1D/2D/3D or Cubemap)</param>
        public static void LoadFromStream(byte[] _RawDataFromFile, out int texturehandle, out OpenTK.Graphics.OpenGL.TextureTarget dimension, out int oWidth, out int oHeight)
        {
            #region Prep data
            // invalidate whatever it was before
            dimension = (OGL.TextureTarget)0;
            texturehandle = 0;
            ErrorCode GLError = ErrorCode.NoError;

            _isCompressed = false;
            _width = 0;
            _height = 0;
            _depth = 0;
            _mipMapCount = 0;
            _bytesForMainSurface = 0;
            _bytesPerBlock = 0;
            _pixelInternalFormat = OGL.PixelInternalFormat.Rgba8;
            #endregion

            #region Try
            try // Exceptions will be thrown if any Problem occurs while working on the file. 
            {
                #region Translate Header to less cryptic representation
                ConvertDX9Header(ref _RawDataFromFile); // The first 128 Bytes of the file is non-image data

                // start by checking if all forced flags are present. Flags indicate valid fields, but aren't written by every tool .....
                if (_idString != "DDS " || // magic key
                     _dwSize != 124 || // constant size of struct, never reused
                     _pfSize != 32 || // constant size of struct, never reused
                     !CheckFlag(_dwFlags, (uint)eDDSD.CAPS) ||        // must know it's caps
                     !CheckFlag(_dwFlags, (uint)eDDSD.PIXELFORMAT) || // must know it's format
                     !CheckFlag(_dwCaps1, (uint)eDDSCAPS.TEXTURE)     // must be a Texture
                    )
                    throw new ArgumentException("ERROR: File has invalid signature or missing Flags.");

                #region Examine Flags
                if (CheckFlag(_dwFlags, (uint)eDDSD.WIDTH))
                    _width = (int)_dwWidth;
                else
                    throw new ArgumentException("ERROR: Flag for Width not set.");

                if (CheckFlag(_dwFlags, (uint)eDDSD.HEIGHT))
                    _height = (int)_dwHeight;
                else
                    throw new ArgumentException("ERROR: Flag for Height not set.");

                oWidth = _width;
                oHeight = _height;

                var pixelFormat = OGL.PixelFormat.Rgba;

                if (CheckFlag(_dwFlags, (uint)eDDSD.DEPTH) && CheckFlag(_dwCaps2, (uint)eDDSCAPS2.VOLUME))
                {
                    dimension = OGL.TextureTarget.Texture3D; // image is 3D Volume
                    _depth = (int)_dwDepth;
                    throw Unfinished;
                }
                else
                {// image is 2D or Cube
                    if (CheckFlag(_dwCaps2, (uint)eDDSCAPS2.CUBEMAP))
                    {
                        dimension = OGL.TextureTarget.TextureCubeMap;
                        _depth = 6;
                    }
                    else
                    {
                        dimension = OGL.TextureTarget.Texture2D;
                        _depth = 1;
                    }
                }

                // these flags must be set for mipmaps to be included
                if (CheckFlag(_dwCaps1, (uint)eDDSCAPS.MIPMAP) && CheckFlag(_dwFlags, (uint)eDDSD.MIPMAPCOUNT))
                    _mipMapCount = (int)_dwMipMapCount; // image contains MipMaps
                else
                    _mipMapCount = 1; // only 1 main image

                // Should never happen
                if (CheckFlag(_dwFlags, (uint)eDDSD.PITCH) && CheckFlag(_dwFlags, (uint)eDDSD.LINEARSIZE))
                    throw new ArgumentException("INVALID: Pitch AND Linear Flags both set. Image cannot be uncompressed and DTXn compressed at the same time.");

                // This flag is set if format is uncompressed RGB RGBA etc.
                if (CheckFlag(_dwFlags, (uint)eDDSD.PITCH))
                {
                    // _BytesForMainSurface = (int) dwPitchOrLinearSize; // holds bytes-per-scanline for uncompressed
                    _isCompressed = false;
                    throw Unfinished;
                }

                // This flag is set if format is compressed DXTn.
                if (CheckFlag(_dwFlags, (uint)eDDSD.LINEARSIZE))
                {
                    _bytesForMainSurface = (int)_dwPitchOrLinearSize;
                    _isCompressed = true;
                }
                #endregion Examine Flags

                var pixelType = OGL.PixelType.Float; // Only used for uncompressed formats

                if (CheckFlag(_pfFlags, (uint)eDDPF.FOURCC))
                {
                    switch ((eFOURCC)_pfFourCc)
                    {
                        case eFOURCC.DXT1:
                            _pixelInternalFormat = (OGL.PixelInternalFormat)ExtTextureCompressionS3tc.CompressedRgbS3tcDxt1Ext;
                            _bytesPerBlock = 8;
                            _isCompressed = true;
                            break;
                        //case eFOURCC.DXT2:
                        case eFOURCC.DXT3:
                            _pixelInternalFormat = (OGL.PixelInternalFormat)ExtTextureCompressionS3tc.CompressedRgbaS3tcDxt3Ext;
                            _bytesPerBlock = 16;
                            _isCompressed = true;
                            break;
                        //case eFOURCC.DXT4:
                        case eFOURCC.DXT5:
                            _pixelInternalFormat = (OGL.PixelInternalFormat)ExtTextureCompressionS3tc.CompressedRgbaS3tcDxt5Ext;
                            _bytesPerBlock = 16;
                            _isCompressed = true;
                            break;
                        case eFOURCC.A32B32G32R32F:
                            _pixelInternalFormat = (OGL.PixelInternalFormat)OGL.PixelInternalFormat.Rgba32f;
                            _isCompressed = false;
                            _bytesPerBlock = 16;
                            pixelType = OGL.PixelType.Float;
                            break;
                        case eFOURCC.BC5_UNORM:
                            _pixelInternalFormat = (OGL.PixelInternalFormat)(int)OpenTK.Graphics.OpenGL4.PixelInternalFormat.CompressedRgRgtc2;
                            _isCompressed = true;
                            _bytesPerBlock = 16;
                            break;
                        default:
                            throw Unfinished; // handle uncompressed formats 
                    }
                }
                else
                {
                    // TODO: Do not assume ARGB8
                    _pixelInternalFormat = OGL.PixelInternalFormat.Rgba8;
                    _isCompressed = false;
                    _bytesPerBlock = 4;
                    pixelType = OGL.PixelType.UnsignedInt8888Reversed;
                    pixelFormat = OGL.PixelFormat.Bgra;
                    /*sourceFormat = convertPixelFormat(pfRGBBitCount,
								pfRBitMask, pfGBitMask,
								pfBBitMask,
								(CheckFlag(pfFlags, (uint)eDDPF.ALPHAPIXELS) ?pfABitMask : 0);*/
                }

                // Works, but commented out because some texture authoring tools don't set this flag.
                /* Safety Check, if file is only 1x 2D surface without mipmaps, eDDSCAPS.COMPLEX should not be set
				if ( CheckFlag( dwCaps1, (uint) eDDSCAPS.COMPLEX ) )
				{
					if ( result == eTextureDimension.Texture2D && _MipMapCount == 1 ) // catch potential problem
						Trace.WriteLine( "Warning: Image is declared complex, but contains only 1 surface." );
				}*/

                #endregion Translate Header to less cryptic representation

                #region send the Texture to GL
                #region Generate and Bind Handle
                GL.GenTextures(1, out texturehandle);
                GL.BindTexture(dimension, texturehandle);
                #endregion Generate and Bind Handle

                int Cursor = HeaderSizeInBytes;
                // foreach face in the cubemap, get all it's mipmaps levels. Only one iteration for Texture2D
                for (int Slices = 0; Slices < _depth; Slices++)
                {
                    int trueMipMapCount = _mipMapCount - 1;
                    int Width = _width;
                    int Height = _height;
                    for (int Level = 0; Level < _mipMapCount; Level++) // start at base image
                    {
                        #region determine Dimensions
                        int BlocksPerRow = (Width + 3) >> 2;
                        int BlocksPerColumn = (Height + 3) >> 2;
                        int SurfaceBlockCount = BlocksPerRow * BlocksPerColumn; //   // DXTn stores Texels in 4x4 blocks, a Color block is 8 Bytes, an Alpha block is 8 Bytes for DXT3/5
                        int SurfaceSizeInBytes = SurfaceBlockCount * _bytesPerBlock;

                        if (!_isCompressed)
                        {
                            SurfaceSizeInBytes = Width * Height * _bytesPerBlock;
                        }

                        // this check must evaluate to false for 2D and Cube maps, or it's impossible to determine MipMap sizes.
                        #endregion determine Dimensions

                        // skip mipmaps smaller than a 4x4 Pixels block, which is the smallest DXTn unit.
                        if (Width > 2 && Height > 2 || !_isCompressed)
                        { // Note: there could be a potential problem with non-power-of-two cube maps
                            #region Prepare Array for TexImage
                            byte[] RawDataOfSurface = new byte[SurfaceSizeInBytes];
                            //if (!TextureLoaderParameters.FlipImages)
                            { // no changes to the image, copy as is
                                Array.Copy(_RawDataFromFile, Cursor, RawDataOfSurface, 0, SurfaceSizeInBytes);
                            }

                            #endregion Prepare Array for TexImage

                            #region Create TexImage
                            switch (dimension)
                            {
                                case OGL.TextureTarget.Texture2D:
                                    if (!_isCompressed)
                                    {
                                        GL.TexImage2D(OGL.TextureTarget.Texture2D,
                                            Level,
                                            _pixelInternalFormat,
                                            Width,
                                            Height,
                                            0,
                                            pixelFormat,
                                            pixelType,
                                            RawDataOfSurface
                                            );
                                    }
                                    else
                                    {
                                        GL.CompressedTexImage2D(OGL.TextureTarget.Texture2D,
                                                                 Level,
                                                                 (OGL.InternalFormat)(int)_pixelInternalFormat,
                                                                 Width,
                                                                 Height,
                                                                0,
                                                                 SurfaceSizeInBytes,
                                                                 RawDataOfSurface);
                                    }
                                    break;
                                case OGL.TextureTarget.TextureCubeMap:
                                    if (!_isCompressed)
                                    {
                                        GL.TexImage2D(OGL.TextureTarget.TextureCubeMapPositiveX + Slices,
                                            Level,
                                            _pixelInternalFormat,
                                            Width,
                                            Height,
                                            0,
                                            OGL.PixelFormat.Rgba, // TODO: don't hardcode this dude
                                            pixelType,
                                            RawDataOfSurface
                                            );
                                    }
                                    else
                                    {
                                        GL.CompressedTexImage2D(OGL.TextureTarget.TextureCubeMapPositiveX + Slices,
                                                                 Level,
                                                                 (OGL.InternalFormat)(int)_pixelInternalFormat,
                                                                 Width,
                                                                 Height,
                                                                 0,
                                                                 SurfaceSizeInBytes,
                                                                 RawDataOfSurface);
                                    }
                                    break;
                                case OGL.TextureTarget.Texture1D: // Untested
                                case OGL.TextureTarget.Texture3D: // Untested
                                default:
                                    throw new ArgumentException("ERROR: Use DXT for 2D Images only. Cannot evaluate " + dimension);
                            }
                            #endregion Create TexImage

                            #region Query Success
                            GLError = GL.GetError();
                            if (GLError != ErrorCode.NoError)
                            {
                                GL.DeleteTextures(1, ref texturehandle);
                                throw new ArgumentException("ERROR: Something went wrong after GL.CompressedTexImage(); Last GL Error: " + GLError.ToString());
                            }
                            #endregion Query Success
                        }
                        else
                        {
                            if (trueMipMapCount > Level)
                                trueMipMapCount = Level - 1; // The current Level is invalid
                        }

                        #region Prepare the next MipMap level
                        Width /= 2;
                        if (Width < 1)
                            Width = 1;
                        Height /= 2;
                        if (Height < 1)
                            Height = 1;
                        Cursor += SurfaceSizeInBytes;
                        #endregion Prepare the next MipMap level
                    }

                    #region Set States properly
                    GL.TexParameter(dimension, (TextureParameterName)All.TextureBaseLevel, 0);
                    GL.TexParameter(dimension, (TextureParameterName)All.TextureMaxLevel, trueMipMapCount);

                    int TexMaxLevel;
                    GL.GetTexParameter(dimension, GetTextureParameter.TextureMaxLevel, out TexMaxLevel);

                    #endregion Set States properly
                }

                #region Set Texture Parameters

                GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                //GLError = GL.GetError();
                //if (GLError != ErrorCode.NoError)
                //{
                //    throw new ArgumentException("Error setting Texture Parameters. GL Error: " + GLError);
                //}
                #endregion Set Texture Parameters

                // If it made it here without throwing any Exception the result is a valid Texture.
                return; // success
                #endregion send the Texture to GL
            }
            catch (Exception e)
            {
                dimension = (OGL.TextureTarget)0;
                texturehandle = 0;
                throw new ArgumentException("ERROR: Exception caught when attempting to load file " + ".\n" + e + "\n" + GetDescriptionFromFile());
                // return; // failure
            }
            finally
            {
                _RawDataFromFile = null; // clarity, not really needed
            }
            #endregion Try
        }

        #region Helpers
        private static void ConvertDX9Header(ref byte[] input)
        {
            UInt32 offset = 0;
            _idString = GetString(ref input, offset);
            offset += 4;
            _dwSize = GetUInt32(ref input, offset);
            offset += 4;
            _dwFlags = GetUInt32(ref input, offset);
            offset += 4;
            _dwHeight = GetUInt32(ref input, offset);
            offset += 4;
            _dwWidth = GetUInt32(ref input, offset);
            offset += 4;
            _dwPitchOrLinearSize = GetUInt32(ref input, offset);
            offset += 4;
            _dwDepth = GetUInt32(ref input, offset);
            offset += 4;
            _dwMipMapCount = GetUInt32(ref input, offset);
            offset += 4;
#if READALL
            _dwReserved1 = new UInt32[11]; // reserved
#endif
            offset += 4 * 11;
            _pfSize = GetUInt32(ref input, offset);
            offset += 4;
            _pfFlags = GetUInt32(ref input, offset);
            offset += 4;
            _pfFourCc = GetUInt32(ref input, offset);
            offset += 4;
#if READALL
            _pfRgbBitCount = GetUInt32(ref input, offset);
            offset += 4;
            _pfRBitMask = GetUInt32(ref input, offset);
            offset += 4;
            _pfGBitMask = GetUInt32(ref input, offset);
            offset += 4;
            _pfBBitMask = GetUInt32(ref input, offset);
            offset += 4;
            _pfABitMask = GetUInt32(ref input, offset);
            offset += 4;
#else
			offset += 20;
#endif
            _dwCaps1 = GetUInt32(ref input, offset);
            offset += 4;
            _dwCaps2 = GetUInt32(ref input, offset);
            offset += 4;
#if READALL
            _dwReserved2 = new UInt32[3]; // offset is 4+112 here, + 12 = 4+124 
#endif
            offset += 4 * 3;
        }

        /// <summary> Returns true if the flag is set, false otherwise</summary>
        private static bool CheckFlag(uint variable, uint flag)
        {
            return (variable & flag) > 0 ? true : false;
        }

        private static string GetString(ref byte[] input, uint offset)
        {
            return "" + (char)input[offset + 0] + (char)input[offset + 1] + (char)input[offset + 2] + (char)input[offset + 3];
        }

        private static uint GetUInt32(ref byte[] input, uint offset)
        {
            return (uint)(((input[offset + 3] * 256 + input[offset + 2]) * 256 + input[offset + 1]) * 256 + input[offset + 0]);
        }

        private static uint GetUInt24(ref byte[] input, uint offset)
        {
            return (uint)((input[offset + 2] * 256 + input[offset + 1]) * 256 + input[offset + 0]);
        }

        private static void GetBytesFromUInt24(ref byte[] input, uint offset, uint splitme)
        {
            input[offset + 0] = (byte)(splitme & 0x000000ff);
            input[offset + 1] = (byte)((splitme & 0x0000ff00) >> 8);
            input[offset + 2] = (byte)((splitme & 0x00ff0000) >> 16);
            return;
        }

        /// <summary>DXT5 Alpha block flipping, inspired by code from Evan Hart (nVidia SDK)</summary>
        private static uint FlipUInt24(uint inputUInt24)
        {
            byte[][] ThreeBits = new byte[2][];
            for (int i = 0; i < 2; i++)
                ThreeBits[i] = new byte[4];

            // extract 3 bits each into the array
            ThreeBits[0][0] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[0][1] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[0][2] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[0][3] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[1][0] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[1][1] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[1][2] = (byte)(inputUInt24 & BitMask);
            inputUInt24 >>= 3;
            ThreeBits[1][3] = (byte)(inputUInt24 & BitMask);

            // stuff 8x 3bits into 3 bytes
            uint Result = 0;
            Result = Result | (uint)(ThreeBits[1][0] << 0);
            Result = Result | (uint)(ThreeBits[1][1] << 3);
            Result = Result | (uint)(ThreeBits[1][2] << 6);
            Result = Result | (uint)(ThreeBits[1][3] << 9);
            Result = Result | (uint)(ThreeBits[0][0] << 12);
            Result = Result | (uint)(ThreeBits[0][1] << 15);
            Result = Result | (uint)(ThreeBits[0][2] << 18);
            Result = Result | (uint)(ThreeBits[0][3] << 21);
            return Result;
        }
        #endregion Helpers

        #region String Representations
        private static string GetDescriptionFromFile()
        {
            return "\n--> Heade " +
                   "\nID: " + _idString +
                   "\nSize: " + _dwSize +
                   "\nFlags: " + _dwFlags + " (" + (eDDSD)_dwFlags + ")" +
                   "\nHeight: " + _dwHeight +
                   "\nWidth: " + _dwWidth +
                   "\nPitch: " + _dwPitchOrLinearSize +
                   "\nDepth: " + _dwDepth +
                   "\nMipMaps: " + _dwMipMapCount +
                   "\n\n---PixelFormat---" +
                   "\nSize: " + _pfSize +
                   "\nFlags: " + _pfFlags + " (" + (eDDPF)_pfFlags + ")" +
                   "\nFourCC: " + _pfFourCc + " (" + (eFOURCC)_pfFourCc + ")" +
#if READALL
 "\nBitcount: " + _pfRgbBitCount +
                   "\nBitMask Red: " + _pfRBitMask +
                   "\nBitMask Green: " + _pfGBitMask +
                   "\nBitMask Blue: " + _pfBBitMask +
                   "\nBitMask Alpha: " + _pfABitMask +
#endif
 "\n\n---Capabilities---" +
                   "\nCaps1: " + _dwCaps1 + " (" + (eDDSCAPS)_dwCaps1 + ")" +
                   "\nCaps2: " + _dwCaps2 + " (" + (eDDSCAPS2)_dwCaps2 + ")";
        }

        private static string GetDescriptionFromMemory(string filename, OpenTK.Graphics.OpenGL.TextureTarget Dimension)
        {
            return "\nFile: " + filename +
                   "\nDimension: " + Dimension +
                   "\nSize: " + _width + " * " + _height + " * " + _depth +
                   "\nCompressed: " + _isCompressed +
                   "\nBytes for Main Image: " + _bytesForMainSurface +
                   "\nMipMaps: " + _mipMapCount;
        }
        #endregion String Representations
    }
}
