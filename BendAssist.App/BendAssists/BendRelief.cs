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
      if (mPart is null || mPart.BendLines is null || mPart.BendLines.Count is 0) return;
      mCommonVertices = mPart.Vertices.GroupBy (p => p).Where (p => p.Count () == 2).Select (g => g.Key)
                        .Where (x => x.IsWithinBound (mPart.Bound)).ToList ();
      mProcessedPart = ApplyBendRelief (mPart);
   }
   #endregion

   #region Implementation ------------------------------------------
   /// <summary>Method which creates a new processed part after craeting bend relief</summary>
   ProcessedPart ApplyBendRelief (Part part) {
      List<PLine> pLines = [.. mPLines];
      foreach (var vertex in mCommonVertices) {
         foreach (var bl in part.BendLines) {
            if (bl.HasVertex (vertex)) {
               if (bl.Orientation is EOrientation.Inclined) return null!;
               bool isHorizontal = bl.Orientation == EOrientation.Horizontal;
               Point2 p1 = vertex, p2, p3, p4;
               (float blAngle, float radius, float deduction) = bl.BLInfo;
               double brHeight = BendUtils.GetBendAllowance ((double)blAngle, 0.38, part.Thickness, radius) / 2;
               double brWidth = part.Thickness / 2;
               List<PLine>? connectedLines = [.. pLines.Where (x => x.HasVertex (vertex)).Where (x => x.Orientation == bl.Orientation)];
               PLine? nearBaseEdge = connectedLines.Count > 0 ? connectedLines.First () : null;
               if (nearBaseEdge is null) return null!;
               double angle = bl.Angle, translateAngle1, translateAngle2;
               angle = angle switch { 180 => 0, 270 => 90, _ => bl.Angle, };
               if (isHorizontal) {
                  translateAngle1 = vertex.Y > part.Centroid.Y ? angle + 270 : angle + 90;
                  translateAngle2 = vertex.X < part.Centroid.X ? angle + 180 : angle;
               } else {
                  translateAngle1 = vertex.X < part.Centroid.X ? angle - 90 : angle + 90;
                  translateAngle2 = vertex.Y < part.Centroid.Y ? angle + 180 : angle;
               }
               p2 = vertex.RadialMove (brHeight, translateAngle1);
               p3 = p2.RadialMove (brWidth, translateAngle2);
               p4 = FindIntersectPoint (nearBaseEdge, p3, translateAngle1);
               pLines.Add (new PLine (p1, p2));
               pLines.Add (new (p2, p3));
               pLines.Add (new PLine (p3, p4));
               pLines.Add (new (p4, vertex == nearBaseEdge.StartPoint ? nearBaseEdge.EndPoint : nearBaseEdge.StartPoint));
               pLines.Remove (nearBaseEdge);
            }
         }
      }
      return new ProcessedPart (pLines, part.BendLines, (float)part.Thickness, EBendAssist.BendRelief);
   }

   /// <summary>Find a point of intersection for given line and a line drawn at given angle from other point</summary>
   Point2 FindIntersectPoint (PLine pLine, Point2 p, double angle) {
      Point2 p1 = p.RadialMove (1, angle);
      double startX = pLine.StartPoint.X, startY = pLine.StartPoint.Y, endX = pLine.EndPoint.X, endY = pLine.EndPoint.Y;
      double slope1 = (p1.Y - p.Y) / (p1.X - p.X), slope2 = (endY - startY) / (endX - startX);
      double intercept1 = p.Y - slope1 * p.X, intercept2 = startY - slope2 * startX;
      double commonX = intercept2 - intercept1 / slope1 - slope2;
      return (slope1, slope2) switch {
         (double.NegativeInfinity or double.PositiveInfinity, 0) => new Point2 (p.X, startY),
         (0, double.NegativeInfinity or double.PositiveInfinity) => new Point2 (startX, p.Y),
         _ => new Point2 (commonX, slope1 * commonX + intercept1)
      };
   }
   #endregion

   #region Private --------------------------------------------------
   readonly List<PLine> mPLines;
   List<Point2> mCommonVertices = [];
   #endregion
}
#endregion