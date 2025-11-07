
using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEditor;
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
		static Vector2Int ParsePair( string value, int indexOf)
		{
			string[] parts = value.Substring( indexOf + 1).Split(',');
			return new Vector2Int( int.Parse( parts[ 0]), int.Parse( parts[ 1]));
		}
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
					
					using( var reader = new StreamReader( assetPath))
					{
						TextureImporter importer = null;
						var sprites = new List<SpriteRect>();
						SpriteRect sprite = null;
						string line;
						
						string imageAssetPath = string.Empty;
						string imageAssetGUID = string.Empty;
						string pngAssetPath = string.Empty;
						string pngAssetGUID = string.Empty;
						
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
									_ => throw new Exception( $"Invalid format. {value}")
								};
								android.format = value switch
								{
									"RGBA8888" => TextureImporterFormat.RGBA32,
									_ => throw new Exception( $"Invalid format. {value}")
								};
								ios.format = value switch
								{
									"RGBA8888" => TextureImporterFormat.RGBA32,
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
								if( sprite != null)
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
										sprite.rect = new Rect( x, y, 0, 0);
									}
									else if( line.StartsWith( "  size:") != false)
									{
										string[] parts = line.Substring( line.IndexOf( ": ") + 2).Split( ',');
										int width = int.Parse( parts[ 0]);
										int height = int.Parse( parts[ 1]);
										importer.GetSourceTextureWidthAndHeight( out int textureWidth, out int textureHeight);
										sprite.rect = new Rect( sprite.rect.x, textureHeight - height - sprite.rect.y, width, height);
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
										sprites.Add( sprite);
										sprite = null;
									}
									else
									{
										throw new Exception( $"Invalid property. {line}");
									}
								}
								else
								{
									var factory = new SpriteDataProviderFactories();
									factory.Init();
									var dataProvider = factory.GetSpriteEditorDataProviderFromObject( importer);
									dataProvider.InitSpriteEditorDataProvider();
									var spriteRects = dataProvider.GetSpriteRects();
									
									dataProvider.SetSpriteRects( spriteRects);
								
									sprite = new SpriteRect
									{
										name = line
									};
								}
							}
							else
							{
								importer.SaveAndReimport();
								
								if( sprites.Count > 0)
								{
									var serializedObject = new SerializedObject( importer);
									var spriteProperty = serializedObject.FindProperty( "m_SpriteSheet.m_Sprites");
									spriteProperty.arraySize = sprites.Count;
									
									for( int i0 = 0; i0 < sprites.Count; ++i0)
									{
										var element = spriteProperty.GetArrayElementAtIndex( i0);

										element.FindPropertyRelative( "m_Name").stringValue = sprites[ i0].name;
										element.FindPropertyRelative( "m_Rect.x").floatValue = sprites[ i0].rect.x;
										element.FindPropertyRelative( "m_Rect.y").floatValue = sprites[ i0].rect.y;
										element.FindPropertyRelative( "m_Rect.width").floatValue = sprites[ i0].rect.width;
										element.FindPropertyRelative( "m_Rect.height").floatValue = sprites[ i0].rect.height;
										element.FindPropertyRelative( "m_Alignment").intValue = (int)SpriteAlignment.Center;
										element.FindPropertyRelative( "m_Pivot.x").floatValue = 0.5f;
										element.FindPropertyRelative( "m_Pivot.y").floatValue = 0.5f;
									}
									serializedObject.ApplyModifiedProperties();
									AssetDatabase.ImportAsset( pngAssetPath, ImportAssetOptions.ForceUpdate);
								}
								importer = null;
								return; // once
							}
						}
					}
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