using BendAssist.App.Model;
using BendAssist.App.Utils;
using static BendAssist.App.Model.EOrientation;

namespace BendAssist.App.BendAssists;

#region class CornerClose -------------------------------------------------------------------------
public sealed class CornerClose : BendAssist {
   #region Constructors ---------------------------------------------
   /// <summary>Gets the part and instantiates the members to accomplish corner closing</summary>
   public CornerClose (Part part) => (mPart, mIndex, mStepLines, mNewLines, mStepStartPts, mStepEndPts) = (part, -1, [], [], [], []);
   #endregion

   #region Properties -----------------------------------------------
   /// <summary>Prompts shown to the user to accomplish the corner closing</summary>
   public override string[] Prompts => ["Select the extruding line", "Select the intruding line"];
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Checks if a part is eligible for corner closing and gives the corner closed part</summary>
   public override void Execute () {
      if (mPart!.AssistInfo.Count == 0 || mPart.AssistInfo.All (ai => ai.ReqAssist == EBendAssist.BendRelief)) { mAssistError = "Cannot apply corner closing"; return; }
      var (pLines, bendLines, centroid) = (mPart.PLines, mPart.BendLines, mPart.Centroid);
      foreach (var ai in mPart.AssistInfo) {
         if (ai.ReqAssist == EBendAssist.CornerClose) {
            var lines = pLines.Where (p => ai.PLIndices.Any (l => l == p.Index)).ToList ();
            var (line1, line2) = (lines[0], lines[1]);
            mStepLines.AddRange (line1.Index != line2.Index - 1 ? [line2, line1] : [line1, line2]);
         }
      }
      mStepLines.ForEach (line => {
         mStepStartPts.Add (line.StartPoint);
         mStepEndPts.Add (line.EndPoint);
      });
      var (angle, kFactor, thickness, radius) = (90, 0.38, 2, 2);
      var halfBA = BendUtils.GetBendAllowance (angle, kFactor, thickness, radius) / 2;
      var lineShift = halfBA - radius;
      var halfBD = BendUtils.GetBendDeduction (angle, kFactor, thickness, radius) / 2;
      foreach (var pLine in pLines) {
         var (startPt, endPt) = (pLine.StartPoint, pLine.EndPoint);
         var (isCentroidAbove, isCentroidRight) = (startPt.Y < centroid.Y, startPt.X < centroid.X);
         if (mStepLines.Contains (pLine)) {
            if (mStepLines.IndexOf (pLine) % 2 == 0) {
               var (dx, dy) = pLine.Orientation switch {
                  Horizontal => isCentroidAbove ? (0.0, lineShift) : (0.0, -lineShift),
                  Vertical => isCentroidRight ? (lineShift, 0.0) : (-lineShift, 0.0),
                  _ => (0.0, 0.0)
               };
               var vect = new Vector2 (dx, dy);
               mNewLines.Add (new (pLine.StartPoint + vect, pLine.EndPoint + vect, ++mIndex));
            } else {
               var trimmedLength = pLine.Length - halfBA;
               (PLine extrudedLine, PLine trimmedLine) = pLine.Orientation switch {
                  Horizontal => isCentroidRight ? ((PLine)pLine.Trimmed (startDx: -halfBA).Translated (0.0, halfBD), (PLine)pLine.Trimmed (startDx: lineShift, endDx: trimmedLength))
                                                                     : ((PLine)pLine.Trimmed (startDx: halfBA).Translated (0.0, -halfBD), (PLine)pLine.Trimmed (startDx: -lineShift, endDx: -trimmedLength)),
                  Vertical => isCentroidAbove ? ((PLine)pLine.Trimmed (startDy: -halfBA).Translated (-halfBD, 0.0), (PLine)pLine.Trimmed (startDy: lineShift, endDy: trimmedLength))
                                                                   : ((PLine)pLine.Trimmed (startDy: halfBA).Translated (halfBD, 0.0), (PLine)pLine.Trimmed (startDy: -lineShift, endDy: -trimmedLength)),
                  _ => (null!, null!)
               };
               mNewLines.Add (new (trimmedLine.StartPoint, trimmedLine.EndPoint, ++mIndex));
               mNewLines.Add (new (trimmedLine.EndPoint, extrudedLine.StartPoint, ++mIndex));
               mNewLines.Add (new (extrudedLine.StartPoint, extrudedLine.EndPoint, ++mIndex));
            }
         } else {
            PLine trimmedEdge;
            if (mStepStartPts.Contains (endPt) && mStepEndPts.Contains (startPt)) {
               trimmedEdge = pLine.Orientation switch {
                  Horizontal => (PLine)pLine.Trimmed (startDx: isCentroidAbove ? -halfBD : halfBD, endDx: isCentroidAbove ? -lineShift : lineShift),
                  Vertical => (PLine)pLine.Trimmed (startDy: isCentroidRight ? halfBD : -halfBD, endDy: isCentroidRight ? lineShift : -lineShift),
                  _ => null!
               };
               mNewLines.Add (new (trimmedEdge.StartPoint, trimmedEdge.EndPoint, ++mIndex));
            } else if (mStepStartPts.Contains (endPt)) {
               trimmedEdge = pLine.Orientation switch {
                  Horizontal => (PLine)pLine.Trimmed (endDx: isCentroidAbove ? -lineShift : lineShift),
                  Vertical => (PLine)pLine.Trimmed (endDy: isCentroidRight ? lineShift : -lineShift),
                  _ => null!
               };
               mNewLines.Add (new (trimmedEdge.StartPoint, trimmedEdge.EndPoint, ++mIndex));
            } else if (mStepEndPts.Contains (startPt)) {
               trimmedEdge = pLine.Orientation switch {
                  Horizontal => (PLine)pLine.Trimmed (startDx: isCentroidAbove ? -halfBD : halfBD),
                  Vertical => (PLine)pLine.Trimmed (startDy: isCentroidRight ? halfBD : -halfBD),
                  _ => null!
               };
               mNewLines.Add (new (trimmedEdge.StartPoint, trimmedEdge.EndPoint, ++mIndex));
            } else mNewLines.Add (new (pLine.StartPoint, pLine.EndPoint, ++mIndex));
         }
      }
      mProcessedPart = new (mNewLines, mPart.BendLines, 2, EBendAssist.CornerClose);
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<PLine> mStepLines, mNewLines;
   List<Point2> mStepStartPts, mStepEndPts;
   int mIndex;
   #endregion
}
#endregion