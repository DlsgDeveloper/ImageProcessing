using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestApp
{
	public delegate void ProgressChangedHnd(double progress);

	class Misc
	{

		#region TestRunDir()
		internal static DirectoryInfo TestRunDir
		{
			get
			{
				if (Environment.MachineName == "JIRKA-S-DESKTOP")
					return new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun");
				else
					return new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun");
			}
		}
		#endregion


		#region LogProgressChanged()
		internal static void LogProgressChanged(float progress)
		{
			Console.WriteLine(string.Format("{0}, {1:00.00}%", DateTime.Now.ToString("HH:mm:ss,ff"), progress * 100.0));
		}
		#endregion

	}

}
