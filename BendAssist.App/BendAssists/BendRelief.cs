using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
   #region Constructors --------------------------------------------
   public BendRelief (Part part) => mPart = part;
   #endregion

   #region Methods -------------------------------------------------
   /// <summary>Method to assess whether bend relief necessary for slected base and bend line</summary>
   public override bool Assisted () {
      return true;
   }

   /// <summary>Applies bend relief for the given part</summary>
   public override void Execute () {
      if (mPart is null || mPart.BendLines is null || mPart.BendLines.Count is 0) return;
      mCommonVertices = [..mPart.Vertices.GroupBy (p => p).Where (p => p.Count () == 2).Select (g => g.Key)
                       .Where (x => x.IsWithinBound (mPart.Bound))]; // Get vertices which are common to bend line points and also within the profile bound
      if (mCommonVertices.Count < 1) return;
      List<PLine> pLines = [.. mPart.PLines];
      foreach (var vertex in mCommonVertices) {
         foreach (var bl in mPart.BendLines) {
            if (bl.HasVertex (vertex) && bl.Orientation is not EOrientation.Inclined) {
               List<PLine>? connectedLines = [.. pLines.Where (x => x.HasVertex (vertex)).Where (x => x.Orientation == bl.Orientation)];
               if (connectedLines.Count is 0) return;
               PLine nearBaseEdge = connectedLines.First (); // Get the near base edge where relief edges will be inserted
               Point2 p1 = vertex, p2, p3, p4, center = mPart.Centroid;
               (float blAngle, float radius, float deduction) = bl.BLInfo;
               // Calculating the bend relief height from bend allownace or flat width
               double brHeight = BendUtils.GetBendAllowance ((double)blAngle, 0.38, mPart.Thickness, radius) / 2;
               double brWidth = mPart.Thickness / 2; // Calculating bend relief width from part thickness 
               double angle = bl.Angle, translateAngle1, translateAngle2;
               angle = angle switch { 180 => 0, 270 => 90, _ => bl.Angle, };
               if (bl.Orientation == EOrientation.Horizontal) {
                  translateAngle1 = vertex.Y > center.Y ? angle + 270 : angle + 90;
                  translateAngle2 = vertex.X < center.X ? angle + 180 : angle;
               } else {
                  translateAngle1 = vertex.X < center.X ? angle - 90 : angle + 90;
                  translateAngle2 = vertex.Y < center.Y ? angle + 180 : angle;
               }
               p2 = vertex.RadialMove (brHeight, translateAngle1);
               p3 = p2.RadialMove (brWidth, translateAngle2);
               p4 = FindIntersectPoint (nearBaseEdge, p3, translateAngle1);
               pLines.Remove (nearBaseEdge);
               Point2[] pts = vertex.IsEqual (nearBaseEdge.EndPoint) ? [nearBaseEdge.StartPoint, p4, p3, p2, p1] : [p1, p2, p3, p4, nearBaseEdge.EndPoint];
               pLines.AddRange (BendUtils.CreateConnectedPLines (nearBaseEdge.Index, pts));
            }
         }
      }
      mProcessedPart = new ProcessedPart (pLines, mPart.BendLines, (float)mPart.Thickness, EBendAssist.BendRelief);
   }
   #endregion

   #region Implementation ------------------------------------------
   /// <summary>Find a point of intersection for given line and a line drawn at given angle from other point</summary>
   Point2 FindIntersectPoint (PLine pLine, Point2 p, double angle) {
      Point2 p1 = p.RadialMove (1, angle);
      (double startX, double startY) = (pLine.StartPoint.X, pLine.StartPoint.Y);
      (double slope1, double slope2) = ((p1.Y - p.Y) / (p1.X - p.X), (pLine.EndPoint.Y - startY) / (pLine.EndPoint.X - startX));
      (double intercept1, double intercept2) = (p.Y - slope1 * p.X, startY - slope2 * startX);
      double commonX = intercept2 - intercept1 / slope1 - slope2;
      return (slope1, slope2) switch {
         (double.NegativeInfinity or double.PositiveInfinity, 0) => new Point2 (p.X, startY),
         (0, double.NegativeInfinity or double.PositiveInfinity) => new Point2 (startX, p.Y),
         _ => new Point2 (commonX, slope1 * commonX + intercept1)
      };
   }
   #endregion

   #region Private --------------------------------------------------
   List<Point2>? mCommonVertices = [];
   #endregion
}
#endregion