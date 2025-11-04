
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
	
namespace LibGDX.Decoder
{
	public static class CIM
	{
		[MenuItem( "Assets/Create/LibGDX Decoder/CIM to PNG")]
		static void KTXtoPNG()
		{
			foreach( var assetGUID in Selection.assetGUIDs)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( assetGUID);
				string extension = Path.GetExtension( assetPath);
				
				if( extension == ".cim")
				{
					ToPng( assetPath, Path.ChangeExtension( assetPath, ".png"));
				}
			}
		}
		static void ToPng( string cimPath, string outputPngPath)
		{
			if( File.Exists( cimPath) == false)
			{
				Debug.LogError( $"cim file not found: {cimPath}");
				return;
			}
			byte[] fileBytes = File.ReadAllBytes( cimPath);
			
			using( var reader = new BinaryReader( new MemoryStream( fileBytes)))
			{
				int version = reader.ReadInt32();
				int width = reader.ReadInt32();
				int height = reader.ReadInt32();
				int formatOrdinal = reader.ReadInt32();
				byte[] pixelBytes = reader.ReadBytes( 
					(int)(reader.BaseStream.Length - reader.BaseStream.Position));
				
				if( GZip.IsCompressed( pixelBytes) != false)
				{
					pixelBytes = GZip.Decompress( pixelBytes);
				}
				byte[] rgba = ToRGBA( pixelBytes, width, height, formatOrdinal);
				var tex = new Texture2D( width, height, TextureFormat.RGBA32, false);
				tex.LoadRawTextureData( rgba);
				tex.Apply();
				
				byte[] png = tex.EncodeToPNG();
				File.WriteAllBytes( outputPngPath, png);
				Debug.Log( $"Converted {cimPath}, ({width}x{height}), {GetFormatName( formatOrdinal)}\n{outputPngPath}");
			}
		}
		static byte[] ToRGBA( byte[] src, int width, int height, int format)
		{
			int pixelCount = width * height;
			byte[] dst = new byte[pixelCount * 4];
			int srcIndex = 0, dstIndex = 0;
			
			switch( format)
			{
				/* Alpha */
				case 0:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						byte alpha = src[ srcIndex++];
						dst[ dstIndex++] = 255;
						dst[ dstIndex++] = 255;
						dst[ dstIndex++] = 255;
						dst[ dstIndex++] = alpha;
					}
					break;
				}
				/* Intensity */
				case 1:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						byte color = src[ srcIndex++];
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = 255;
					}
					break;
				}
				/* LuminanceAlpha */
				case 2:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						byte color = src[ srcIndex++];
						byte alpha = src[ srcIndex++];
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = color;
						dst[ dstIndex++] = alpha;
					}
					break;
				}
				/* RGB565 */
				case 3:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						ushort value = (ushort)(src[ srcIndex++] | (src[ srcIndex++] << 8));
						byte r = (byte)(((value >> 11) & 0x1f) * 255 / 31);
						byte g = (byte)(((value >> 5) & 0x3f) * 255 / 63);
						byte b = (byte)((value & 0x1f) * 255 / 31);
						dst[ dstIndex++] = r;
						dst[ dstIndex++] = g;
						dst[ dstIndex++] = b;
						dst[ dstIndex++] = 255;
					}
					break;
				}
				/* RGBA4444 */
				case 4:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						ushort value = (ushort)(src[ srcIndex++] | (src[ srcIndex++] << 8));
						byte r = (byte)(((value >> 12) & 0xf) * 17);
						byte g = (byte)(((value >> 8) & 0xf) * 17);
						byte b = (byte)(((value >> 4) & 0xf) * 17);
						byte a = (byte)((value & 0xf) * 17);
						dst[ dstIndex++] = r;
						dst[ dstIndex++] = g;
						dst[ dstIndex++] = b;
						dst[ dstIndex++] = a;
					}
					break;
				}
				/* RGB888 */
				case 5:
				{
					for( int i0 = 0; i0 < pixelCount; ++i0)
					{
						byte r = src[ srcIndex++];
						byte g = src[ srcIndex++];
						byte b = src[ srcIndex++];
						dst[ dstIndex++] = r;
						dst[ dstIndex++] = g;
						dst[ dstIndex++] = b;
						dst[ dstIndex++] = 255;
					}
					break;
				}
				/* RGBA8888 */
				case 6:
				{
					Buffer.BlockCopy( src, 0, dst, 0, Math.Min( src.Length, dst.Length));
					break;
				}
				default:
				{
					throw new Exception( $"Unknown Pixmap.Format. ordinal: {format}");
				}
			}
			return dst;
		}
		static string GetFormatName( int ordinal)
		{
			return ordinal switch
			{
				0 => "Alpha",
				1 => "Intensity",
				2 => "LuminanceAlpha",
				3 => "RGB565",
				4 => "RGBA4444",
				5 => "RGB888",
				6 => "RGBA8888",
				_ => $"Unknown({ordinal})"
			};
		}
	}
}