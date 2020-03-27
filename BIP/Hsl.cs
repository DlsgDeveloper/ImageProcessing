using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// http://en.wikipedia.org/wiki/HSV_color_space
	/// </summary>
	public class Hsl
	{
		float hue;
		float saturation;
		float lightness;

		#region constructor
		public Hsl(float hue, float saturation, float lightness)
		{
			this.hue = hue;
			this.saturation = saturation;
			this.lightness = lightness;
		}

		/// <summary>
		/// http://en.wikipedia.org/wiki/HSV_color_space
		/// algorithm to transfer RGB to HSL
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public Hsl(byte rB, byte gB, byte bB)
		{
			float r = rB / 255F, g = gB / 255F, b = bB / 255F;
			float min = (r < g && r < b) ? r : ((g < b) ? g : b);
			float max = (r > g && r > b) ? r : ((g > b) ? g : b);

			if (max == min)
				this.hue = 0;
			else if (max == r)
				this.hue = (60 * (g - b) / (max - min));
			else if (max == g)
				this.hue = (60 * (b - r) / (max - min) + 120);
			else
				this.hue = 60 * (r - g) / (max - min) + 240;

			if (this.hue < 0)
				this.hue += 360;

			this.lightness = 0.5F * (max + min);

			if (max == min)
				this.saturation = 0;
			else if (this.lightness <= 0.5)
				this.saturation = (max - min) / (max + min);
			else
				this.saturation = (max - min) / (2.0F - (max + min));
		}

		public Hsl(Color color)
			: this(color.R, color.G, color.B)
		{
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public float Hue
		{
			get { return this.hue; }
			set { this.hue = (value < 0) ? value + 360 : ((value > 360) ? value - 360 : value); }
		}

		public float Saturation
		{
			get { return this.saturation; }
			set { this.saturation = (value < 0) ? 0 : ((value > 1) ? 1 : value); }
		}

		public float Lightness
		{
			get { return this.lightness; }
			set { this.lightness = (value < 0) ? 0 : ((value > 1) ? 1 : value); }
		}
		#endregion

		//PUBLIC METHODS
		#region public methods


		#region GetColor()
		/// <summary>
		/// http://en.wikipedia.org/wiki/HSV_color_space
		/// Conversion from HSL to RGB
		/// </summary>
		/// <param name="rB"></param>
		/// <param name="gB"></param>
		/// <param name="bB"></param>
		public void GetColor(ref byte rB, ref byte gB, ref byte bB)
		{
			float q;

			if (this.lightness < 0.5)
				q = this.lightness * (1 + this.saturation);
			else
				q = this.lightness + this.saturation - (this.lightness * this.saturation);

			float p = 2 * this.lightness - q;
			float hK = this.hue / 360.0F;
			float tR = hK + 1 / 3.0F;
			float tG = hK;
			float tB = hK - 1 / 3.0F;

			if (tR < 0)
				tR += 1;
			if (tG < 0)
				tG += 1;
			if (tB < 0)
				tB += 1;
			if (tR > 1)
				tR -= 1;
			if (tG > 1)
				tG -= 1;
			if (tB > 1)
				tB -= 1;

			float r, g, b;

			if (tR < 1 / 6F)
				r = p + ((q - p) * 6 * tR);
			else if (tR < 0.5)
				r = q;
			else if (tR < 2 / 3F)
				r = p + ((q - p) * 6 * (2 / 3F - tR));
			else
				r = p;

			if (tG < 1 / 6F)
				g = p + ((q - p) * 6 * tG);
			else if (tG < 0.5)
				g = q;
			else if (tG < 2 / 3F)
				g = p + ((q - p) * 6 * (2 / 3F - tG));
			else
				g = p;

			if (tB < 1 / 6F)
				b = p + ((q - p) * 6 * tB);
			else if (tB < 0.5)
				b = q;
			else if (tB < 2 / 3F)
				b = p + ((q - p) * 6 * (2 / 3F - tB));
			else
				b = p;

			rB = Convert.ToByte(r * 255F);
			gB = Convert.ToByte(g * 255F);
			bB = Convert.ToByte(b * 255F);
			//return Color.FromArgb((byte)r, (byte)g, (byte)b);
		}
		#endregion

		#region GetSaturation()
		public static float GetSaturation(byte rB, byte gB, byte bB)
		{
			float r = rB / 255F, g = gB / 255F, b = bB / 255F;
			float min = (r < g && r < b) ? r : ((g < b) ? g : b);
			float max = (r > g && r > b) ? r : ((g > b) ? g : b);
			float lightness = 0.5F * (max + min);

			if (max == min)
				return 0;
			else if (lightness <= 0.5)
				return (max - min) / (max + min);
			else
				return (max - min) / (2.0F - 2 * lightness);
		}
		#endregion

		#region IncreaseSaturation()
		public static void IncreaseSaturation(ref byte rB, ref byte gB, ref byte bB, float minimum, float ratio)
		{
			//to compute saturation
			float r = rB / 255F, g = gB / 255F, b = bB / 255F;
			float min = (r < g && r < b) ? r : ((g < b) ? g : b);
			float max = (r > g && r > b) ? r : ((g > b) ? g : b);
			float hue, saturation, lightness;

			if (max == min)
				hue = 0;
			else if (max == r)
				hue = (60 * (g - b) / (max - min));
			else if (max == g)
				hue = (60 * (b - r) / (max - min) + 120);
			else
				hue = 60 * (r - g) / (max - min) + 240;

			if (hue < 0)
				hue += 360;

			lightness = 0.5F * (max + min);

			if (max == min)
				saturation = 0;
			else if (lightness <= 0.5)
				saturation = (max - min) / (max + min);
			else
				saturation = (max - min) / (2.0F - (max + min));

			//change saturation
			saturation = ((saturation - minimum) * ratio);
			
			if (saturation < 0)
				saturation = 0;
			else if (saturation > 1)
				saturation = 1;

			//to compute color
			float q;

			if (lightness < 0.5)
				q = lightness * (1 + saturation);
			else
				q = lightness + saturation - (lightness * saturation);

			float p = 2 * lightness - q;
			float hK = hue / 360.0F;
			float tR = hK + 1 / 3.0F;
			float tG = hK;
			float tB = hK - 1 / 3.0F;

			if (tR < 0)
				tR += 1;
			if (tG < 0)
				tG += 1;
			if (tB < 0)
				tB += 1;
			if (tR > 1)
				tR -= 1;
			if (tG > 1)
				tG -= 1;
			if (tB > 1)
				tB -= 1;

			if (tR < 1 / 6F)
				r = p + ((q - p) * 6 * tR);
			else if (tR < 0.5)
				r = q;
			else if (tR < 2 / 3F)
				r = p + ((q - p) * 6 * (2 / 3F - tR));
			else
				r = p;

			if (tG < 1 / 6F)
				g = p + ((q - p) * 6 * tG);
			else if (tG < 0.5)
				g = q;
			else if (tG < 2 / 3F)
				g = p + ((q - p) * 6 * (2 / 3F - tG));
			else
				g = p;

			if (tB < 1 / 6F)
				b = p + ((q - p) * 6 * tB);
			else if (tB < 0.5)
				b = q;
			else if (tB < 2 / 3F)
				b = p + ((q - p) * 6 * (2 / 3F - tB));
			else
				b = p;

			rB = Convert.ToByte(r * 255F);
			gB = Convert.ToByte(g * 255F);
			bB = Convert.ToByte(b * 255F);
		}
		#endregion

		#endregion
	}
}
