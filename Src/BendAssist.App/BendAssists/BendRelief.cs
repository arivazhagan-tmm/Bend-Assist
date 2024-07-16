using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
   #region Constructors --------------------------------------------
   public BendRelief (Part part) => (mPart, mCenter) = (part, part.Centroid);
   #endregion

   #region Methods -------------------------------------------------
   /// <summary>Applies bend relief for the given part</summary>
   public override void Execute () {
      if (mPart is null || mPart.BendLines is null || mPart.BendLines.Count is 0) { mAssistError = "Cannot apply Bend relief"; return; }
      mCommonVertices = [..mPart.Vertices.GroupBy (p => p).Where (p => p.Count () == 2).Select (g => g.Key)
                       .Where (x => x.IsWithinBound (mPart.Bound))]; // Get vertices which are common to bend line points and also within the profile bound
      if (mCommonVertices.Count < 1) { mAssistError = "Cannot apply Bend relief"; return; }
      List<PLine> pLines = [.. mPart.PLines];
      foreach (var vertex in mCommonVertices) {
         BendLine bl = mPart.BendLines.Where (x => x.HasVertex (vertex)).First ();
         List<PLine>? connectedLines = [.. pLines.Where (x => x.HasVertex (vertex))];
         if (connectedLines.Count is 0) { mAssistError = "Cannot apply Bend relief"; return; }
         Point2 p1 = vertex, p2, p3, p4;
         (float bendAngle, float radius, float deduction) = bl.BLInfo;
         // Calculating the bend relief height from bend allownace or flat width
         double brHeight = BendUtils.GetBendAllowance ((double)bendAngle, 0.38, mPart.Thickness, radius) / 2;
         // Calculating bend relief width from part thickness
         double brWidth = mPart.Thickness / 2;
         double angle = bl.Angle;
         bool IsHorizontalRange = (angle > 135 && angle < 225) || (angle < 45 && angle >= 0) || (angle > 315 && angle <= 360);
         // Get the near base edge where relief edges will be inserted
         PLine nearBaseEdge = GetNearBaseEdge (connectedLines, vertex, IsHorizontalRange);
         angle = angle switch { 180 => 0, 270 => 90, _ => bl.Angle, };
         (double translateAngle1, double translateAngle2) = GetTranslateAngles (vertex, angle, IsHorizontalRange);
         p2 = vertex.RadialMoved (brHeight, translateAngle1);
         p3 = p2.RadialMoved (brWidth, translateAngle2);
         p4 = p3.RadialMoved (p3.DistToLine (nearBaseEdge.StartPoint, nearBaseEdge.EndPoint), translateAngle1 - 180);
         pLines.Remove (nearBaseEdge);
         Point2[] pts = vertex.IsEqual (nearBaseEdge.EndPoint) ? [nearBaseEdge.StartPoint, p4, p3, p2, p1]
                                                               : [p1, p2, p3, p4, nearBaseEdge.EndPoint];
         pLines.AddRange (BendUtils.CreateConnectedPLines (nearBaseEdge.Index, pts));
      }
      mProcessedPart = new ProcessedPart (pLines, mPart.BendLines, (float)mPart.Thickness, EBendAssist.BendRelief);
   }
   #endregion

   #region Implementation ------------------------------------------
   ///// <summary>Find a point of intersection for given line and a line drawn at given angle from other point</summary>
   //Point2 FindIntersectPoint (PLine pLine, Point2 p, double angle) {
   //   Point2 p1 = p.RadialMoved (1, angle);
   //   (double startX, double startY) = (pLine.StartPoint.X, pLine.StartPoint.Y);
   //   (double slope1, double slope2) = ((p1.Y - p.Y) / (p1.X - p.X), (pLine.EndPoint.Y - startY) / (pLine.EndPoint.X - startX));
   //   (double intercept1, double intercept2) = (p.Y - slope1 * p.X, startY - slope2 * startX);
   //   double commonX = intercept2 - intercept1 / slope1 - slope2;
   //   return (slope1, slope2) switch {
   //      (double.NegativeInfinity or double.PositiveInfinity, 0) => new Point2 (p.X, startY),
   //      (0, double.NegativeInfinity or double.PositiveInfinity) => new Point2 (startX, p.Y),
   //      _ => new Point2 (commonX, slope1 * commonX + intercept1)
   //   };
   //}

   PLine GetNearBaseEdge (List<PLine> connectedLines, Point2 vertex, bool IsHorizontalRange) => IsHorizontalRange
         ? (vertex.X < mCenter.X && vertex.Y > mCenter.Y) || (vertex.X > mCenter.X && vertex.Y < mCenter.Y)
            ? connectedLines[1] : connectedLines.First ()
         : (vertex.Y < mCenter.Y && vertex.X < mCenter.X) || (vertex.X > mCenter.X && vertex.Y > mCenter.Y)
            ? connectedLines[1] : connectedLines.First ();

   Tuple<double, double> GetTranslateAngles (Point2 vertex, double angle, bool IsHorizontalRange) => IsHorizontalRange
         ? new Tuple<double, double> (vertex.Y > mCenter.Y ? angle + 270 : angle + 90, vertex.X < mCenter.X ? angle + 180 : angle)
         : new Tuple<double, double> (vertex.X > mCenter.X ? angle + 90 : angle - 90, vertex.Y < mCenter.Y ? angle + 180 : angle);
   #endregion

   #region Private --------------------------------------------------
   List<Point2>? mCommonVertices = [];
   readonly Point2 mCenter;
   #endregion
}
#endregion