using static System.Math;
using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region struct Point2 -----------------------------------------------------------------------------
public readonly record struct Point2 (double X = double.NaN, double Y = double.NaN, int Index = -1) {
   #region Properties -----------------------------------------------
   public bool IsSet => !double.IsNaN (X) && !double.IsNaN (Y);
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Angle made with given P in degrees</summary>
   public double AngleTo (Point2 p) {
      var angle = Round (Atan2 (p.Y - Y, p.X - X) * (180 / PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }

   /// <summary>Checks given point is a duplicate</summary>
   public bool AreEqual (Point2 p) => p.X.IsEqual (X) && p.Y.IsEqual (Y);

   /// <summary>Distance from given point p</summary>
   public double DistanceTo (Point2 p) => Round (Sqrt (Pow (p.X - X, 2) + Pow (p.Y - Y, 2)), 2);

   /// <summary>Copy of the point with custom index</summary>
   public Point2 Duplicate (int index) => new (X, Y, index);

   /// <summary>Nearest neighbourhood point within given proximity</summary>
   public bool HasNeighbour (IEnumerable<Point2> neighbours, double proximity, out Point2 neighbour) {
      neighbour = new Point2 ();
      if (neighbours is null || neighbours.Count () is 0) return false;
      var pt = this;
      neighbour = neighbours.ToList ().Find (p => p.DistanceTo (pt).IsEqual (proximity));
      return neighbour.IsSet;
   }

   /// <summary>Radially moves the point to distance at theta in degrees</summary>
   public Point2 RadialMove (double distance, double theta) {
      theta = theta.ToRadians ();
      return new Point2 (X + distance * Cos (theta), Y + distance * Sin (theta));
   }

   /// <summary></summary>
   public override string? ToString () => $"({X.Round ()}, {Y.Round ()})";

   /// <summary>Translation with dx and dy from the vector v</summary>
   public Point2 Translate (Vector2 v) => this + v;
   #endregion

   #region Operators ------------------------------------------------
   public static Point2 operator + (Point2 p, Vector2 v) => new (p.X + v.DX, p.Y + v.DY, p.Index);
   #endregion
}
#endregion

#region struct Vector2 ----------------------------------------------------------------------------
public readonly record struct Vector2 (double DX, double DY);
#endregion

#region struct Bound2 -----------------------------------------------------------------------------
public readonly struct Bound2 {
   #region Constructors ---------------------------------------------
   public Bound2 (Point2 p1, Point2 p2) {
      MinX = Min (p1.X, p2.X);
      MaxX = Max (p1.X, p2.X);
      MinY = Min (p1.Y, p2.Y);
      MaxY = Max (p1.Y, p2.Y);
      (Height, Width) = (MaxY - MinY, MaxX - MinX);
      Mid = new ((MaxX + MinX) * 0.5, (MaxY + MinY) * 0.5);
   }

   public Bound2 (IEnumerable<Point2> pts) {
      MinX = pts.Min (p => p.X);
      MaxX = pts.Max (p => p.X);
      MinY = pts.Min (p => p.Y);
      MaxY = pts.Max (p => p.Y);
      (Height, Width) = (MaxY - MinY, MaxX - MinX);
      Mid = new ((MaxX + MinX) * 0.5, (MaxY + MinY) * 0.5);
   }
   #endregion

   #region Properties -----------------------------------------------
   public bool IsEmpty => MinX > MaxX || MinY > MaxY;
   public double MinX { get; init; }
   public double MaxX { get; init; }
   public double MinY { get; init; }
   public double MaxY { get; init; }
   public double Width { get; init; }
   public double Height { get; init; }
   public Point2 Mid { get; init; }
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Inflates and scales the bound about the given point</summary>
   public Bound2 Inflated (Point2 p, double f) {
      if (IsEmpty) return this;
      var minX = p.X - (p.X - MinX) * f;
      var maxX = p.X + (MaxX - p.X) * f;
      var minY = p.Y - (p.Y - MinY) * f;
      var maxY = p.Y + (MaxY - p.Y) * f;
      return new (new (minX, minY), new (maxX, maxY));
   }
   #endregion

   #region Implementation -------------------------------------------
   #endregion

   #region Private Data ---------------------------------------------
   #endregion
}
#endregion