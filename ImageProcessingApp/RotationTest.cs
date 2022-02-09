using ImageProcessing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class RotationTest : TestBase
	{

		#region Rotate()
		public static void Rotate()
		{
			string source32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\RotateClip\01.png";
			string result32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\RotateClip\result32bpp.png";

			using (Bitmap b = new Bitmap(source32bpp))
			{
				/*
				DateTime start = DateTime.Now;
				using (Bitmap result = ImageProcessing.Rotation.Rotate32bpp(b, Math.PI / 4))
				{
					Console.WriteLine("Rotation: " + DateTime.Now.Subtract(start).ToString());

					result.Save(result32bpp, ImageFormat.Png);
				}
				*/
				DateTime start = DateTime.Now;
				using (Bitmap result = ImageProcessing.Rotation.RotateClip(b, Math.PI / 4, new Rectangle(100, 100, 100, 100), 0, 0, 0))
				{
					Console.WriteLine("Rotation: " + DateTime.Now.Subtract(start).ToString());

					result.Save(result32bpp, ImageFormat.Png);
				}
			}
		}
		#endregion

		#region GetClip()
		public static void GetClip()
		{
			string sourcePath = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\GetClip\01.jpg";
			string resultPath = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\GetClip\result.png";

			using (Bitmap b = new Bitmap(sourcePath))
			{
				DateTime start = DateTime.Now;

				using (Bitmap result = ImageProcessing.Rotation.GetClip(b, new Point(2117, 542), new Point(2752, 703), new Point(1979, 1063)))
				{
					Console.WriteLine("GetClip: " + DateTime.Now.Subtract(start).ToString());

					result.Save(resultPath, ImageFormat.Png);
				}
			}
		}
		#endregion

		#region Rotation()
		private unsafe static void Rotation()
		{
			try
			{
				FileInfo sourceFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\Rotation\Rotation.png");
				string dest = @"C:\Users\jirka.stybnar\TestRun\Rotation\result1.png";
				DateTime start = DateTime.Now;

				Bitmap source = new Bitmap(sourceFile.FullName);
				Bitmap r1 = ImageProcessing.Rotation.Rotate(source, -4 * Math.PI / 180, 0, 0, 0, new Point(source.Width / 2, source.Height / 2));

				Console.WriteLine(string.Format("{0}: {1}", sourceFile.FullName, DateTime.Now.Subtract(start).ToString()));

				r1.Save(dest, ImageFormat.Png);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
		#endregion

		#region RotationBigImage()
		private unsafe static void RotationBigImage()
		{
			try
			{
				//string filePath = @"C:\Opus4\WorkingData\ActiveObjectHive\00060\00\000\004\ScanImages\Full\00000004_000012.jpg";
				string filePath = @"C:\temp\01.png";
				string dest = @"C:\temp\result.png";
				DateTime start = DateTime.Now;

				using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(filePath))
				{
					ImageProcessing.BigImages.Rotation rotation = new ImageProcessing.BigImages.Rotation();
					double ratio = itDecoder.Width / (double)itDecoder.Height;

					rotation.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

					ImageProcessing.IpSettings.Clip clip = new ImageProcessing.IpSettings.Clip(0.26923076907230242, 0.055093142906446763,
						0.56398440301101371, 0.84244946517707876, ratio);
					clip.SetSkew(-0.4522318329721377, 1.0F);
					//clip.SetSkew(0.45, 1.0F);


					rotation.RotateClip(itDecoder, dest, new ImageProcessing.FileFormat.Png(), clip, 0xff, 0xff, 0xff);
				}

				Console.WriteLine(DateTime.Now.Subtract(start).ToString());
				//Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region RotationFlipping()
		private unsafe static void RotationFlipping()
		{
			try
			{
				string sourceR = @"C:\Users\jirka.stybnar\TestRun\ColorModes\24bpp.png";
				string sourceG = @"C:\Users\jirka.stybnar\TestRun\ColorModes\8bpp gray.png";
				string sourceB = @"C:\Users\jirka.stybnar\TestRun\ColorModes\1bpp.png";
				string dir1 = @"C:\delete\01\";
				string dir2 = @"C:\delete\02\";
				DateTime start;

				Directory.CreateDirectory(dir1);
				Directory.CreateDirectory(dir2);

				for (int i = 0; i < 8; i++)
				//for (int i = 1; i < 2; i++)
				{
					System.Drawing.RotateFlipType rotateFlipType = (System.Drawing.RotateFlipType)i;

					//color
					using (Bitmap bitmap = new Bitmap(sourceR))
					{
						start = DateTime.Now;
						Bitmap result = ImageProcessing.RotationFlipping.Go(bitmap, rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
						result.Save(dir1 + i.ToString() + " Color.png", ImageFormat.Png);

						start = DateTime.Now;
						bitmap.RotateFlip(rotateFlipType);
						Console.WriteLine("Windows: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
						bitmap.Save(dir2 + i.ToString() + " Color.png", ImageFormat.Png);
					}

					//gray
					using (Bitmap bitmap = new Bitmap(sourceG))
					{
						start = DateTime.Now;
						Bitmap result = ImageProcessing.RotationFlipping.Go(bitmap, rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
						result.Save(dir1 + i.ToString() + " Gray.png", ImageFormat.Png);

						start = DateTime.Now;
						bitmap.RotateFlip(rotateFlipType);
						Console.WriteLine("Windows: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
						bitmap.Save(dir2 + i.ToString() + " Gray.png", ImageFormat.Png);
					}

					//black/white
					using (Bitmap bitmap = new Bitmap(sourceB))
					{
						start = DateTime.Now;
						Bitmap result = ImageProcessing.RotationFlipping.Go(bitmap, rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
						result.Save(dir1 + i.ToString() + " BW.png", ImageFormat.Png);

						start = DateTime.Now;
						bitmap.RotateFlip(rotateFlipType);
						Console.WriteLine("Windows: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
						bitmap.Save(dir2 + i.ToString() + " BW.png", ImageFormat.Png);
						bitmap.Dispose();
					}
				}

				Console.WriteLine("\n\nDone.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region RotationFlippingBigImage()
		private unsafe static void RotationFlippingBigImage()
		{
			try
			{
				string sourceR = @"C:\Users\jirka.stybnar\TestRun\ColorModes\24bpp.png";
				string sourceG = @"C:\Users\jirka.stybnar\TestRun\ColorModes\8bpp gray.png";
				string sourceB = @"C:\Users\jirka.stybnar\TestRun\ColorModes\1bpp.png";
				string dir1 = @"C:\delete\01\";
				string dir2 = @"C:\delete\02\";
				DateTime start;
				Bitmap bitmap;

				Directory.CreateDirectory(dir1);
				Directory.CreateDirectory(dir2);

				ImageProcessing.BigImages.RotationFlipping flipping = new ImageProcessing.BigImages.RotationFlipping();
				flipping.ProgressChanged += new ProgressHnd(ProgressChanged);

				for (int i = 0; i < 8; i++)
				//for (int i = 1; i < 2; i++)
				{
					System.Drawing.RotateFlipType rotateFlipType = (System.Drawing.RotateFlipType)i;

					//color
					using (ImageProcessing.BigImages.ItDecoder decoder = new ImageProcessing.BigImages.ItDecoder(sourceR))
					{
						start = DateTime.Now;
						flipping.Go(decoder, dir1 + i.ToString() + " Color.png", new ImageProcessing.FileFormat.Png(), rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
					}

					bitmap = new Bitmap(sourceR);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " Color.png", ImageFormat.Png);
					bitmap.Dispose();

					//gray
					using (ImageProcessing.BigImages.ItDecoder decoder = new ImageProcessing.BigImages.ItDecoder(sourceG))
					{
						start = DateTime.Now;
						flipping.Go(decoder, dir1 + i.ToString() + " Gray.png", new ImageProcessing.FileFormat.Png(), rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
					}

					bitmap = new Bitmap(sourceG);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " Gray.png", ImageFormat.Png);
					bitmap.Dispose();

					//black/white
					using (ImageProcessing.BigImages.ItDecoder decoder = new ImageProcessing.BigImages.ItDecoder(sourceB))
					{
						start = DateTime.Now;
						flipping.Go(decoder, dir1 + i.ToString() + " BW.png", new ImageProcessing.FileFormat.Png(), rotateFlipType);
						Console.WriteLine("My: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
					}

					bitmap = new Bitmap(sourceB);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " BW.png", ImageFormat.Png);
					bitmap.Dispose();
				}

				Console.WriteLine("\n\nDone.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}

		/*private unsafe static void RotationFlippingBigImage()
		{
			try
			{
				string sourceR = @"C:\delete\01.jpg";
				string sourceG = @"C:\delete\02.png";
				string sourceB = @"C:\delete\03.png";
				string dir1 = @"C:\delete\01\";
				string dir2 = @"C:\delete\02\";
				DateTime start;
				Bitmap bitmap;

				for (int i = 0; i < 8; i++)
				//for (int i = 2; i < 3; i++)
				{
					System.Drawing.RotateFlipType rotateFlipType = (System.Drawing.RotateFlipType)i;
							
					//color
					bitmap = new Bitmap(sourceR);
					start = DateTime.Now; 
					ImageProcessing.BigImages.RotationFlipping.Go(ref bitmap, rotateFlipType);
					Console.WriteLine("My: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir1 + i.ToString() + " Color.png", ImageFormat.Png);
					bitmap.Dispose();

					bitmap = new Bitmap(sourceR);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " Color: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " Color.png", ImageFormat.Png);
					bitmap.Dispose();
							
					//gray
					bitmap = new Bitmap(sourceG);
					start = DateTime.Now;
					ImageProcessing.BigImages.RotationFlipping.Go(ref bitmap, rotateFlipType);
					Console.WriteLine("My: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir1 + i.ToString() + " Gray.png", ImageFormat.Png);
					bitmap.Dispose();

					bitmap = new Bitmap(sourceG);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " Gray: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " Gray.png", ImageFormat.Png);
					bitmap.Dispose();
							
					//black/white
					bitmap = new Bitmap(sourceB);
					start = DateTime.Now;
					ImageProcessing.BigImages.RotationFlipping.Go(ref bitmap, rotateFlipType);
					Console.WriteLine("My: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir1 + i.ToString() + " BW.png", ImageFormat.Png);
					bitmap.Dispose(); 
							
					bitmap = new Bitmap(sourceB);
					start = DateTime.Now;
					bitmap.RotateFlip(rotateFlipType);
					Console.WriteLine("Windows: " + i.ToString() + " BW: " + DateTime.Now.Subtract(start).ToString());
					bitmap.Save(dir2 + i.ToString() + " BW.png", ImageFormat.Png);
					bitmap.Dispose();
				}

				CompareFolders(dir1, dir2);

				Console.WriteLine("\n\nDone.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}*/
		#endregion

	}
}
