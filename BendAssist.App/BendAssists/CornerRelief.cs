using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class CornerRelief ------------------------------------------------------------------------
public sealed class CornerRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerRelief (Part part) {
      mPart = part;
      mBendAllowance = 4.335; // Material 1.0038 with a bend radius value is 2.
   }
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      return true;
   }

   public override void Execute () {
      if (mPart is null) return;
      List<Point2> commonBendPoints = [], // Intersect points between the two bend lines.
      pLinesStartPts = [], // All starting points of the PLines.
      pLinesEndPts = []; // All ending points of the PLines.
      if (mPart.BendLines.Count >= 2) {
         for (int i = 0; i < mPart.BendLines.Count - 1; i++) {
            var (repeatedPoint1, cPoint1) = GetCommonBendPoints (mPart.BendLines[i].StartPoint, i);
            if (repeatedPoint1 > 0) commonBendPoints.Add (cPoint1);
            var (repeatedPoint2, cPoint2) = GetCommonBendPoints (mPart.BendLines[i].EndPoint, i);
            if (repeatedPoint2 > 0) commonBendPoints.Add (cPoint2);
         }
      }
      if (commonBendPoints.Count == 0) {
         mProcessedPart = new (mPart.PLines, mPart.BendLines, 2, EBendAssist.CornerRelief);
         mCanAssist = false;
         return;
      }
      if (mPart.PLines.Count > 0) {
         foreach (var point in mPart.PLines) {
            pLinesStartPts.Add (point.StartPoint);
            pLinesEndPts.Add (point.EndPoint);
         }
         for (int i = 0; i < commonBendPoints.Count; i++) {
            if (pLinesStartPts.Contains (commonBendPoints[i]))
               mCommonBendAndPlinesPts.Add (commonBendPoints[i]);
            else if (pLinesEndPts.Contains (commonBendPoints[i]))
               mCommonBendAndPlinesPts.Add (commonBendPoints[i]);
         }
      }
      UpdatedVertices ();
      mProcessedPart = new (UpdatedPLines (), mPart.BendLines, 2, EBendAssist.CornerRelief);
   }
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Get a common point bewteen the two bend lines</summary>
   (int, Point2) GetCommonBendPoints (Point2 point, int index) {
      List<Point2> tempPoint = [];
      while (++index < mPart!.BendLines.Count) {
         if (point.AreEqual (mPart.BendLines[index].StartPoint)) tempPoint.Add (point);
         if (point.AreEqual (mPart.BendLines[index].EndPoint)) tempPoint.Add (point);
      }
      return (tempPoint.Count, tempPoint.FirstOrDefault ());
   }

   /// <summary>New 45 degree points for common intersect points between each two plines and bend lines</summary>
   List<Point2> UpdatedVertices () {
      Dictionary<Point2, List<Point2>> commonPointAndBendLines = [];
      for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
         List<Point2> tempPoint = [];
         foreach (var pLine in mPart!.BendLines) {
            if (pLine.StartPoint.AreEqual (mCommonBendAndPlinesPts[i])) tempPoint.Add (pLine.EndPoint);
            if (pLine.EndPoint.AreEqual (mCommonBendAndPlinesPts[i])) tempPoint.Add (pLine.StartPoint);
         }
         commonPointAndBendLines.Add (mCommonBendAndPlinesPts[i], tempPoint);
      }
      for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
         List<Point2> bendLinePoints = commonPointAndBendLines[mCommonBendAndPlinesPts[i]];
         var x = mCommonBendAndPlinesPts[i].X < Math.Abs (bendLinePoints[1].X - bendLinePoints[0].X) ?
               mCommonBendAndPlinesPts[i].X + Math.Round (mBendAllowance / 2, 3) :
               mCommonBendAndPlinesPts[i].X - Math.Round (mBendAllowance / 2, 3);

         var y = mCommonBendAndPlinesPts[i].Y < Math.Abs (bendLinePoints[1].Y - bendLinePoints[0].Y) ?
               mCommonBendAndPlinesPts[i].Y + Math.Round (mBendAllowance / 2, 3) :
               mCommonBendAndPlinesPts[i].Y - Math.Round (mBendAllowance / 2, 3);
         mNew45DegVertices.Add (new Point2 (x, y));
      }
      return mNew45DegVertices;
   }

   /// <summary>Updated new plines to recreate the exist part</summary>
   List<PLine> UpdatedPLines () {
      List<int> changingIndex = [];
      foreach (var pLine in mPart!.PLines) {
         for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
            if (pLine.StartPoint.AreEqual (mCommonBendAndPlinesPts[i])) changingIndex.Add (pLine.Index);
            else if (pLine.EndPoint.AreEqual (mCommonBendAndPlinesPts[i])) changingIndex.Add (pLine.Index);
         }
      }
      int len = mPart.PLines.Count + (mNew45DegVertices.Count * 2);
      List<PLine> tempPLines = [];
      for (int i = 1, index = 1, loopBreaker = 0; tempPLines.Count < len; i++) {
         if (loopBreaker == 0 && !changingIndex.Contains (i)) tempPLines.Add (mPart.PLines[i - 1]);
         else if (!changingIndex.Contains (i))
            tempPLines.Add (new PLine (mPart.PLines[i - 1].StartPoint, mPart.PLines[i - 1].EndPoint, index++));
         else {
            if (loopBreaker == 0) {
               loopBreaker = 1; index = i;
            }
            int choose = 0;
            List<Point2> tempPoint = [];
            foreach (var point in mCommonBendAndPlinesPts) {
               if (mPart.PLines[i - 1].StartPoint.AreEqual (point) || mPart.PLines[i - 1].EndPoint.AreEqual (point)) {
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

   /// <summary>Get a new list of plines</summary>
   List<Point2> GetPLines (PLine first, PLine second, Point2 cPoint, Point2 new45DegPoint) {
      List<Point2> tempPoint = [first.StartPoint, new45DegPoint, second.EndPoint];
      double px1 = Math.Round (first.StartPoint.X, 3), px2 = Math.Round (second.EndPoint.X, 3),
             py1 = Math.Round (first.StartPoint.Y, 3), py2 = Math.Round (second.EndPoint.Y, 3),
             cpx1 = Math.Round (cPoint.X, 3), cpx2 = Math.Round (cPoint.Y, 3);
      tempPoint.Insert (1, GetPoint (px1, py1, cpx1, cpx2, mBendAllowance));
      tempPoint.Insert (3, GetPoint (px2, py2, cpx1, cpx2, mBendAllowance));
      return tempPoint;
   }

   /// <summary>Get a new point</summary>
   static Point2 GetPoint (double px, double py, double cx, double cy, double bendAllowance) {
      if (cx.IsEqual (px) && cy > py) return new Point2 (cx, cy - bendAllowance / 2);
      else if (cx.IsEqual (px) && cy < py) return new Point2 (cx, cy + bendAllowance / 2);
      else if (cy.IsEqual (py) && cx > px) return new Point2 (cx - bendAllowance / 2, cy);
      else if (cy.IsEqual (py) && cx < px) return new Point2 (cx + bendAllowance / 2, cy);
      return new Point2 ();
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<Point2> mCommonBendAndPlinesPts = [], // Common intersect points between the two bend lines and plines.
      mNew45DegVertices = []; // New 45 degree points.
   double mBendAllowance; // Bend Allowance value.
   #endregion
}
#endregion