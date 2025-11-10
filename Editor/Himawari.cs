
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Sprites;

namespace LibGDX.Decoder
{
	public static class Himawari
	{
		enum TextScope
		{
			ImageFileName,
			ImageSize,
			ImageFormat,
			ImageFilterMode,
			ImageWarpMode,
			SpriteName,
			SpriteRotate,
			SpritePositionUV,
			SpriteSizeUV,
			SpriteOriginal,
			SpriteOffset,
			SpriteIndex,
		};
		[MenuItem( "Assets/Create/Himawari/TXT to Sprites")]
		static void TXTtoSprites()
		{
			var textures = new List<Texture>();
			
			foreach( string assetGUID in Selection.assetGUIDs)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath( assetGUID);
				
				if( string.IsNullOrEmpty( assetPath) == false)
				{
					string directory = Path.GetDirectoryName( assetPath);
					var newSpriteRects = new List<SpriteRect>();
					TextureImporter importer = null;
					SpriteRect newSpriteRect = null;
					
					using( var reader = new StreamReader( assetPath))
					{
						string imageAssetPath = string.Empty;
						string imageAssetGUID = string.Empty;
						string pngAssetPath = string.Empty;
						string pngAssetGUID = string.Empty;
						string line;
						
						while( (line = reader.ReadLine()) != null)
						{
							if( importer == null)
							{
								imageAssetPath = Path.Combine( directory, line).Replace( @"\", "/");
								imageAssetGUID = AssetDatabase.AssetPathToGUID( imageAssetPath);
								pngAssetPath = Path.ChangeExtension( imageAssetPath, ".png");
								pngAssetGUID = AssetDatabase.AssetPathToGUID( pngAssetPath);
								
								if( string.IsNullOrEmpty( pngAssetGUID) != false || File.Exists( pngAssetPath) == false)
								{
									if( string.IsNullOrEmpty( imageAssetGUID) == false && File.Exists( imageAssetPath) != false)
									{
										switch( Path.GetExtension( imageAssetPath))
										{
											case ".cim":
											{
												CIM.ToPng( imageAssetPath, pngAssetPath);
												break;
											}
											case ".ktx":
											case ".zktx":
											{
												KTX.ToPng( imageAssetPath, pngAssetPath);
												break;
											}
										}
									}
								}
								importer = AssetImporter.GetAtPath( pngAssetPath) as TextureImporter;
								
								if( importer == null)
								{
									throw new Exception( $"Not found image assset. {pngAssetPath}");
								}
							}
							else if( line.StartsWith( "size: ") != false)
							{
								string[] parts = line.Substring( line.IndexOf( ":") + 1).Split( ',');
								
								if( parts.Length != 2)
								{
									throw new Exception( $"Invalid property. {line}");
								}
							}
							else if( line.StartsWith( "format: ") != false)
							{
								TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings( "Standalone");
								TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings( "Android");
								TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings( "iPhone");
								string value = line.Substring( line.IndexOf( ": ") + 2);
								
								standalone.format = value switch
								{
									"RGBA8888" => TextureImporterFormat.RGBA32,
									"RGBA4444" => TextureImporterFormat.RGBA16,
									_ => throw new Exception( $"Invalid format. {value}")
								};
								android.format = value switch
								{
									"RGBA8888" => TextureImporterFormat.RGBA32,
									"RGBA4444" => TextureImporterFormat.RGBA16,
									_ => throw new Exception( $"Invalid format. {value}")
								};
								ios.format = value switch
								{
									"RGBA8888" => TextureImporterFormat.RGBA32,
									"RGBA4444" => TextureImporterFormat.RGBA16,
									_ => throw new Exception( $"Invalid format. {value}")
								};
							}
							else if( line.StartsWith( "filter: ") != false)
							{
								string value = line.Substring( line.IndexOf( ": ") + 2);
								
								if( value == "Linear,Linear")
								{
									importer.filterMode = FilterMode.Bilinear;
								}
								else
								{
									throw new Exception( $"Invalid filterMode. {line}");
								}
							}
							else if( line.StartsWith( "repeat: ") != false)
							{
								string value = line.Substring( line.IndexOf( ": ") + 2);
								
								if( value == "none")
								{
									importer.wrapMode = TextureWrapMode.Clamp;
								}
								else
								{
									throw new Exception( $"Invalid warpMode. {value}");
								}
							}
							else if( line.Length > 0)
							{
								if( newSpriteRect != null)
								{
									if( line.StartsWith( "  rotate:") != false)
									{
										/* どういう風に扱うのか現段階では不明 */
									}
									else if( line.StartsWith( "  xy:") != false)
									{
										string[] parts = line.Substring( line.IndexOf( ": ") + 2).Split( ',');
										int x = int.Parse( parts[ 0]);
										int y = int.Parse( parts[ 1]);
										newSpriteRect.rect = new Rect( x, y, 0, 0);
									}
									else if( line.StartsWith( "  size:") != false)
									{
										string[] parts = line.Substring( line.IndexOf( ": ") + 2).Split( ',');
										int width = int.Parse( parts[ 0]);
										int height = int.Parse( parts[ 1]);
										importer.GetSourceTextureWidthAndHeight( out int textureWidth, out int textureHeight);
										newSpriteRect.rect = new Rect( newSpriteRect.rect.x, textureHeight - height - newSpriteRect.rect.y, width, height);
									}
									else if( line.StartsWith( "  orig:") != false)
									{
										/* どういう風に扱うのか現段階では不明 */
									}
									else if( line.StartsWith( "  offset:") != false)
									{
										/* どういう風に扱うのか現段階では不明 */
									}
									else if( line.StartsWith( "  index:") != false)
									{
										/* どういう風に扱うのか現段階では不明 */
										newSpriteRects.Add( newSpriteRect);
										newSpriteRect = null;
									}
									else
									{
										throw new Exception( $"Invalid property. {line}");
									}
								}
								else
								{
									newSpriteRect = new SpriteRect
									{
										name = line
									};
								}
							}
							else
							{
								if( newSpriteRects.Count > 0)
								{
									var factory = new SpriteDataProviderFactories();
									factory.Init();
									var dataProvider = factory.GetSpriteEditorDataProviderFromObject( importer);
									dataProvider.InitSpriteEditorDataProvider();
									var currentSpriteRects = dataProvider.GetSpriteRects().ToDictionary( x => x.name, x => x);
									
									foreach( var spriteRect in newSpriteRects)
									{
										if( currentSpriteRects.TryGetValue( spriteRect.name, out var currentSpriteRect) != false)
										{
											spriteRect.spriteID = spriteRect.spriteID;
										}
									}
									dataProvider.SetSpriteRects( newSpriteRects.ToArray());
									dataProvider.Apply();
								}
								importer.SaveAndReimport();
								newSpriteRects.Clear();
								importer = null;
							}
						}
					}
					if( newSpriteRects.Count > 0)
					{
						var factory = new SpriteDataProviderFactories();
						factory.Init();
						var dataProvider = factory.GetSpriteEditorDataProviderFromObject( importer);
						dataProvider.InitSpriteEditorDataProvider();
						var currentSpriteRects = dataProvider.GetSpriteRects().ToDictionary( x => x.name, x => x);
						
						foreach( var spriteRect in newSpriteRects)
						{
							if( currentSpriteRects.TryGetValue( spriteRect.name, out var currentSpriteRect) != false)
							{
								spriteRect.spriteID = spriteRect.spriteID;
							}
						}
						dataProvider.SetSpriteRects( newSpriteRects.ToArray());
						dataProvider.Apply();
					}
					importer.SaveAndReimport();
				}
			}
		}
		class Texture
		{
			public string fileName;
			public int width;
			public int height;
			public TextureFormat format;
			public FilterMode uFilter;
			public FilterMode vFilter;
			public TextureWrapMode warpMode;
			public List<Sprite> sprites;
		}
		class Sprite
		{
			public string name;
			public int x;
			public int y;
			public int width;
			public int height;
			public int origX;
			public int origY;
			public int offsetX;
			public int offsetY;
			public int index;
		}
	}
}