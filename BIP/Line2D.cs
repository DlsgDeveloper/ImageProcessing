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
			a = 0.0;// Nulování
			b = 0.0;
			c = 0.0;
		}

		//Kopírovací konstruktor...
		public Line2D(Line2D line)// Kopírovací konstruktor
		{
			a = line.a;
			b = line.b;
			c = line.c;
		}

		//Abychom mohli inicializovat trídu už pri jejím vytvorení, pretížíme konstruktor. Kdykoli v programu ho muže nahradit metoda Create().
		public Line2D(double a, double b, double c)// Prímé zadání promenných
		{
			Create(a, b, c);
		}

		//Ctvrtý konstruktor umí vytvorit prímku ze dvou bodu, opet ho muže nahradit funkce Create(). Jak jsem 
		//naznacil výše, promenné a, b predstavují normálový vektor prímky. Smerový vektor by se získal jednoduchým 
		//odectením koncového bodu od pocátecního. Vytvorení normálového vektoru je podobné, ale navíc prohodíme 
		//složky vektoru a u jedné invertujeme znaménko.
		//Radeji príklad. Máme dva body [1; 2] a [4; 3], smerový vektor se získá odectením koncového bodu od 
		//pocátecního, nicméne pri vytvárení prímky je úplne jedno, který považujeme za pocátecní a který za 
		//koncový. První bod bude napríklad pocátecní a druhý koncový. Smerový vektor je tedy s = (4-1, 3-2) = (3; 1). 
		//Normálový vektor má prohozené poradí složek a u jedné opacné znaménko. n = (-1; 3) nebo (1; -3).
		//Pro úplnost: je naprosto jedno, zda vezmeme prímo vypoctený vektor nebo jeho k-násobek. Oba vektory 
		//uvedené v minulém odstavci jsou k-násobkem toho druhého (k = -1). Stejne tak bychom mohli vykrátit 
		//vektor (5; 10) na (1; 2). Z toho plyne, že jedna prímka muže být k-násobkem druhé - viz. dále.
		public Line2D(double x1, double y1, double x2, double y2)// Prímka ze dvou bodu
		{
			Create(x1, y1, x2, y2);
		}

		public Line2D(PointF p1, PointF p2)// Prímka ze dvou bodu
		{
			Create(p1.X, p1.Y, p2.X, p2.Y);
		}
		#endregion

		#region Create()
		public void Create(double a, double b, double c)// Prímé zadání promenných
		{
			this.a = a;
			this.b = b;
			this.c = c;
		}

		public void Create(double x1, double y1, double x2, double y2)
		{
			if(x1 == x2 && y1 == y2)// 2 stejné body netvorí prímku
			{
				Create(0.0, 0.0, 0.0);// Platné hodnoty
				return;
			}

			a = y2 - y1;
			b = x1 - x2;

			//Promennou c vypocteme dosazením jednoho bodu (v našem prípade prvního) do zatím neúplné rovnice. V základní 
			// rovnici a*x + b*y + c = 0 presuneme všechno krome c na pravou stranu a získáme c = -a*x -b*y.
			c = -a*x1 -b*y1;
		}
		#endregion

		#region IsPointOnLine()
		/// <summary>
		/// Zda bod leží na prímce, zjistíme dosazením jeho souradnic do rovnice prímky. Pokud se výsledek rovná nule, leží na ní.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsPointOnLine(double x, double y)// Leží bod na prímce?
		{
			return (a*x + b*y + c == 0.0);	// Dosazení souradnic do rovnice
		}
		#endregion

		#region operator ==
		/// <summary>
		/// Jestli jsou prímky stejné (splývající) se zjistí porovnáním jejich složek, ale navíc musíme vzít v úvahu i k-násobky. 
		/// Nebudeme tedy porovnávat prímo vnitrní promenné, ale místo toho vypocteme pomery a/a, b/b a c/c. Budou-li tyto pomery 
		/// vnitrních promenných stejné, je jasné, že se jedná se o jednu a tu samou prímku.
		/// </summary>
		/// <param name="primka1"></param>
		/// <param name="primka2"></param>
		/// <returns></returns>
		public static bool operator ==(Line2D primka1, Line2D primka2)// Jsou prímky splývající?
		{
			if ((primka1.a != 0 && primka2.a == 0) || (primka1.a == 0 && primka2.a != 0))
				return false;
			if ((primka1.b != 0 && primka2.b == 0) || (primka1.b == 0 && primka2.b != 0))
				return false;
			if ((primka1.c != 0 && primka2.c == 0) || (primka1.c == 0 && primka2.c != 0))
				return false;

			// Nestací pouze zkontrolovat hodnoty, primka muže být k-násobkem
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

			if (ka == kb && ka == kc)// Musí být stejné
			{
				return true;// Splývající prímky
			}
			else
			{
				return false;// Dve ruzné prímky
			}
		}
		#endregion

		#region operator!=
		public static bool operator!=(Line2D primka1, Line2D primka2)// Nejsou prímky splývající?
		{
			return !(primka1 == primka2);// Negace porovnání
		}
		#endregion

		#region AreParallelLines()
		//Zjištení, jestli jsou prímky rovnobežné, je velmi podobné operátoru porovnání. Mají-li stejný 
		//normálový vektor, popr. vektor jedné je k-násobkem druhé, jsou rovnobežné. Tretí promennou, c, 
		//nemusíme a vlastne ani nesmíme testovat.
		public bool AreParallelLines(Line2D primka)// Jsou prímky rovnobežné?
		{
			if (primka.a == 0)
				return (a == 0);
			if (primka.b == 0)
				return (b == 0);
			
			// Nestací zkontrolovat hodnoty, p muže být k-násobkem
			double ka = a / primka.a;
			double kb = b / primka.b;

			return (ka == kb);// Musí být stejné
		}
		#endregion

		#region ArePerpendicularLines()
		//Kolmost dvou prímek se nejjednodušeji odhalí tak, že se jedna z nich natocí o 90 stupnu a otestuje 
		//se jejich rovnobežnost - proc to zbytecne komplikovat...
		public bool ArePerpendicularLines(Line2D primka)// Jsou prímky kolmé?
		{
			Line2D pom = new Line2D(-primka.b, primka.a, primka.c);// Prímka s kolmým vektorem

			return AreParallelLines(pom);
		}
		#endregion

		#region InterceptPoint()
		//Dostáváme se k podstate celého clánku - prusecík dvou prímek. Nejdríve otestujeme jestli se 
		//nejedná o dve splývající prímky, pokud ano, mají nekonecne mnoho spolecných bodu. Nejsou-li 
		//splývající, mohou být ješte rovnobežné, pak nemají žádný spolecný bod. Ve všech ostatních prípadech 
		//mají pouze jeden spolecný bod a tím je prusecík. Protože musí vyhovovat soucasne obema rovnicím, 
		//rešíme soustavu dvou rovnic o dvou neznámých x a y.

		//Pokud funkce vrátí true, byl prusecík nalezen, souradnice uložíme do referencí retx a rety. False 
		//indikuje bud žádný prusecík (rovnobežné prímky), nebo nekonecne mnoho spolecných bodu (splývající prímky).
		public bool InterceptPoint(Line2D primka, ref double retx, ref double rety)// Prusecík prímek
		{
			if(this == primka)// Prímky jsou splývající - nekonecne mnoho spolecných bodu
			{
				return false;// Spíše by se melo vrátit true a nejaký bod... záleží na použití
			}
			else if(AreParallelLines(primka))// Prímky jsou rovnobežné - žádný spolecný bod
			{
				return false;
			}
			else// Jeden spolecný bod - prusecík (vyhovuje soucasne obema rovnicím)
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
		public static bool InterceptPoint(double x1, double y1, double x2, double y2, double pX, double pY, ref double retx, ref double rety)// Prusecík prímek
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
		//Úhel dvou prímek je úhlem dvou smerových vektoru, mužeme však použít i normálové vektory, protože 
		//výsledek bude stejný. Kosinus úhlu se rovná zlomku, u kterého se v citateli nachází skalární 
		//soucin vektoru (násobí se zvlášt x a zvlášt y složky) a ve jmenovateli soucin délek vektoru (Pythagorova 
		//veta). Pokud nechápete, berte to jako vzorec.
		public double Angle(Line2D primka)// Úhel prímek
		{
			return Math.Acos((a*primka.a + b*primka.b) / (Math.Sqrt(a*a + b*b) * Math.Sqrt(primka.a*primka.a + primka.b*primka.b)));
		}
		#endregion

		#region DistancePointToLine()
		//Vzdálenost bodu od prímky je už trochu složitejší. Vypocte se rovnice prímky, která je kolmá k zadané 
		//prímce a prochází urceným bodem. Potom se najde prusecík techto prímek a vypocte se vzdálenost bodu. 
		//Celý tento postup se ale dá mnohonásobne zjednodušit, když si najdete vzorec v matematicko fyzikálních tabulkách :-)
		public double DistancePointToLine(double x, double y)// Vzdálenost bodu od prímky
		{
			double vzdalenost = (a*x + b*y + c) / Math.Sqrt(a*a + b*b);

			if(vzdalenost < 0.0)// Absolutní hodnota
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
