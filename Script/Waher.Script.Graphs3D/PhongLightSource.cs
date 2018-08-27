﻿using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;

namespace Waher.Script.Graphs3D
{
	/// <summary>
	/// Contains information about a light source, as used in the Phong reflection model.
	/// https://en.wikipedia.org/wiki/Phong_reflection_model
	/// </summary>
	public class PhongLightSource
	{
		private PhongIntensity diffuse;
		private PhongIntensity specular;
		private Vector3 position;
		private Vector3 transformedPosition;

		/// <summary>
		/// Contains information about a light source, as used in the Phong reflection model.
		/// https://en.wikipedia.org/wiki/Phong_reflection_model
		/// </summary>
		/// <param name="Diffuse">Diffuse intensity.</param>
		/// <param name="Specular">Specular intensity.</param>
		/// <param name="Position">Position of light source.</param>
		public PhongLightSource(PhongIntensity Diffuse, PhongIntensity Specular, Vector3 Position)
		{
			this.diffuse = Diffuse;
			this.specular = Specular;
			this.position = Position;
			this.transformedPosition = Position;
		}

		/// <summary>
		/// Diffuse intensity.
		/// </summary>
		public PhongIntensity Diffuse => this.diffuse;

		/// <summary>
		/// Specular intensity.
		/// </summary>
		public PhongIntensity Specular => this.specular;

		/// <summary>
		/// Position of light source..
		/// </summary>
		public Vector3 Position => this.position;

		/// <summary>
		/// Transformed position of light source..
		/// </summary>
		public Vector3 TransformedPosition => this.transformedPosition;

		/// <summary>
		/// Transforms any coordinates according to current settings in <paramref name="Canvas"/>.
		/// </summary>
		/// <param name="Canvas">3D Canvas</param>
		public void Transform(Canvas3D Canvas)
		{
			this.transformedPosition = Canvas.Transform(this.position);
		}
	}
}
