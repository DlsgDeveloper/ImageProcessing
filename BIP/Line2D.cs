using System;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Line2D.
	/// </summary>
	public class Line2D
	{
		double	a, b, c;


		#region constructor
		public Line2D()
		{
			a = 0.0;// Nulov�n�
			b = 0.0;
			c = 0.0;
		}

		//Kop�rovac� konstruktor...
		public Line2D(Line2D line)// Kop�rovac� konstruktor
		{
			a = line.a;
			b = line.b;
			c = line.c;
		}

		//Abychom mohli inicializovat tr�du u� pri jej�m vytvoren�, pret��me konstruktor. Kdykoli v programu ho mu�e nahradit metoda Create().
		public Line2D(double a, double b, double c)// Pr�m� zad�n� promenn�ch
		{
			Create(a, b, c);
		}

		//Ctvrt� konstruktor um� vytvorit pr�mku ze dvou bodu, opet ho mu�e nahradit funkce Create(). Jak jsem 
		//naznacil v��e, promenn� a, b predstavuj� norm�lov� vektor pr�mky. Smerov� vektor by se z�skal jednoduch�m 
		//odecten�m koncov�ho bodu od poc�tecn�ho. Vytvoren� norm�lov�ho vektoru je podobn�, ale nav�c prohod�me 
		//slo�ky vektoru a u jedn� invertujeme znam�nko.
		//Radeji pr�klad. M�me dva body [1; 2] a [4; 3], smerov� vektor se z�sk� odecten�m koncov�ho bodu od 
		//poc�tecn�ho, nicm�ne pri vytv�ren� pr�mky je �plne jedno, kter� pova�ujeme za poc�tecn� a kter� za 
		//koncov�. Prvn� bod bude napr�klad poc�tecn� a druh� koncov�. Smerov� vektor je tedy s = (4-1, 3-2) = (3; 1). 
		//Norm�lov� vektor m� prohozen� porad� slo�ek a u jedn� opacn� znam�nko. n = (-1; 3) nebo (1; -3).
		//Pro �plnost: je naprosto jedno, zda vezmeme pr�mo vypocten� vektor nebo jeho k-n�sobek. Oba vektory 
		//uveden� v minul�m odstavci jsou k-n�sobkem toho druh�ho (k = -1). Stejne tak bychom mohli vykr�tit 
		//vektor (5; 10) na (1; 2). Z toho plyne, �e jedna pr�mka mu�e b�t k-n�sobkem druh� - viz. d�le.
		public Line2D(double x1, double y1, double x2, double y2)// Pr�mka ze dvou bodu
		{
			Create(x1, y1, x2, y2);
		}

		public Line2D(PointF p1, PointF p2)// Pr�mka ze dvou bodu
		{
			Create(p1.X, p1.Y, p2.X, p2.Y);
		}
		#endregion

		#region Create()
		public void Create(double a, double b, double c)// Pr�m� zad�n� promenn�ch
		{
			this.a = a;
			this.b = b;
			this.c = c;
		}

		public void Create(double x1, double y1, double x2, double y2)
		{
			if(x1 == x2 && y1 == y2)// 2 stejn� body netvor� pr�mku
			{
				Create(0.0, 0.0, 0.0);// Platn� hodnoty
				return;
			}

			a = y2 - y1;
			b = x1 - x2;

			//Promennou c vypocteme dosazen�m jednoho bodu (v na�em pr�pade prvn�ho) do zat�m ne�pln� rovnice. V z�kladn� 
			// rovnici a*x + b*y + c = 0 presuneme v�echno krome c na pravou stranu a z�sk�me c = -a*x -b*y.
			c = -a*x1 -b*y1;
		}
		#endregion

		#region IsPointOnLine()
		/// <summary>
		/// Zda bod le�� na pr�mce, zjist�me dosazen�m jeho souradnic do rovnice pr�mky. Pokud se v�sledek rovn� nule, le�� na n�.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsPointOnLine(double x, double y)// Le�� bod na pr�mce?
		{
			return (a*x + b*y + c == 0.0);	// Dosazen� souradnic do rovnice
		}
		#endregion

		#region operator ==
		/// <summary>
		/// Jestli jsou pr�mky stejn� (spl�vaj�c�) se zjist� porovn�n�m jejich slo�ek, ale nav�c mus�me vz�t v �vahu i k-n�sobky. 
		/// Nebudeme tedy porovn�vat pr�mo vnitrn� promenn�, ale m�sto toho vypocteme pomery a/a, b/b a c/c. Budou-li tyto pomery 
		/// vnitrn�ch promenn�ch stejn�, je jasn�, �e se jedn� se o jednu a tu samou pr�mku.
		/// </summary>
		/// <param name="primka1"></param>
		/// <param name="primka2"></param>
		/// <returns></returns>
		public static bool operator ==(Line2D primka1, Line2D primka2)// Jsou pr�mky spl�vaj�c�?
		{
			if ((primka1.a != 0 && primka2.a == 0) || (primka1.a == 0 && primka2.a != 0))
				return false;
			if ((primka1.b != 0 && primka2.b == 0) || (primka1.b == 0 && primka2.b != 0))
				return false;
			if ((primka1.c != 0 && primka2.c == 0) || (primka1.c == 0 && primka2.c != 0))
				return false;

			// Nestac� pouze zkontrolovat hodnoty, primka mu�e b�t k-n�sobkem
			double ka = (primka2.a != 0) ? primka1.a / primka2.a : 0;
			double kb = (primka2.b != 0) ? primka1.b / primka2.b : 0;
			double kc = (primka2.c != 0) ? primka1.c / primka2.c : 0;

			if (ka == 0)
			{
				if (kb == 0 || kc == 0)
					return true;
				else
					return (kb == kc);
			}
			if (kb == 0)
			{
				return true;
			}
			if (kc == 0)
			{
				return (ka == kb);
			}

			if (ka == kb && ka == kc)// Mus� b�t stejn�
			{
				return true;// Spl�vaj�c� pr�mky
			}
			else
			{
				return false;// Dve ruzn� pr�mky
			}
		}
		#endregion

		#region operator!=
		public static bool operator!=(Line2D primka1, Line2D primka2)// Nejsou pr�mky spl�vaj�c�?
		{
			return !(primka1 == primka2);// Negace porovn�n�
		}
		#endregion

		#region AreParallelLines()
		//Zji�ten�, jestli jsou pr�mky rovnobe�n�, je velmi podobn� oper�toru porovn�n�. Maj�-li stejn� 
		//norm�lov� vektor, popr. vektor jedn� je k-n�sobkem druh�, jsou rovnobe�n�. Tret� promennou, c, 
		//nemus�me a vlastne ani nesm�me testovat.
		public bool AreParallelLines(Line2D primka)// Jsou pr�mky rovnobe�n�?
		{
			if (primka.a == 0)
				return (a == 0);
			if (primka.b == 0)
				return (b == 0);
			
			// Nestac� zkontrolovat hodnoty, p mu�e b�t k-n�sobkem
			double ka = a / primka.a;
			double kb = b / primka.b;

			return (ka == kb);// Mus� b�t stejn�
		}
		#endregion

		#region ArePerpendicularLines()
		//Kolmost dvou pr�mek se nejjednodu�eji odhal� tak, �e se jedna z nich natoc� o 90 stupnu a otestuje 
		//se jejich rovnobe�nost - proc to zbytecne komplikovat...
		public bool ArePerpendicularLines(Line2D primka)// Jsou pr�mky kolm�?
		{
			Line2D pom = new Line2D(-primka.b, primka.a, primka.c);// Pr�mka s kolm�m vektorem

			return AreParallelLines(pom);
		}
		#endregion

		#region InterceptPoint()
		//Dost�v�me se k podstate cel�ho cl�nku - prusec�k dvou pr�mek. Nejdr�ve otestujeme jestli se 
		//nejedn� o dve spl�vaj�c� pr�mky, pokud ano, maj� nekonecne mnoho spolecn�ch bodu. Nejsou-li 
		//spl�vaj�c�, mohou b�t je�te rovnobe�n�, pak nemaj� ��dn� spolecn� bod. Ve v�ech ostatn�ch pr�padech 
		//maj� pouze jeden spolecn� bod a t�m je prusec�k. Proto�e mus� vyhovovat soucasne obema rovnic�m, 
		//re��me soustavu dvou rovnic o dvou nezn�m�ch x a y.

		//Pokud funkce vr�t� true, byl prusec�k nalezen, souradnice ulo��me do referenc� retx a rety. False 
		//indikuje bud ��dn� prusec�k (rovnobe�n� pr�mky), nebo nekonecne mnoho spolecn�ch bodu (spl�vaj�c� pr�mky).
		public bool InterceptPoint(Line2D primka, ref double retx, ref double rety)// Prusec�k pr�mek
		{
			if(this == primka)// Pr�mky jsou spl�vaj�c� - nekonecne mnoho spolecn�ch bodu
			{
				return false;// Sp�e by se melo vr�tit true a nejak� bod... z�le�� na pou�it�
			}
			else if(AreParallelLines(primka))// Pr�mky jsou rovnobe�n� - ��dn� spolecn� bod
			{
				return false;
			}
			else// Jeden spolecn� bod - prusec�k (vyhovuje soucasne obema rovnic�m)
			{
				double sum = ((a * primka.b - primka.a * b) != 0) ? (a * primka.b - primka.a * b) : 1;
				
				retx = (b*primka.c - c * primka.b) / sum;
				rety = -(a*primka.c - primka.a * c) / sum;

				return true;
			}
		}

		/// <summary>
		/// Returns intercept point, that is on line and closest to the point. Returns false if p is on line.
		/// </summary>
		/// <param name="pX"></param>
		/// <param name="pY"></param>
		/// <param name="retx"></param>
		/// <param name="rety"></param>
		/// <returns></returns>
		public static bool InterceptPoint(double x1, double y1, double x2, double y2, double pX, double pY, ref double retx, ref double rety)// Prusec�k pr�mek
		{
			if (new Line2D(x1, y1, x2, y2).DistancePointToLine(pX, pY) == 0)
			{
				return false;
			}
			else
			{
				double k = ((y2 - y1) * (pX - x1) - (x2 - x1) * (pY - y1)) / ((y2 - y1) * (y2 - y1) + (x2 - x1) * (x2 - x1));
				retx = pX - k * (y2 - y1);
				rety = pY + k * (x2 - x1);

				return true;
			}
		}
		#endregion

		#region Angle()
		//�hel dvou pr�mek je �hlem dvou smerov�ch vektoru, mu�eme v�ak pou��t i norm�lov� vektory, proto�e 
		//v�sledek bude stejn�. Kosinus �hlu se rovn� zlomku, u kter�ho se v citateli nach�z� skal�rn� 
		//soucin vektoru (n�sob� se zvl�t x a zvl�t y slo�ky) a ve jmenovateli soucin d�lek vektoru (Pythagorova 
		//veta). Pokud nech�pete, berte to jako vzorec.
		public double Angle(Line2D primka)// �hel pr�mek
		{
			return Math.Acos((a*primka.a + b*primka.b) / (Math.Sqrt(a*a + b*b) * Math.Sqrt(primka.a*primka.a + primka.b*primka.b)));
		}
		#endregion

		#region DistancePointToLine()
		//Vzd�lenost bodu od pr�mky je u� trochu slo�itej��. Vypocte se rovnice pr�mky, kter� je kolm� k zadan� 
		//pr�mce a proch�z� urcen�m bodem. Potom se najde prusec�k techto pr�mek a vypocte se vzd�lenost bodu. 
		//Cel� tento postup se ale d� mnohon�sobne zjednodu�it, kdy� si najdete vzorec v matematicko fyzik�ln�ch tabulk�ch :-)
		public double DistancePointToLine(double x, double y)// Vzd�lenost bodu od pr�mky
		{
			double vzdalenost = (a*x + b*y + c) / Math.Sqrt(a*a + b*b);

			if(vzdalenost < 0.0)// Absolutn� hodnota
			{
				vzdalenost = -vzdalenost;
			}

			return vzdalenost;
		}
		#endregion

		#region GetHashCode()
		public override int GetHashCode()
		{
			return 0;
		}
		#endregion

		#region Equals()
		public override bool Equals(object  primka)
		{
			if(primka is Line2D)	
				return (this == (Line2D) primka);

			return false;
		}
		#endregion

		#region GetPerpendicularLine()
		public static Line2D GetPerpendicularLine(PointF point, double angle)
		{
			Line2D perpendicularLine = new Line2D();

			if (angle < .00001 && angle > -.00001)
			{
				perpendicularLine.Create(point.X, point.Y, point.X, point.Y + 10000);
			}
			else
			{
				double point2Y = point.Y + (10000 * Math.Tan(angle + Math.PI / 2));

				perpendicularLine.Create(point.X, point.Y, point.X + 10000, point2Y);
			}

			return perpendicularLine;
		}
		#endregion

		#region GetPerpendicularLine()
		/// <summary>
		/// Returns line perpendicular to current line going thru 'point'. 
		/// https://www.varsitytutors.com/act_math-help/how-to-find-the-equation-of-a-perpendicular-line
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public Line2D GetPerpendicularLine(PointF point)
		{
			return GetPerpendicularLine(point.X, point.Y);
		}

		/// <summary>
		/// Returns line perpendicular to current line going thru point [x, y]. 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public Line2D GetPerpendicularLine(double x, double y)
		{
			double aNew, bNew, cNew;

			if (a != 0 && b != 0)
			{
				aNew = b / a;
				bNew = -1;
				cNew = y - (b / a) * x;
			}
			else if (a != 0)
			{
				aNew = 0;
				bNew = 1;
				cNew = -y;
			}
			else
			{
				aNew = 1;
				bNew = 0;
				cNew = -x;
			}

			return new Line2D(aNew, bNew, cNew);
		}
		#endregion

		#region GetX()
		/// <summary>
		/// ax + by + c = 0
		/// x = (-by - c) / a
		/// </summary>
		/// <param name="y"></param>
		/// <returns></returns>
		public double GetX(double y)
		{
			if (a != 0)
				return (-b * y - c) / a;
			else
				return double.NaN;
		}
		#endregion

		#region GetY()
		/// <summary>
		/// ax + by + c = 0
		/// y = (-ax - c) / b
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public double GetY(double x)
		{
			if (b != 0)
				return (-a * x - c) / b;
			else
				return double.NaN;
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			string s = "Line: ";

			if (a != 0)
				s += a.ToString("+#;-#") + "x ";
			if (b != 0)
				s += b.ToString("+#;-#") + "y ";
			if (c != 0)
				s += c.ToString("+#;-#") + " ";

			return s + " = 0";
		}
		#endregion

	}
}
