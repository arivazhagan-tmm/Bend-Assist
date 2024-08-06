using static System.Math;
using System.Windows;
using System.Windows.Media;
using BendAssist.App.Model;
using System.Text.RegularExpressions;
using BendAssist.App.BendAssists;

namespace BendAssist.App.Utils;

#region class BendUtils ---------------------------------------------------------------------------
public static class BendUtils {
   /// <summary>Area enclosed by the given points</summary>
   public static double Area (this List<Point2> pts) {
      var (n, area) = (pts.Count, 0.0);
      for (int i = 0; i < n - 1; i++) area += pts[i].X * pts[i + 1].Y - pts[i].Y * pts[i + 1].X;
      area += pts[n - 1].X * pts[0].Y - pts[n - 1].Y * pts[0].X;
      return Abs (area) / 2;
   }

   /// <summary>Centroid of the given points</summary>
   public static Point2 Centroid (this List<Point2> pts) {
      var (xCords, yCords) = (pts.Select (p => p.X), pts.Select (p => p.Y));
      var (minX, minY, maxX, maxY) = (xCords.Min (), yCords.Min (), xCords.Max (), yCords.Max ());
      return new ((minX + maxX) * 0.5, (minY + maxY) * 0.5);
   }

   /// <summary>Converts and returns point2 to Windows.Point</summary>
   public static Point Convert (this Point2 p) => new (p.X, p.Y);

   /// <summary>Calculates and returns the convex hull points</summary>
   public static List<Point2> ConvexHull (this List<Point2> pts) {
      List<Point2> hull = [];
      int n = pts.Count;
      if (n > 3) {
         int l = 0, p, q;
         for (int i = 1; i < n; i++) if (pts[i].X < pts[l].X) l = i;
         p = l;
         do {
            hull.Add (pts[p]);
            q = (p + 1) % n;
            for (int i = 0; i < n; i++) if (Orientation (pts[p], pts[i], pts[q]) == 2) q = i;
            p = q;
         } while (p != l);
      }
      return hull;
   }

   public static Bound2 CreateBoundAround (PLine p) {
      var (p1, p2) = (p.StartPoint, p.EndPoint);
      var theta = p1.AngleTo (p2);
      var (t1, t2, offset) = (theta + 90, theta - 90, 2.0);
      return new Bound2 ([p1.RadialMoved (offset, t1), p1.RadialMoved (offset, t2), p2.RadialMoved (offset, t1), p2.RadialMoved (offset, t2)]);
   }

   /// <summary>Creates and returns the list of connected plines with the given points.</summary>
   public static List<PLine> CreateConnectedPLines (int index, params Point2[] pts) {
      var plines = new List<PLine> ();
      for (int i = 0, len = pts.Length - 1; i < len; i++) plines.Add (new PLine (pts[i], pts[i + 1], index));
      return plines;
   }

   public static double GetBendDeduction (double angle, double kFactor, double thickness, double radius) {
      angle = angle.ToRadians ();
      var totalSetBack = 2 * ((radius + thickness) * Math.Tan (angle / 2));
      var bendAllowance = angle * (kFactor * thickness + radius);
      return double.Round (Abs (totalSetBack - bendAllowance), 3);
   }

   public static double GetBendAllowance (double angle, double kFactor, double thickness, double radius)
      => angle.ToRadians () * (kFactor * thickness + radius);

   /// <summary>Returns the array of indices of the connected pLines from the given pLine</summary>
   public static int[] GetCPIndices (PLine refPLine, List<PLine> pLines) {
      var (start, end) = (refPLine.StartPoint, refPLine.EndPoint);
      return pLines.Where (c => c.Index != refPLine.Index && (c.HasVertex (start) || c.HasVertex (end))).Select (c => c.Index).ToArray ();
   }

   /// <summary>Checks if the given line present in the list or not by comparing the vertices</summary>
   public static bool HasDuplicate (this List<Line> lines, Line l) {
      foreach (var line in lines)
         if (line.StartPoint.IsEqual (l.StartPoint) && line.EndPoint.IsEqual (l.EndPoint)) return true;
      return false;
   }

   /// <summary>Returns true if start point or end point of the line is equal to given point p</summary>
   public static bool HasVertex (this Line l, Point2 p) {
      var index = p.Index;
      return l.StartPoint.Index == index || l.EndPoint.Index == index;
   }

   /// <summary>Inserts the given pline at the lines at the given index</summary>
   public static List<PLine> InsertAt (this PLine l, int index, List<PLine> lines) {
      if (lines.Count == 0) { lines.Add (l); return lines; }
      var len = lines.Count + 1;
      var tmp = new PLine[len];
      var (ptIndex, inserted) = (0, false);
      for (int i = 0; i < len; i++) {
         var line = lines[i];
         var (startPt, endPt) = (line.StartPoint, line.EndPoint);
         if (i == index) {
            tmp[i] = new (l.StartPoint.WithIndex (ptIndex), l.EndPoint.WithIndex (++ptIndex), i++);
            inserted = true;
         }
         tmp[i] = !inserted ? new (startPt, endPt, i++)
                            : new (startPt.WithIndex (ptIndex), endPt.WithIndex (++ptIndex), i++);
         ptIndex = line.EndPoint.Index;
      }
      return [.. tmp];
   }

   /// <summary>Returns if two plines intersect or not.If true, updates the intersection point in the out parameter</summary>
   public static bool Intersects (this PLine l, PLine other, out Point2 intersectPoint) {
      intersectPoint = new ();
      var (a, b, c, d) = (l.StartPoint, l.EndPoint, other.StartPoint, other.EndPoint);
      var (a1, b1, a2, b2) = (b.Y - a.Y, a.X - b.X, d.Y - c.Y, c.X - d.X);
      var determinant = (a1 * b2) - (a2 * b1);
      if (determinant.IsEqual (0)) return false;
      var (c1, c2) = ((a1 * a.X) + (b1 * a.Y), (a2 * c.X) + (b2 * c.Y));
      double x = (b2 * c1 - b1 * c2) / determinant;
      double y = (a1 * c2 - a2 * c1) / determinant;
      intersectPoint = new (x, y);
      return true;
   }

   /// <summary>If the given point joins any lines in the list, if any updates the lines in the out parameter</summary>
   public static bool IsCommonVertex (this Point2 p, List<Line> lines, out List<Line> connectedLines) {
      connectedLines = lines.Where (l => l.HasVertex (p)).ToList ();
      return connectedLines.Count > 1;
   }

   /// <summary>If the given line is connected with other lines, if any then updates the conncted lines in the out parameter</summary>
   public static bool IsConnected (this Line l, List<Line> lines, out List<Line> connectedLines) {
      var (v1, v2) = (l.StartPoint, l.EndPoint);
      v1.IsCommonVertex (lines, out var tmp);
      v2.IsCommonVertex (lines, out connectedLines);
      connectedLines.AddRange (tmp);
      return connectedLines.Count > 1;
   }

   public static bool IsInside (this Line l, Bound2 b) => l.StartPoint.IsWithinBound (b) && l.EndPoint.IsWithinBound (b);

   /// <summary>Checks whether a point is within the bound</summary>
   public static bool IsWithinBound (this Point2 p, Bound2 b) => p.X <= b.MaxX && p.X >= b.MinX && p.Y <= b.MaxY && p.Y >= b.MinY;

   /// <summary>Orientation of the point based on the previous two points</summary>
   public static int Orientation (Point2 p, Point2 q, Point2 r) {
      int val = (int)((q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y));
      if (val == 0) return 0; // colinear 
      return (val > 0) ? 1 : 2; // clock or counterclock wise 
   }

   /// <summary>Applies transformation on point p and returns as Point2</summary>
   public static Point2 Transform (this Point p, Matrix xfm) {
      var pt = xfm.Transform (p);
      return new Point2 (pt.X, pt.Y);
   }

   /// <summary>Returns a new bound by applying given transformation</summary>
   public static Bound2 Transform (this Bound2 b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new (new (min.X, min.Y), new (max.X, max.Y));
   }
}
#endregion

#region class CommonUtils -------------------------------------------------------------------------
public static class CommonUtils {
   #region Methods --------------------------------------------------
   /// <summary>Checking the double values are same</summary>
   public static bool IsEqual (this double a, double b) => Abs (a - b) < 1e-6;

   /// <summary>Round off the value to the given precision</summary>
   public static double Round (this double f, int precision = 2) => Math.Round (f, precision);

   /// <summary>Converts the given angle in degrees to radians</summary>
   public static double ToRadians (this double theta) => theta * sFactor;

   /// <summary>Converts the given angle in radians to degrees</summary>
   public static double ToDegrees (this double theta) => theta / sFactor;

   /// <summary>Add space between words</summary>
   public static string AddSpace (this string str) {
      var result = Regex.Split (str, @"(?=[A-Z])");
      return string.Join (" ", result);
   }

   /// <summary>Clamps the given double to lie within min..max (inclusive)</summary>
   public static double Clamp (this double a, double min, double max) => a < min ? min : (a > max ? max : a);
   /// <summary>Clamps the given double to the range 0..1</summary>
   public static double Clamp (this double a) => a < 0 ? 0 : (a > 1 ? 1 : a);

   /// <summary>Finds a quadrant of the given point using the reference point</summary>
   public static IQuadrant Quadrant (this Point2 p, Point2 refPoint) {
      var (x, y, cx, cy) = (p.X, p.Y, refPoint.X, refPoint.Y);
      if (cx < x && cy < y) return IQuadrant.I;
      else if (cx > x && cy < y) return IQuadrant.II;
      else if (cx > x && cy > y) return IQuadrant.III;
      else if (cx < x && cy > y) return IQuadrant.IV;
      return IQuadrant.None;
   }
   #endregion

   #region Private Data ---------------------------------------------
   // Factor useful for converting radians to degrees and vice versa
   static double sFactor = Math.PI / 180;
   #endregion
}
#endregion