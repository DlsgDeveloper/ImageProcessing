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
	public class Hsv
	{
		public float hue;
		public float saturation;
		public float value;

		public Hsv(float hue, float saturation, float value)
		{
			this.hue = hue;
			this.saturation = saturation;
			this.value = value;
		}

		/// <summary>
		/// http://en.wikipedia.org/wiki/HSV_color_space
		/// algorithm to transfer RGB to HSV
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public Hsv(byte rB, byte gB, byte bB)
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

			this.value = max;

			if (max == 0)
				this.saturation = 0;
			else
				this.saturation = 1.0F - (min / max);
		}

		public Hsv(Color color)
			: this(color.R, color.G, color.B)
		{
		}

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

		public float Value
		{
			get { return this.value; }
			set { this.value = (value < 0) ? 0 : ((value > 1) ? 1 : value); }
		}

		/// <summary>
		/// http://en.wikipedia.org/wiki/HSV_color_space
		/// Conversion from HSV to RGB
		/// </summary>
		/// <returns></returns>
		public Color GetColor()
		{
			return GetColor(this.hue, this.saturation, this.value);
		}

		public static float GetSaturation(byte rB, byte gB, byte bB)
		{
			float r = rB / 255F, g = gB / 255F, b = bB / 255F;
			float max = (r > g && r > b) ? r : ((g > b) ? g : b);

			if (max == 0)
				return 0;
			else
			{
				float min = (r < g && r < b) ? r : ((g < b) ? g : b);
				return 1.0F - (min / max);
			}
		}

		public static Color GetColor(float hue, float saturation, float value)
		{
			int hI = (int)(hue / 60.0) % 6;
			float f = (hue / 60.0F) - (int)(hue / 60.0F);
			float p = value * (1 - saturation);
			float q = value * (1 - f * saturation);
			float t = value * (1 - (1 - f) * saturation);

			if (hI == 0)
				return Color.FromArgb(Convert.ToInt32(value * 255F), Convert.ToInt32(t * 255F), Convert.ToInt32(p * 255F));
			else if (hI == 1)
				return Color.FromArgb(Convert.ToInt32(q * 255F), Convert.ToInt32(value * 255F), Convert.ToInt32(p * 255F));
			else if (hI == 2)
				return Color.FromArgb(Convert.ToInt32(p * 255F), Convert.ToInt32(value * 255F), Convert.ToInt32(t * 255F));
			else if (hI == 3)
				return Color.FromArgb(Convert.ToInt32(p * 255F), Convert.ToInt32(q * 255F), Convert.ToInt32(value * 255F));
			else if (hI == 4)
				return Color.FromArgb(Convert.ToInt32(t * 255F), Convert.ToInt32(p * 255F), Convert.ToInt32(value * 255F));
			else
				return Color.FromArgb(Convert.ToInt32(value * 255F), Convert.ToInt32(p * 255F), Convert.ToInt32(q * 255F));
		}

	}
}
