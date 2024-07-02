using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class CornerRelief ------------------------------------------------------------------------
public sealed class CornerRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerRelief (Part part) => (Part, mBendAllowance) = (part, 4.335);
   #endregion

   #region Methods --------------------------------------------------
   public override void Execute () {
      if (mPart is null) return;
      List<Point2> intersectPts = [], // Intersect points between the two bend lines.
                   pLinesStartPts = [], // All starting points of the PLines.
                   pLinesEndPts = []; // All ending points of the PLines.
      if (mPart.BendLines.Count >= 2) {
         for (int i = 0; i < mPart.BendLines.Count - 1; i++) {
            var (p1, cP1) = GetIntersectBendPts (mPart.BendLines[i].StartPoint, i);
            if (p1 > 0) intersectPts.Add (cP1);
            var (p2, cP2) = GetIntersectBendPts (mPart.BendLines[i].EndPoint, i);
            if (p2 > 0) intersectPts.Add (cP2);
         }
      }
      if (intersectPts.Count == 0) {
         mCanAssist = false;
         mAssistError = "No bend line intersection exists";
         return;
      }
      foreach (var point in mPart.PLines) {
         pLinesStartPts.Add (point.StartPoint);
         pLinesEndPts.Add (point.EndPoint);
      }
      for (int i = 0; i < intersectPts.Count; i++) {
         if (pLinesStartPts.Contains (intersectPts[i]))
            mIntersectPts.Add (intersectPts[i]);
         else if (pLinesEndPts.Contains (intersectPts[i]))
            mIntersectPts.Add (intersectPts[i]);
      }
      UpdatedVertices ();
      mProcessedPart = new (UpdatedPLines (), mPart.BendLines, 2, EBendAssist.CornerRelief);
   }
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Get a common point between the two bend lines</summary>
   // check the point with other bend line points (it can be either start or end point)
   (int, Point2) GetIntersectBendPts (Point2 point, int index) {
      List<Point2> tempPoint = [];
      while (++index < mPart!.BendLines.Count) {
         if (point.IsEqual (mPart.BendLines[index].StartPoint)) tempPoint.Add (point);
         if (point.IsEqual (mPart.BendLines[index].EndPoint)) tempPoint.Add (point);
      }
      return (tempPoint.Count, tempPoint.FirstOrDefault ());
   }

   /// <summary>Each new 45 degree points for an intersect points between each two plines and bend lines</summary>
   List<Point2> UpdatedVertices () {
      Dictionary<Point2, List<Point2>> intersectPtAndBendLines = [];// Intersect point
      //along with respective bend lines another point.
      for (int i = 0; i < mIntersectPts.Count; i++) {
         List<Point2> tempPoint = [];
         foreach (var pLine in mPart!.BendLines) {
            if (pLine.StartPoint.IsEqual (mIntersectPts[i])) tempPoint.Add (pLine.EndPoint);
            if (pLine.EndPoint.IsEqual (mIntersectPts[i])) tempPoint.Add (pLine.StartPoint);
         }
         intersectPtAndBendLines.Add (mIntersectPts[i], tempPoint);
      }
      for (int i = 0; i < mIntersectPts.Count; i++) {
         List<Point2> bendLinePoints = intersectPtAndBendLines[mIntersectPts[i]];
         var x = mIntersectPts[i].X < Math.Abs (bendLinePoints[1].X - bendLinePoints[0].X) ?
                 mIntersectPts[i].X + Math.Round (mBendAllowance / 2, 3) :
                 mIntersectPts[i].X - Math.Round (mBendAllowance / 2, 3);

         var y = mIntersectPts[i].Y < Math.Abs (bendLinePoints[1].Y - bendLinePoints[0].Y) ?
                 mIntersectPts[i].Y + Math.Round (mBendAllowance / 2, 3) :
                 mIntersectPts[i].Y - Math.Round (mBendAllowance / 2, 3);
         mNew45DegVertices.Add (new Point2 (x, y));
      }
      return mNew45DegVertices;
   }

   /// <summary>Updated new plines to recreate the exist part with the addition of corner relief</summary>
   List<PLine> UpdatedPLines () {
      List<int> changeIndex = [];
      foreach (var pLine in mPart!.PLines) {
         for (int i = 0; i < mIntersectPts.Count; i++) {
            if (pLine.StartPoint.IsEqual (mIntersectPts[i])) changeIndex.Add (pLine.Index);
            else if (pLine.EndPoint.IsEqual (mIntersectPts[i])) changeIndex.Add (pLine.Index);
         }
      }
      int len = mPart.PLines.Count + (mNew45DegVertices.Count * 2);
      List<PLine> tempPLines = [];
      for (int i = 1, index = 1, loopBreaker = 0; tempPLines.Count < len; i++) {
         if (loopBreaker == 0 && !changeIndex.Contains (i)) tempPLines.Add (mPart.PLines[i - 1]);
         else if (!changeIndex.Contains (i))
            tempPLines.Add (new PLine (mPart.PLines[i - 1].StartPoint, mPart.PLines[i - 1].EndPoint, index++));
         else {
            if (loopBreaker == 0) {
               loopBreaker = 1; index = i;
            }
            int choose = 0;
            List<Point2> tempPoint = [];
            foreach (var point in mIntersectPts) {
               if (mPart.PLines[i - 1].StartPoint.IsEqual (point) || mPart.PLines[i - 1].EndPoint.IsEqual (point)) {
                  tempPoint = GetPLines (mPart.PLines[i - 1], mPart.PLines[i], point, mNew45DegVertices[choose]);
                  break;
               }
               choose++;
            }
            for (int a = 0; a < tempPoint.Count - 1; a++)
               tempPLines.Add (new PLine (tempPoint[a], tempPoint[a + 1], index++));
            i += 1;
         }
      }
      return tempPLines;
   }

   /// <summary>Get a new list of plines for corner relief</summary>
   List<Point2> GetPLines (PLine first, PLine second, Point2 cPoint, Point2 new45DegPoint) {
      List<Point2> tempPoint = [first.StartPoint, new45DegPoint, second.EndPoint];
      double px1 = Math.Round (first.StartPoint.X, 3), px2 = Math.Round (second.EndPoint.X, 3),
             py1 = Math.Round (first.StartPoint.Y, 3), py2 = Math.Round (second.EndPoint.Y, 3),
             cpx1 = Math.Round (cPoint.X, 3), cpx2 = Math.Round (cPoint.Y, 3);
      tempPoint.Insert (1, GetPoint (px1, py1, cpx1, cpx2, mBendAllowance));
      tempPoint.Insert (3, GetPoint (px2, py2, cpx1, cpx2, mBendAllowance));
      return tempPoint;
   }

   /// <summary>Get a new point for corner relief</summary>
   Point2 GetPoint (double px, double py, double cx, double cy, double bendAllowance) {
      if (cx.IsEqual (px) && cy > py) return new Point2 (cx, cy - bendAllowance / 2);
      else if (cx.IsEqual (px) && cy < py) return new Point2 (cx, cy + bendAllowance / 2);
      else if (cy.IsEqual (py) && cx > px) return new Point2 (cx - bendAllowance / 2, cy);
      else if (cy.IsEqual (py) && cx < px) return new Point2 (cx + bendAllowance / 2, cy);
      return new Point2 ();
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<Point2> mIntersectPts = [], // Intersect point between the two bend lines and plines.
                mNew45DegVertices = []; // 45 degree points.
   readonly double mBendAllowance; // Bend Allowance value.(Predefined material 1.0038 with a bend radius value is 2)
   #endregion
}
#endregion