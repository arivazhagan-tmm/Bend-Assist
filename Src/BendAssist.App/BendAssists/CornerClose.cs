using BendAssist.App.Model;
using static BendAssist.App.Model.EOrientation;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class CornerClose -------------------------------------------------------------------------
public sealed class CornerClose : BendAssist {
   #region Constructors ---------------------------------------------
   /// <summary>Gets the part and instantiates the members to accomplish corner closing</summary>
   public CornerClose (Part part) => (mPart, mIndex, mCcVertices, mStepLines, mNewLines, mStepStartPts, mStepEndPts) = (part, -1, [], [], [], [], []);
   #endregion

   #region Properties -----------------------------------------------
   /// <summary>Prompts shown to the user to accomplish the corner closing</summary>
   public override string[] Prompts => ["Select the extruding line", "Select the intruding line"];
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Checks if a part is eligible for corner closing and gives the corner closed part</summary>
   public override void Execute () {
      var (vertices, pLines, bendLines, centroid) = (mPart!.Vertices, mPart.PLines.Cast<Line> ().ToList (), mPart.BendLines.Cast<Line> ().ToList (), mPart.Centroid);
      var commonVertices = vertices.GroupBy (p => p).Where (p => p.Count () > 2).Select (g => g.Key).ToList (); // To find the intersecting points of bendlines with stepcut 
      if (commonVertices.Count < 1) { mAssistError = mErrorMsg; return; }
      foreach (var vertex in commonVertices) {
         vertex.IsCommonVertex (bendLines, out var connectedBLines);
         if (connectedBLines.All (b => b.Orientation != Inclined)) mCcVertices.Add (vertex); // Checks if the bend lines connected to the common vertices are not inclined
      }
      if (mCcVertices.Count < 1) { mAssistError = mErrorMsg; return; }
      foreach (var ccVertex in mCcVertices) {
         ccVertex.IsCommonVertex (pLines, out var connectedPLines);
         if (connectedPLines.Count == 2) {
            var (line1, line2) = (connectedPLines[0], connectedPLines[1]);
            if (line1.Index != line2.Index - 1) {
               mStepLines.Add ((PLine)line2); mStepLines.Add ((PLine)line1);
            } else connectedPLines.ForEach (x => mStepLines.Add ((PLine)x));
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
         if (mStepLines.Contains (pLine)) {
            if (mStepLines.IndexOf ((PLine)pLine) % 2 == 0) {
               PLine translatedLine = pLine.Orientation switch {
                  Horizontal => startPt.Y < centroid.Y ? (PLine)pLine.Translated (0, lineShift) : (PLine)pLine.Translated (0, -lineShift),
                  Vertical => startPt.X < centroid.X ? (PLine)pLine.Translated (lineShift, 0) : (PLine)pLine.Translated (-lineShift, 0),
                  _ => null!
               };
               mNewLines.Add (new (translatedLine.StartPoint, translatedLine.EndPoint, ++mIndex));
            } else {
               (PLine extrudedLine, PLine trimmedLine) = pLine.Orientation switch {
                  Horizontal => startPt.X < centroid.X ? ((PLine)pLine.Trimmed (startDx: -halfBA).Translated (0, halfBD), (PLine)pLine.Trimmed (startDx: lineShift, endDx: pLine.Length - halfBA))
                                                                     : ((PLine)pLine.Trimmed (startDx: halfBA).Translated (0, -halfBD), (PLine)pLine.Trimmed (startDx: -lineShift, endDx: -(pLine.Length - halfBA))),
                  Vertical => startPt.Y < centroid.Y ? ((PLine)pLine.Trimmed (startDy: -halfBA).Translated (-halfBD, 0), (PLine)pLine.Trimmed (startDy: lineShift, endDy: pLine.Length - halfBA))
                                                                   : ((PLine)pLine.Trimmed (startDy: halfBA).Translated (halfBD, 0), (PLine)pLine.Trimmed (startDy: -lineShift, endDy: -(pLine.Length - halfBA))),
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
                  Horizontal => startPt.Y < centroid.Y ? (PLine)pLine.Trimmed (startDx: -halfBD, endDx: -lineShift) : (PLine)pLine.Trimmed (startDx: halfBD, endDx: lineShift),
                  Vertical => startPt.X < centroid.X ? (PLine)pLine.Trimmed (startDy: halfBD, endDy: lineShift) : (PLine)pLine.Trimmed (startDy: -halfBD, endDy: -lineShift),
                  _ => null!
               };
               mNewLines.Add (new (trimmedEdge.StartPoint, trimmedEdge.EndPoint, ++mIndex));
            } else if (mStepStartPts.Contains (endPt)) {
               trimmedEdge = pLine.Orientation switch {
                  Horizontal => startPt.Y < centroid.Y ? (PLine)pLine.Trimmed (endDx: -lineShift) : (PLine)pLine.Trimmed (endDx: lineShift),
                  Vertical => startPt.X < centroid.X ? (PLine)pLine.Trimmed (endDy: lineShift) : (PLine)pLine.Trimmed (endDy: -lineShift),
                  _ => null!
               };
               mNewLines.Add (new (trimmedEdge.StartPoint, trimmedEdge.EndPoint, ++mIndex));
            } else if (mStepEndPts.Contains (startPt)) {
               trimmedEdge = pLine.Orientation switch {
                  Horizontal => startPt.Y < centroid.Y ? (PLine)pLine.Trimmed (startDx: -halfBD) : (PLine)pLine.Trimmed (startDx: halfBD),
                  Vertical => startPt.X < centroid.X ? (PLine)pLine.Trimmed (startDy: halfBD) : (PLine)pLine.Trimmed (startDy: -halfBD),
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
   List<Point2> mCcVertices, mStepStartPts, mStepEndPts;
   int mIndex;
   string mErrorMsg = "Cannot apply corner closing";
   #endregion
}
#endregion