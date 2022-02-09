using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
	class TestBase
	{

		// PRIVATE METHODS
		#region private methods

		#region ProgressChanged()
		protected static void ProgressChanged(float progress)
		{
			Console.WriteLine(string.Format("{0}, {1:00.00}%", DateTime.Now.ToString("HH:mm:ss,ff"), progress * 100.0));
		}
		#endregion

		#endregion
	}
}
