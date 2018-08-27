﻿using System;
using System.IO;
using System.Numerics;
using SkiaSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Script.Graphs3D;

namespace Waher.Script.Test
{
	[TestClass]
	public class Canvas3DTests
	{
		private void Save(Canvas3D Canvas, string FileName)
		{
			if (!Directory.Exists("Canvas3D"))
				Directory.CreateDirectory("Canvas3D");

			using (SKImage Image = Canvas.GetBitmap())
			{
				using (SKData Data = Image.Encode(SKEncodedImageFormat.Png, 100))
				{
					File.WriteAllBytes(Path.Combine("Canvas3D", FileName), Data.ToArray());
				}
			}
		}

		[TestMethod]
		public void Canvas3D_Test_01_Plot()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 1, SKColors.White);
			int t;

			for (t = 0; t < 1000000; t++)
			{
				double x = t * Math.Sin(t / 10000.0) / 5000.0;
				double y = t * Math.Cos(t / 20000.0) / 5000.0;
				double z = t / 10000.0;
				Vector4 P = new Vector4((float)x, (float)y, (float)z, 1);
				Canvas.Plot(P, SKColors.Red);
			}

			this.Save(Canvas, "01.png");
		}

		[TestMethod]
		public void Canvas3D_Test_02_Line()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 1, SKColors.White);
			this.DrawCurve(Canvas);
			this.Save(Canvas, "02.png");
		}

		private void DrawCurve(Canvas3D Canvas)
		{
			int t;

			for (t = 0; t < 10000; t++)
			{
				double x = t * Math.Sin(t / 100.0) / 50.0;
				double y = t * Math.Cos(t / 200.0) / 50.0;
				double z = t / 100.0;
				Vector4 P = new Vector4((float)x, (float)y, (float)z, 1);

				if (t == 0)
					Canvas.MoveTo(P);
				else
					Canvas.LineTo(P, SKColors.Red);
			}
		}

		[TestMethod]
		public void Canvas3D_Test_03_Oversampling()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			this.DrawCurve(Canvas);
			this.Save(Canvas, "03.png");
		}

		[TestMethod]
		public void Canvas3D_Test_04_Perspective()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawWireframeCube(Canvas);
			this.Save(Canvas, "04.png");
		}

		private void DrawWireframeCube(Canvas3D Canvas)
		{
			Vector4 P0 = new Vector4(-200, -200, 100, 1);
			Vector4 P1 = new Vector4(-200, -200, 300, 1);
			Vector4 P2 = new Vector4(200, -200, 300, 1);
			Vector4 P3 = new Vector4(200, -200, 100, 1);
			Vector4 P4 = new Vector4(-200, 200, 100, 1);
			Vector4 P5 = new Vector4(-200, 200, 300, 1);
			Vector4 P6 = new Vector4(200, 200, 300, 1);
			Vector4 P7 = new Vector4(200, 200, 100, 1);

			Canvas.PolyLine(new Vector4[] { P0, P1, P2, P3, P0 }, SKColors.Red);
			Canvas.PolyLine(new Vector4[] { P4, P5, P6, P7, P4 }, SKColors.Red);
			Canvas.Line(P0, P4, SKColors.Red);
			Canvas.Line(P1, P5, SKColors.Red);
			Canvas.Line(P2, P6, SKColors.Red);
			Canvas.Line(P3, P7, SKColors.Red);
		}

		[TestMethod]
		public void Canvas3D_Test_05_Polygon()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawCube(Canvas);
			this.Save(Canvas, "05.png");
		}

		private void DrawCube(Canvas3D Canvas)
		{
			Vector4 P0 = new Vector4(-200, -200, 100, 1);
			Vector4 P1 = new Vector4(-200, -200, 300, 1);
			Vector4 P2 = new Vector4(200, -200, 300, 1);
			Vector4 P3 = new Vector4(200, -200, 100, 1);
			Vector4 P4 = new Vector4(-200, 200, 100, 1);
			Vector4 P5 = new Vector4(-200, 200, 300, 1);
			Vector4 P6 = new Vector4(200, 200, 300, 1);
			Vector4 P7 = new Vector4(200, 200, 100, 1);

			Canvas.Polygon(new Vector4[] { P0, P1, P2, P3 }, new SKColor(255, 0, 0, 128));
			Canvas.Polygon(new Vector4[] { P4, P5, P6, P7 }, new SKColor(255, 0, 0, 128));
			Canvas.Polygon(new Vector4[] { P1, P2, P6, P5 }, new SKColor(0, 255, 0, 128));
			Canvas.Polygon(new Vector4[] { P0, P1, P5, P4 }, new SKColor(0, 0, 255, 128));
			Canvas.Polygon(new Vector4[] { P2, P3, P7, P6 }, new SKColor(0, 0, 255, 128));
			Canvas.Polygon(new Vector4[] { P0, P3, P7, P4 }, new SKColor(0, 255, 0, 128));
		}

		[TestMethod]
		public void Canvas3D_Test_06_ZBuffer()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawPlanes(Canvas);
			this.Save(Canvas, "06.png");
		}

		private void DrawPlanes(Canvas3D Canvas)
		{
			Canvas.Polygon(new Vector4[]
			{
				new Vector4(-200, 100, 100, 1),
				new Vector4(-200, 100, 300, 1),
				new Vector4(200, 100, 300, 1),
				new Vector4(200, 100, 100, 1)
			}, SKColors.Red);

			Canvas.Polygon(new Vector4[]
			{
				new Vector4(100, -200, 100, 1),
				new Vector4(100, -200, 300, 1),
				new Vector4(100, 200, 300, 1),
				new Vector4(100, 200, 100, 1)
			}, SKColors.Green);

			Canvas.Polygon(new Vector4[]
			{
				new Vector4(-200, -200, 200, 1),
				new Vector4(200, -200, 200, 1),
				new Vector4(200, 200, 200, 1),
				new Vector4(-200, 200, 200, 1),
			}, new SKColor(0, 0, 255, 64));
		}

		[TestMethod]
		public void Canvas3D_Test_07_Text()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawPlanes(Canvas);
			//Canvas.Text("d", new Vector4(-100, -100, 150, 1), "Tahoma", 100, SKColors.BlueViolet);
			Canvas.Text("Hello World!", new Vector4(-200, -100, 150, 1), "Tahoma", 100, SKColors.BlueViolet);

			this.Save(Canvas, "07.png");
		}

		[TestMethod]
		public void Canvas3D_Test_08_PhongShading_NoOversampling()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 1, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawThreePlanes(Canvas);
			this.Save(Canvas, "08.png");
		}

		[TestMethod]
		public void Canvas3D_Test_09_PhongShading_Oversampling_2()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 2, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawThreePlanes(Canvas);
			this.Save(Canvas, "09.png");
		}

		[TestMethod]
		public void Canvas3D_Test_10_PhongShading_Oversampling_3()
		{
			Canvas3D Canvas = new Canvas3D(640, 480, 3, SKColors.White);
			Canvas.ProjectZ(500);
			this.DrawThreePlanes(Canvas);
			this.Save(Canvas, "10.png");
		}

		private void DrawThreePlanes(Canvas3D Canvas)
		{
			PhongShader Shader = new PhongShader(
				new PhongMaterial(1, 2, 0, 10),
				new PhongIntensity(64, 64, 64, 255),
				new PhongLightSource(
					new PhongIntensity(255, 0, 0, 255),
					new PhongIntensity(255, 255, 255, 255),
					new Vector3(200, 200, 100)));
					//new Vector3(400, 400, 50)));

			Canvas.Polygon(new Vector4[]
			{
				new Vector4(-200, 200, 300, 1),
				new Vector4(200, 200, 300, 1),
				new Vector4(200, -200, 300, 1),
				new Vector4(-200, -200, 300, 1)
			}, Shader);
			
			Canvas.Polygon(new Vector4[]
			{
				new Vector4(-200, 200, 100, 1),
				new Vector4(-200, 200, 300, 1),
				new Vector4(-200, -200, 300, 1),
				new Vector4(-200, -200, 100, 1)
			}, Shader);

			Canvas.Polygon(new Vector4[]
			{
				new Vector4(-200, -200, 300, 1),
				new Vector4(200, -200, 300, 1),
				new Vector4(200, -200, 100, 1),
				new Vector4(-200, -200, 100, 1)
			}, Shader);
		}

		// TODO: Clip
		// TODO: Light / Phong shading with multiple light sources
		// TODO: Proper interpolation of z
		// TODO: Light arithmetic in un-projected coordinates.
		// TODO: Fix glitches in polygon rendering. Example: letter "d", no oversampling. (See test 7)
		//			Probably due to roundoff errors at last pixel when drawing edges
		// TODO: Culling
	}
}