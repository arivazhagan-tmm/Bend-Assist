using static System.Math;
using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region struct Point2 -----------------------------------------------------------------------------
public readonly struct Point2 {
   #region Constructors ---------------------------------------------
   public Point2 () => (X, Y) = (double.NaN, double.NaN);
   public Point2 (double x, double y, int index = -1) => (X, Y, Index) = (x, y, index);
   #endregion

   #region Properties -----------------------------------------------
   /// <summary>X ordinate</summary>
   public readonly double X;
   /// <summary>Y ordinate</summary>
   public readonly double Y;
   /// <summary>Index of the point</summary>
   public readonly int Index;
   /// <summary>Returns true if the point ordinates have valid values</summary>
   public bool IsSet => !double.IsNaN (X) && !double.IsNaN (Y);
   #endregion

   #region Methods --------------------------------------------------
   // AV. Angles internally should always be in radians.
   // Don't round off returned values from API like this.
   /// <summary>Angle made with given P in degrees</summary>
   public double AngleTo (Point2 p) {
      var angle = Round (Atan2 (p.Y - Y, p.X - X) * (180 / PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }

   /// <summary>Checks given point is a duplicate</summary>
   public bool IsEqual (Point2 p) => p.X.IsEqual (X) && p.Y.IsEqual (Y);

   /// <summary>Distance from given point p</summary>
   public double DistanceTo (Point2 p) {
      double dx = p.X - X, dy = p.Y - Y;
      return Sqrt (dx * dx + dy * dy);
   }

   /// <summary>Distance between this point and another</summary>
   public double DistTo (Point2 b) => Sqrt (DistToSq (b));

   /// <summary>Returns the perpendicular distance between this point and the inifinite line a..b</summary>
   public double DistToLine (Point2 a, Point2 b) => DistTo (SnappedToLine (a, b));

   /// <summary>Square of the distance between this point and another</summary>
   public double DistToSq (Point2 b) { double dx = b.X - X, dy = b.Y - Y; return dx * dx + dy * dy; }

   /// <summary>Copy of the point with custom index</summary>
   public Point2 WithIndex (int index) => new (X, Y, index);

   /// <summary>Nearest neighbourhood point within given proximity</summary>
   public bool HasNeighbour (IEnumerable<Point2> neighbours, double proximity, out Point2 neighbour) {
      foreach (var pt in neighbours)
         if (pt.DistanceTo (this) < proximity) { neighbour = pt; return true; }
      neighbour = new ();
      return false;
   }

   /// <summary>Radially moves the point to distance at theta in degrees</summary>
   public Point2 RadialMoved (double distance, double theta) {
      var (sin, cos) = SinCos (theta.ToRadians ());
      return new Point2 (X + distance * cos, Y + distance * sin);
   }

   /// <summary>Returns the closest point on the given line a..b</summary>
   /// If the points a and b are the same, this just returns a
   public Point2 SnappedToLine (Point2 a, Point2 b) => SnapHelper (a, b, false);
   /// <summary>Returns the closest point to the given _finite_ line segment a..b</summary>
   public Point2 SnappedToLineSeg (Point2 a, Point2 b) => SnapHelper (a, b, true);

   /// <summary>Helper used by SnappedToLine and SnappedToLineSeg</summary>
   Point2 SnapHelper (Point2 a, Point2 b, bool clamp) {
      var (dx, dy) = (b.X - a.X, b.Y - a.Y);
      double scale = 1 / (dx * dx + dy * dy);
      if (double.IsInfinity (scale)) return a;
      // Use the parametric form of the line equation, and compute
      // the 'parameter t' of the closest point
      double t = ((X - a.X) * dx + (Y - a.Y) * dy) * scale;
      if (clamp) t = t.Clamp ();
      return new (a.X + t * dx, a.Y + t * dy);
   }

   public override string ToString () => $"{X:F9},{Y:F9}";

   /// <summary>Translation with dx and dy from the vector v</summary>
   public Point2 Translated (Vector2 v) => this + v;
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
   }

   public Bound2 (IEnumerable<Point2> pts) {
      MinX = pts.Min (p => p.X);
      MaxX = pts.Max (p => p.X);
      MinY = pts.Min (p => p.Y);
      MaxY = pts.Max (p => p.Y);
   }
   #endregion

   #region Properties -----------------------------------------------
   public bool IsEmpty => MinX > MaxX || MinY > MaxY;
   public double MinX { get; init; }
   public double MaxX { get; init; }
   public double MinY { get; init; }
   public double MaxY { get; init; }
   public double Width => MaxX - MinX;
   public double Height => MaxY - MinY;
   public Point2 Mid => new ((MinX + MaxX) / 2, (MinY + MaxY) / 2);
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
}
#endregion