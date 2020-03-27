using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessingApp
{
	public delegate void ProgressChangedHnd(double progress);

	class Misc
	{

		#region LogProgressChanged()
		internal static void LogProgressChanged(float progress)
		{
			Console.WriteLine(string.Format("{0}, {1:00.00}%", DateTime.Now.ToString("HH:mm:ss,ff"), progress * 100.0));
		}
		#endregion

	}

}
