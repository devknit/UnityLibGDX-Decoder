
using System;
using System.IO;
using System.IO.Compression;
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
		static byte[] DecompressZlib( byte[] data)
		{
			using( var input = new MemoryStream( data))
			{
				input.ReadByte(); // 0x78
				input.ReadByte(); // 0x9c
				using( var deflate = new DeflateStream( input, CompressionMode.Decompress))
				using( var output = new MemoryStream())
				{
					deflate.CopyTo( output);
					return output.ToArray();
				}
			}
		}
		static int Reverse( int value)
		{
			return	(value & 0xff) << 24
				|	((value >> 8) & 0xff) << 16
				|	((value >> 16) & 0xff) << 8
				|	((value >> 24) & 0xff);
		}
		static void ToPng( string cimPath, string outputPngPath)
		{
			if( File.Exists( cimPath) == false)
			{
				Debug.LogError( $"cim file not found: {cimPath}");
				return;
			}
			byte[] fileBytes = File.ReadAllBytes( cimPath);
			fileBytes = DecompressZlib( fileBytes);
			
			using( var reader = new BinaryReader( new MemoryStream( fileBytes)))
			{
				int width = Reverse( reader.ReadInt32());
				int height = Reverse( reader.ReadInt32());
				int formatOrdinal = Reverse( reader.ReadInt32());
				int pixelLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
				byte[] pixelBytes = reader.ReadBytes( pixelLength);
				
				if( GZip.IsCompressed( pixelBytes) != false)
				{
					pixelBytes = GZip.Decompress( pixelBytes);
				}
				Color32[] pixels = ToRGBA( pixelBytes, width, height, formatOrdinal);
				
				var tex = new Texture2D( width, height, TextureFormat.RGBA32, false);
				tex.SetPixels32( pixels);
				tex.Apply();
				
				byte[] png = tex.EncodeToPNG();
				File.WriteAllBytes( outputPngPath, png);
				Debug.Log( $"Converted {cimPath}, ({width}x{height}), {GetFormatName( formatOrdinal)}\n{outputPngPath}");
			}
		}
		static void FlipVertical( Color32[] pixels, int width, int height)
		{
			int half = height / 2;
			
			for( int y = 0; y < half; ++y)
			{
				int yTop = y * width;
				int yBottom = (height - 1 - y) * width;
				
				for( int x = 0; x < width; ++x)
				{
					Color32 temp = pixels[ yTop + x];
					pixels[ yTop + x] = pixels[ yBottom + x];
					pixels[ yBottom + x] = temp;
				}
			}
		}
		static Color32[] ToRGBA( byte[] src, int width, int height, int format)
		{
			var dst = new Color32[ width * height];
			int dstIndex = 0;
			
			{
				switch( format)
				{
					/* GDX2D_FORMAT_ALPHA  */
					case 1:
					{
						for( int i0 = 0; i0 < src.Length; ++i0)
						{
							dst[ dstIndex++] = new Color32( 255, 255, 255, src[ i0]);
						}
						break;
					}
					/* GDX2D_FORMAT_LUMINANCE_ALPHA  */
					case 2:
					{
						for( int i0 = 0; i0 < src.Length; i0 += 2)
						{
							dst[ dstIndex++] = new Color32( src[ i0 + 0], src[ i0 + 0], src[ i0 + 0], src[ i0 + 1]);
						}
						break;
					}
					/* GDX2D_FORMAT_RGB888  */
					case 3:
					{
						for( int i0 = 0; i0 < src.Length; i0 += 3)
						{
							dst[ dstIndex++] = new Color32( src[ i0 + 0], src[ i0 + 1], src[ i0 + 2], 255);
						}
						break;
					}
					/* GDX2D_FORMAT_RGBA8888  */
					case 4:
					{
						for( int i0 = 0; i0 < src.Length; i0 += 4)
						{
							dst[ dstIndex++] = new Color32( src[ i0 + 0], src[ i0 + 1], src[ i0 + 2], src[ i0 + 3]);
						}
						break;
					}
					/* GDX2D_FORMAT_RGB565  */
					case 5:
					{
						for( int i0 = 0; i0 < src.Length; i0 += 2)
						{
							ushort packed = BitConverter.ToUInt16( src, i0);
							dst[ dstIndex++] = new Color32( 
								(byte)(((packed >> 11) & 0x1f) * 255 / 31), 
								(byte)(((packed >> 5) & 0x3f) * 255 / 63), 
								(byte)((packed & 0x1f) * 255 / 31), 
								255);
						}
						break;
					}
					/* GDX2D_FORMAT_RGBA4444  */
					case 6:
					{
						for( int i0 = 0; i0 < src.Length; i0 += 2)
						{
							ushort packed = BitConverter.ToUInt16( src, i0);
							dst[ dstIndex++] = new Color32( 
								(byte)(((packed >> 12) & 0xf) * 17), 
								(byte)(((packed >> 8) & 0xf) * 17), 
								(byte)(((packed >> 4) & 0xf) * 17), 
								(byte)(((packed >> 4) & 0xf) * 17));
						}
						break;
					}
					default:
					{
						throw new Exception( $"Unknown Pixmap.Format. ordinal: {format}");
					}
				}
			}
			FlipVertical( dst, width, height);
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