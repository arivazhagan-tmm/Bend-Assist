using static System.Math;
using BendAssist.App.Model;
using System.Windows;
using System.Windows.Media;

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

   public static bool HasDuplicate (this List<Line> lines, Line l) {
      foreach (var line in lines)
         if (line.StartPoint.AreEqual (l.StartPoint) && line.EndPoint.AreEqual (l.EndPoint)) return true;
      return false;
   }

   /// <summary>Returns true if start point or end point of the line is equal to given point p</summary>
   public static bool HasVertex (this Line l, Point2 p) {
      var index = p.Index;
      return l.StartPoint.Index == index || l.EndPoint.Index == index;
   }

   /// <summary>Inserts the given pline at the lines at the given index</summary>
   public static List<PLine> InsertAt (this PLine l, int index, List<PLine> lines) {
      var len = lines.Count + 1;
      var tmp = new PLine[lines.Count + 1];
      for (int i = 0; i < len; i++) {
         var line = lines[i];
         if (i == index) tmp[i] = new (l.StartPoint, l.EndPoint, i++);
         tmp[i] = new (line.StartPoint, line.EndPoint, i++);
      }
      return [.. tmp];
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
}
#endregion

public static class CommonUtils {
   /// <summary>Checking the double values are same</summary>
   public static bool IsEqual (this double a, double b) => Abs (a - b) < 1e-6;

   /// <summary>Round off the value to the given precision</summary>
   public static double Round (this double f, int precision = 2) => Math.Round (f, precision);

   public static Point2 Transform (this Point p, Matrix xfm) {
      var pt = xfm.Transform (p);
      return new Point2 (pt.X, pt.Y);
   }

   public static Bound2 Transform (this Bound2 b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new (new (min.X, min.Y), new (max.X, max.Y));
   }

   public static Point Convert (this Point2 p) => new (p.X, p.Y);
}
