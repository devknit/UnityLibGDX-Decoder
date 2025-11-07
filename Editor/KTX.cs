
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
	
namespace LibGDX.Decoder
{
	public static class KTX
	{
		[MenuItem("Assets/Create/LibGDX Decoder/KTX to PNG")]
		static void KTXtoPNG()
		{
			foreach (var assetGUID in Selection.assetGUIDs)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
				string extension = Path.GetExtension(assetPath);

				if (extension == ".ktx" || extension == ".zktx")
				{
					ToPng(assetPath, Path.ChangeExtension(assetPath, ".png"));
				}
			}
		}
		public static void ToPng( string ktxPath, string outputPngPath)
		{
			if( File.Exists( ktxPath) == false)
			{
				Debug.LogError( $"ktx file not found: {ktxPath}");
				return;
			}
			byte[] fileBytes = File.ReadAllBytes( ktxPath);
			
			if( GZip.IsCompressed( fileBytes) != false)
			{
				fileBytes = GZip.Decompress( fileBytes);
			}
			using( var reader = new BinaryReader( new MemoryStream( fileBytes)))
			{
				uint fileSize = reader.ReadUInt32();
				string identifier = System.Text.Encoding.ASCII.GetString( reader.ReadBytes( 12));
				if( identifier.Contains( "KTX") == false)
				{
					throw new Exception( "Invalid KTX data inside zktx.");
				}
				uint endianness = reader.ReadUInt32();
				bool littleEndian = endianness == 0x04030201;
				
				uint glType = reader.ReadUInt32();
				uint glTypeSize = reader.ReadUInt32();
				uint glFormat = reader.ReadUInt32();
				uint glInternalFormat = reader.ReadUInt32();
				uint glBaseInternalFormat = reader.ReadUInt32();
				uint pixelWidth = reader.ReadUInt32();
				uint pixelHeight = reader.ReadUInt32();
				uint pixelDepth = reader.ReadUInt32();
				uint numberOfArrayElements = reader.ReadUInt32();
				uint numberOfFaces = reader.ReadUInt32();
				uint numberOfMipmapLevels  = reader.ReadUInt32();
				uint bytesOfKeyValueData = reader.ReadUInt32();

				if( littleEndian == false)
				{
					glType = Endian.Reverse( glType);
					glTypeSize = Endian.Reverse( glTypeSize);
					glFormat = Endian.Reverse( glFormat);
					glInternalFormat = Endian.Reverse( glInternalFormat);
					glBaseInternalFormat = Endian.Reverse( glBaseInternalFormat);
					pixelWidth = Endian.Reverse( pixelWidth);
					pixelHeight = Endian.Reverse( pixelHeight);
					pixelDepth = Endian.Reverse( pixelDepth);
					numberOfArrayElements = Endian.Reverse( numberOfArrayElements);
					numberOfFaces = Endian.Reverse( numberOfFaces);
					numberOfMipmapLevels = Endian.Reverse( numberOfMipmapLevels);
					bytesOfKeyValueData = Endian.Reverse( bytesOfKeyValueData);
				}
				reader.BaseStream.Position += bytesOfKeyValueData;
				uint imageSize = reader.ReadUInt32();
				byte[] imageData = reader.ReadBytes( (int)imageSize);
				Color32[] pixels = null;
				
				try
				{
					switch( glInternalFormat)
					{
						case 32856: /* GL_RGBA8 */
						case 32858: /* GL_RGBA12 */
						case 32859: /* GL_RGBA16 */
						{
							pixels = DecodeRGBA( imageData, (int)pixelWidth, (int)pixelHeight, 4);
							break;
						}
						case 32849: /* GL_RGB8 */
						case 32850: /* GL_RGB10 */
						case 32851: /* GL_RGB12 */
						case 32852: /* GL_RGB16 */
						{
							pixels = DecodeRGB( imageData, (int)pixelWidth, (int)pixelHeight, 3);
							break;
						}
						case 36196: /* ETC1_RGB8_OES */
						case 37492: /* COMPRESSED_RGB8_ETC2 */
						case 37494: /* COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 */
						case 37496: /* COMPRESSED_RGBA8_ETC2_EAC */
						{
							pixels = ETC.Decode( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
							break;
						}
						case 33776: /* COMPRESSED_RGB_S3TC_DXT1_EXT */
						case 33777: /* COMPRESSED_RGBA_S3TC_DXT1_EXT */
						case 33778: /* COMPRESSED_RGBA_S3TC_DXT3_EXT */
						case 33779: /* COMPRESSED_RGBA_S3TC_DXT5_EXT */
						// {
						// 	pixels = DecodeS3TC( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						case 35840: /* COMPRESSED_RGB_PVRTC_4BPPV1_IMG */
						case 35841: /* COMPRESSED_RGB_PVRTC_2BPPV1_IMG */
						case 35842: /* COMPRESSED_RGBA_PVRTC_4BPPV1_IMG */
						case 35843: /* COMPRESSED_RGBA_PVRTC_2BPPV1_IMG */
						// {
						// 	pixels = DecodePVRTC( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						case 37808: /* COMPRESSED_RGBA_ASTC_4x4_KHR */
						case 37809: /* COMPRESSED_RGBA_ASTC_5x4_KHR */
						case 37810: /* COMPRESSED_RGBA_ASTC_5x5_KHR */
						case 37811: /* COMPRESSED_RGBA_ASTC_6x5_KHR */
						case 37812: /* COMPRESSED_RGBA_ASTC_6x6_KHR */
						case 37813: /* COMPRESSED_RGBA_ASTC_8x5_KHR */
						case 37814: /* COMPRESSED_RGBA_ASTC_8x6_KHR */
						case 37815: /* COMPRESSED_RGBA_ASTC_8x8_KHR */
						case 37816: /* COMPRESSED_RGBA_ASTC_10x5_KHR */
						case 37817: /* COMPRESSED_RGBA_ASTC_10x6_KHR */
						case 37818: /* COMPRESSED_RGBA_ASTC_10x8_KHR */
						case 37819: /* COMPRESSED_RGBA_ASTC_10x10_KHR */
						case 37820: /* COMPRESSED_RGBA_ASTC_12x10_KHR */
						case 37821: /* COMPRESSED_RGBA_ASTC_12x12_KHR */
						// {
						// 	pixels = DecodeASTC( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						case 35952: /* COMPRESSED_LUMINANCE_LATC1_EXT */
						case 35953: /* COMPRESSED_SIGNED_LUMINANCE_LATC1_EXT */
						case 35954: /* COMPRESSED_LUMINANCE_ALPHA_LATC2_EXT */
						case 35955: /* COMPRESSED_SIGNED_LUMINANCE_ALPHA_LATC2_EXT */
						// {
						// 	pixels = DecodeLATC( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						case 37488: /* COMPRESSED_R11_EAC */
						case 37489: /* COMPRESSED_SIGNED_R11_EAC */
						case 37490: /* COMPRESSED_RG11_EAC */
						case 37491: /* COMPRESSED_SIGNED_RG11_EAC */
						// {
						// 	pixels = DecodeEAC( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						case 35904: /* COMPRESSED_SRGB_EXT */
						case 35907: /* SRGB8_ALPHA8_EXT */
						case 35905: /* SRGB8_EXT */
						case 35906: /* SRGB_ALPHA_EXT */
						case 35908: /* SLUMINANCE_ALPHA_EXT */
						case 35909: /* SLUMINANCE8_ALPHA8_EXT */
						case 35910: /* SLUMINANCE_EXT */
						case 35911: /* SLUMINANCE8_EXT */
						case 35912: /* COMPRESSED_SRGB_EXT */
						case 35913: /* COMPRESSED_SRGB_ALPHA_EXT */
						case 37497: /* COMPRESSED_SRGB8_ALPHA8_ETC2_EAC */
						case 37840: /* COMPRESSED_SRGB8_ALPHA8_ASTC_4x4_KHR */
						// {
						// 	pixels = DecodeSRGBVariants( imageData, (int)pixelWidth, (int)pixelHeight, glInternalFormat);
						// 	break;
						// }
						default:
						{
							throw new NotSupportedException( $"Unsupported glInternalFormat: 0x{glInternalFormat:X} ({glInternalFormat})");
						}
					}
					var tex = new Texture2D( (int)pixelWidth, (int)pixelHeight, TextureFormat.RGBA32, false);
					tex.SetPixels32( pixels);
					tex.Apply();
					
					byte[] png = tex.EncodeToPNG();
					File.WriteAllBytes( outputPngPath, png);
					AssetDatabase.ImportAsset( outputPngPath);
					Debug.Log( $"Converted {ktxPath} ({pixelWidth}x{pixelHeight})\n{outputPngPath}");
				}
				catch( Exception e)
				{
					Debug.LogError( e);
				}
			}
		}
		static Color32[] DecodeRGBA( byte[] srcPixels, int width, int height, int pixelStride)
		{
			var dstPixels = new Color32[ width * height];
			
			for( int i0 = 0; i0 < width * height; ++i0)
			{
				int index = i0 * pixelStride;
				byte r = srcPixels[ index + 0];
				byte g = srcPixels[ index + 1];
				byte b = srcPixels[ index + 2];
				byte a = (pixelStride >= 4)? srcPixels[ index + 3] : (byte)255;
				dstPixels[ i0] = new Color32( r, g, b, a);
			}
			return dstPixels;
		}
		static Color32[] DecodeRGB( byte[] srcPixels, int width, int height, int pixelStride)
		{
			var dstPixels = new Color32[ width * height];
			
			for( int i0 = 0; i0 < width * height; ++i0)
			{
				int index = i0 * pixelStride;
				byte r = srcPixels[ index + 0];
				byte g = srcPixels[ index + 1];
				byte b = srcPixels[ index + 2];
				dstPixels[ i0] = new Color32( r, g, b, 255);
			}
			return dstPixels;
		}
	}
}