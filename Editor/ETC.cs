
using System;
using UnityEngine;
	
namespace LibGDX.Decoder
{
	public static class ETC
	{
		public static Color32[] Decode( byte[] etcData, int width, int height, uint glInternalFormat)
		{
			int blockWidth = width / 4;
			int blockHeight = height / 4;
			var pixels = new Color32[ width * height];
			int offset = 0;
			
			for( int by = 0; by < blockHeight; ++by)
			{
				for( int bx = 0; bx < blockWidth; ++bx)
				{
					switch( glInternalFormat)
					{
						case 36196: /* ETC1_RGB8_OES */
						case 37492: /* COMPRESSED_RGB8_ETC2 */
						{
							Decode1Or2Block( etcData, offset, pixels, bx * 4, by * 4, width);
							offset += 8;
							break;
						}
						case 37494: /* COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 */
						{
							DecodePunchthroughBlock( etcData, offset, pixels, bx * 4, by * 4, width);
							offset += 8;
							break;
						}
						case 37496: /* COMPRESSED_RGBA8_ETC2_EAC */
						{
							DecodeEACBlock( etcData, offset, pixels, bx * 4, by * 4, width);
							offset += 16;
							break;
						}
						default:
						{
							throw new NotSupportedException( $"Unsupported glInternalFormat: 0x{glInternalFormat:X} ({glInternalFormat})");
						}
					}
				}
			}
			return pixels;
		}
		static void Decode1Or2Block( byte[] data, int offset, Color32[] pixels, int bx, int by, int width)
		{
			ulong block = BitConverter.ToUInt64( data, offset);
			block = ReverseBytes( block);
			
			bool diffBit = ((block >> 33) & 1) != 0;
			bool flipBit = ((block >> 32) & 1) != 0;
			int r1, g1, b1, r2, g2, b2;
			
			if( diffBit != false)
			{
				r1 = (int)((block >> 59) & 0x1f);
				g1 = (int)((block >> 51) & 0x1f);
				b1 = (int)((block >> 43) & 0x1f);
				int dr = SignExtend3( (int)((block >> 56) & 0x7));
				int dg = SignExtend3( (int)((block >> 48) & 0x7));
				int db = SignExtend3( (int)((block >> 40) & 0x7));
				r2 = Mathf.Clamp(r1 + dr, 0, 31);
				g2 = Mathf.Clamp(g1 + dg, 0, 31);
				b2 = Mathf.Clamp(b1 + db, 0, 31);
			}
			else
			{
				r1 = (int)((block >> 60) & 0xf) * 0x11;
				g1 = (int)((block >> 52) & 0xf) * 0x11;
				b1 = (int)((block >> 44) & 0xf) * 0x11;
				r2 = (int)((block >> 56) & 0xf) * 0x11;
				g2 = (int)((block >> 48) & 0xf) * 0x11;
				b2 = (int)((block >> 40) & 0xf) * 0x11;
			}
			int table1 = (int)((block >> 37) & 0x7);
			int table2 = (int)((block >> 34) & 0x7);
			
			for( int py = 0; py < 4; ++py)
			{
				for( int px = 0; px < 4; ++px)
				{
					int pixelIdx = py * 4 + px;
					int msb = (int)((block >> (15 + pixelIdx)) & 1);
					int lsb = (int)((block >> pixelIdx) & 1);
					int code = (msb << 1) | lsb;
					
					bool second = flipBit ? (py >= 2) : (px >= 2);
					int r = second ? r2 : r1;
					int g = second ? g2 : g1;
					int b = second ? b2 : b1;
					
					int modifier = kModifierTables[ second ? table2 : table1][code];
					r = Mathf.Clamp( (r * 255 / 31) + modifier, 0, 255);
					g = Mathf.Clamp( (g * 255 / 31) + modifier, 0, 255);
					b = Mathf.Clamp( (b * 255 / 31) + modifier, 0, 255);
					pixels[ (by + py) * width + bx + px] = new Color32( (byte)r, (byte)g, (byte)b, 255);
				}
			}
		}
		static void DecodePunchthroughBlock( byte[] data, int offset, Color32[] pixels, int bx, int by, int width)
		{
			ulong block = BitConverter.ToUInt64( data, offset);
			block = ReverseBytes( block);
			
			var temp = new Color32[ 16];
			Decode1Or2Block( data, offset, temp, 0, 0, 4);
			
			for( int py = 0; py < 4; ++py)
			{
				for( int px = 0; px < 4; ++px)
				{
					int pixelIdx = py * 4 + px;
					int alphaBit = (int)((block >> (63 - pixelIdx)) & 1);
					Color32 c = temp[pixelIdx];
					c.a = (byte)(alphaBit == 1 ? 255 : 0);
					pixels[ (by + py) * width + bx + px] = c;
				}
			}
		}
		static void DecodeEACBlock( byte[] data, int offset, Color32[] pixels, int bx, int by, int width)
		{
			var alphaBlock = new byte[ 8];
			var colorBlock = new byte[ 8];
			Array.Copy( data, offset, alphaBlock, 0, 8);
			Array.Copy( data, offset + 8, colorBlock, 0, 8);
			
			byte[] alpha = DecodeEACAlpha( alphaBlock);
			var colorPixels = new Color32[ 16];
			Decode1Or2Block( colorBlock, 0, colorPixels, 0, 0, 4);
			
			for( int i0 = 0; i0 < 16; ++i0)
			{
				Color32 c = colorPixels[ i0];
				c.a = alpha[ i0];
				int px = i0 % 4;
				int py = i0 / 4;
				pixels[ (by + py) * width + bx + px] = c;
			}
		}
		static byte[] DecodeEACAlpha( byte[] block)
		{
			int baseCode = block[ 0];
			int tableIdx = block[ 1] & 0xf;
			int multiplier = block[ 1] >> 4;
			ulong bits = 0;
			
			for( int i0 = 2; i0 < 8; ++i0)
			{
				bits |= ((ulong)block[ i0]) << (8 * (i0 - 2));
			}
			var alpha = new byte[ 16];
			
			for( int i0 = 0; i0 < 16; ++i0)
			{
				int code = (int)((bits >> (i0 * 3)) & 0x7);
				int mod = kEACAlphaModifiers[ tableIdx][ code];
				int a = Mathf.Clamp( baseCode + mod * multiplier, 0, 255);
				alpha[ i0] = (byte)a;
			}
			return alpha;
		}
		static int SignExtend3( int v)
		{
			v &= 0x7;
			return (v & 0x4) != 0 ? v - 8 : v;
		}
		static ulong ReverseBytes( ulong v)
		{
			return 	(  (0x00000000000000FFUL & v) << 56)
					| ((0x000000000000FF00UL & v) << 40)
					| ((0x0000000000FF0000UL & v) << 24)
					| ((0x00000000FF000000UL & v) << 8)
					| ((0x000000FF00000000UL & v) >> 8)
					| ((0x0000FF0000000000UL & v) >> 24)
					| ((0x00FF000000000000UL & v) >> 40)
					| ((0xFF00000000000000UL & v) >> 56);
		}
		static readonly int[][] kModifierTables =
		{
			new int[]{  2,  8, -2,  -8 },
			new int[]{  5, 17, -5, -17 },
			new int[]{  9, 29, -9, -29 },
			new int[]{ 13, 42, -13, -42 },
			new int[]{ 18, 60, -18, -60 },
			new int[]{ 24, 80, -24, -80 },
			new int[]{ 33,106, -33,-106 },
			new int[]{ 47,183, -47,-183 }
		};
		static readonly int[][] kEACAlphaModifiers =
		{
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 },
			new int[]{ 0,  8,  -8, 17, -17, 29, -29, 42 }
		};
	}
}