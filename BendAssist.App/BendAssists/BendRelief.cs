using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
   #region Constructors --------------------------------------------
   public BendRelief (Part part) => (mPart, mPLines) = (part, part.PLines);
   #endregion

   #region Methods -------------------------------------------------
   /// <summary>Method to assess whether bend relief necessary for slected base and bend line</summary>
   public override bool Assisted () {
      return true;
   }

   /// <summary>Applies bend relief for the given part</summary>
   public override void Execute () {
      if (mPart != null && mPart.BendLines.Count != 0) {
         mCommonVertices = mPart.Vertices.GroupBy (p => p).Where (p => p.Count () > 1).Select (g => g.Key)
                           .Where (x => x.IsWithinBound (mPart.Bound)).ToList ();
         mHLines = mPLines.Where (x => x.Orientation == EOrientation.Horizontal).ToList ();
         mVLines = mPLines.Where (x => x.Orientation == EOrientation.Vertical).ToList ();
         ApplyBendRelief (mPart);
      }
      return;
   }
   #endregion

   #region Implementation ------------------------------------------
   /// <summary>Method which creates a new processed part after craeting bend relief</summary>
   ProcessedPart ApplyBendRelief (Part part) {
      List<PLine> pLines = mPLines;
      foreach (var vertex in mCommonVertices) {
         foreach (var bl in part.BendLines) {
            if (bl.HasVertex (vertex)) {
               bool isHorizontal = bl.Orientation == EOrientation.Horizontal;
               Point2 p1 = vertex, p2, p3, p4;
               PLine nearAlignedLine = null!;
               (float blAngle, float radius, float deduction) = bl.BLInfo;
               double brHeight = BendUtils.GetBendAllowance ((double)blAngle, 0.38, part.Thickness, radius) / 2;
               double brWidth = part.Thickness / 2;
               PLine? parallelLine = GetNearestParallelLine (bl.Orientation == EOrientation.Horizontal ? mHLines : mVLines, bl);
               if (parallelLine != null) nearAlignedLine = parallelLine;
               double angle = bl.Angle, translateAngle1, translateAngle2;
               angle = angle switch {
                  180 => 0,
                  270 => 90,
                  _ => bl.Angle,
               };
               if (isHorizontal) {
                  translateAngle1 = vertex.Y > part.Centroid.Y ? angle + 270 : angle + 90;
                  translateAngle2 = vertex.X < part.Centroid.X ? angle + 180 : angle;
               } else {
                  translateAngle1 = vertex.X < part.Centroid.X ? angle - 90 : angle + 90;
                  translateAngle2 = vertex.Y < part.Centroid.Y ? angle + 180 : angle;
               }
               p2 = vertex.RadialMove (brHeight, translateAngle1);
               p3 = p2.RadialMove (brWidth, translateAngle2);
               p4 = FindIntersectPoint (nearAlignedLine, p3, 180 - translateAngle1);
               var reliefLines = new PLine[4];
               reliefLines[0] = new PLine (p1, p2);
               reliefLines[1] = new (p2, p3);
               reliefLines[2] = new PLine (p3, p4);
               reliefLines[3] = new (p4, vertex == nearAlignedLine.StartPoint ? nearAlignedLine.EndPoint : nearAlignedLine.StartPoint);
               for (int i = 0; i < 4; i++)
                  pLines.Add (reliefLines[i]);
               pLines.Remove (nearAlignedLine);
            }
         }
      }
      return new ProcessedPart (pLines, part.BendLines, (float)part.Thickness, EBendAssist.BendRelief);
   }

   /// <summary>Find distance between a line and a bend line</summary>
   double GetDistanceToLine (PLine pLine, BendLine bLine) =>
           bLine.Orientation == EOrientation.Horizontal ? Math.Abs (pLine.StartPoint.Y - bLine.StartPoint.Y)
                                : Math.Abs (pLine.StartPoint.X - bLine.StartPoint.X);

   /// <summary>Find a point of intersection for given line and a line drawn at given angle from other point</summary>
   Point2 FindIntersectPoint (PLine pLine, Point2 p, double angle) {
      Point2 p1 = p.Translate (new Vector2 (1 * (Math.Sin (angle.ToRadians ())), 1 * (Math.Cos (angle.ToRadians ()))));
      double slope1 = (p1.Y - p.Y) / (p1.X - p.X), slope2 = (pLine.EndPoint.Y - pLine.StartPoint.Y) / (pLine.EndPoint.X - pLine.StartPoint.X);
      double intercept1 = p.Y - slope1 * (p.X), intercept2 = pLine.StartPoint.Y - slope2 * (pLine.StartPoint.X);
      double commonX = intercept2 - intercept1 / slope1 - slope2;
      return (slope1, slope2) switch {
         (0, 0) => new Point2 (p.X, pLine.StartPoint.Y),
         (double.NegativeInfinity or double.PositiveInfinity, double.NegativeInfinity or double.PositiveInfinity) => new Point2 (pLine.StartPoint.X, p.Y),
         _ => new Point2 (commonX, slope1 * commonX + intercept1)
      };
   }

   /// <summary>Find the nearest parallel line to the bendline from the given list of lines</summary>
   PLine GetNearestParallelLine (List<PLine> pLines, BendLine bLine) {
      List<PLine> p = [.. pLines.OrderBy (line => GetDistanceToLine (line, bLine))];
      if (p.Count > 0)
         return p.First ();
      return null!;
   }
   #endregion

   #region Private --------------------------------------------------
   readonly List<PLine> mPLines;
   List<Point2> mCommonVertices = [];
   List<PLine> mHLines = [];
   List<PLine> mVLines = [];
   #endregion
}
#endregion