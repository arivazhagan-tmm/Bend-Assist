using BendAssist.App.Model;
using static BendAssist.App.Model.EOrientation;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class CornerClose -------------------------------------------------------------------------
public sealed class CornerClose : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerClose (Part part) => (mPart, mIndex, mStepLines, mEdgeLines, mNewLines, mStepStartPts, mStepEndPts) = (part, -1, [], [], [], [], []);
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      return true;
   }

   /// <summary>Checks if a part is eligible for corner closing and gives the corner closed part</summary>
   public override void Execute () {
      base.Execute ();
      var vertices = mPart!.Vertices; var pLines = mPart.PLines;
      var commonVertices = vertices.GroupBy (p => p).Where (p => p.Count () > 2).Select (g => g.Key).ToList (); // To find the intersecting points of bendlines with stepcut 
      if (commonVertices.Count < 1) mProcessedPart = null;
      else {
         var halfBA = GetBendAllowance (90, 0.38, 2, 2) / 2;
         var lineShift = halfBA - 2;
         var halfBD = GetBendDeduction (90, 0.38, 2, 2) / 2;
         foreach (var pLine in pLines)
            if (commonVertices.Contains (pLine.StartPoint) || commonVertices.Contains (pLine.EndPoint)) mStepLines.Add (pLine);
            else mEdgeLines.Add (pLine);
         foreach (var stepLine in mStepLines) {
            mStepStartPts.Add (stepLine.StartPoint);
            mStepEndPts.Add (stepLine.EndPoint);
         }
         for (int i = 0; i < pLines.Count; i++) {
            var pLine = pLines[i];
            if (mEdgeLines.Contains (pLine)) {
               PLine trimmedEdge;
               if (mStepStartPts.Contains (pLine.EndPoint) && mStepEndPts.Contains (pLine.StartPoint)) {
                  trimmedEdge = pLine.Orientation switch {
                     Horizontal => pLine.StartPoint.Y < mPart.Centroid.Y ? (PLine)pLine.Trimmed (-halfBD, 0, -lineShift, 0) : (PLine)pLine.Trimmed (halfBD, 0, lineShift, 0),
                     Vertical => pLine.StartPoint.X < mPart.Centroid.X ? (PLine)pLine.Trimmed (0, halfBD, 0, lineShift) : (PLine)pLine.Trimmed (0, -halfBD, 0, -lineShift),
                     _ => throw new NotImplementedException ()
                  };
                  trimmedEdge.InsertAt (++mIndex, mNewLines);
               } else if (mStepStartPts.Contains (pLine.EndPoint)) {
                  trimmedEdge = pLine.Orientation switch {
                     Horizontal => pLine.StartPoint.Y < mPart.Centroid.Y ? (PLine)pLine.Trimmed (0, 0, -lineShift, 0) : (PLine)pLine.Trimmed (0, 0, lineShift, 0),
                     Vertical => pLine.StartPoint.X < mPart.Centroid.X ? (PLine)pLine.Trimmed (0, 0, 0, lineShift) : (PLine)pLine.Trimmed (0, 0, 0, -lineShift),
                     _ => throw new NotImplementedException ()
                  };
                  trimmedEdge.InsertAt (++mIndex, mNewLines);
               } else if (mStepEndPts.Contains (pLine.StartPoint)) {
                  trimmedEdge = pLine.Orientation switch {
                     Horizontal => pLine.StartPoint.Y < mPart.Centroid.Y ? (PLine)pLine.Trimmed (-halfBD, 0, 0, 0) : (PLine)pLine.Trimmed (halfBD, 0, 0, 0),
                     Vertical => pLine.StartPoint.X < mPart.Centroid.X ? (PLine)pLine.Trimmed (0, halfBD, 0, 0) : (PLine)pLine.Trimmed (0, -halfBD, 0, 0),
                     _ => throw new NotImplementedException ()
                  };
                  trimmedEdge.InsertAt (++mIndex, mNewLines);
               } else pLine.InsertAt (++mIndex, mNewLines);
            } else {
               if (mStepLines.IndexOf (pLine) % 2 == 0) {
                  PLine translatedLine = pLine.Orientation switch {
                     Horizontal => pLine.EndPoint.Y < mPart.Centroid.Y ? (PLine)pLine.Translated (0, lineShift) : (PLine)pLine.Translated (0, -lineShift),
                     Vertical => pLine.EndPoint.X < mPart.Centroid.X ? (PLine)pLine.Translated (lineShift, 0) : (PLine)pLine.Translated (-lineShift, 0),
                     _ => throw new NotImplementedException ()
                  };
                  translatedLine.InsertAt (++mIndex, mNewLines);
               } else {
                  (PLine extrudedLine, PLine trimmedLine) = pLine.Orientation switch {
                     Horizontal => pLine.StartPoint.X < mPart.Centroid.X ? ((PLine)pLine.Trimmed (-halfBA, 0, 0, 0).Translated (0, halfBD), (PLine)pLine.Trimmed (lineShift, 0, pLine.Length - halfBA, 0))
                                                                        : ((PLine)pLine.Trimmed (halfBA, 0, 0, 0).Translated (0, -halfBD), (PLine)pLine.Trimmed (-lineShift, 0, -(pLine.Length - halfBA), 0)),
                     Vertical => pLine.StartPoint.Y < mPart.Centroid.Y ? ((PLine)pLine.Trimmed (0, -halfBA, 0, 0).Translated (-halfBD, 0), (PLine)pLine.Trimmed (0, lineShift, 0, pLine.Length - halfBA))
                                                                      : ((PLine)pLine.Trimmed (0, halfBA, 0, 0).Translated (halfBD, 0), (PLine)pLine.Trimmed (0, -lineShift, 0, -(pLine.Length - halfBA))),
                     _ => throw new NotImplementedException ()
                  };
                  trimmedLine.InsertAt (++mIndex, mNewLines);
                  mNewLines.Add (new PLine (trimmedLine.EndPoint, extrudedLine.StartPoint, ++mIndex));
                  extrudedLine.InsertAt (++mIndex, mNewLines);
               }
            }
         }
         mProcessedPart = new ProcessedPart (mNewLines, mPart.BendLines, 2, EBendAssist.CornerClose);
      }
   }
   #endregion

   // This region is to be implemented in Utils.cs
   #region UtilsImplementation -------------------------------------------
   public static double GetBendDeduction (double angle, double kFactor, double thickness, double radius) {
      angle = angle.ToRadians ();
      var totalSetBack = 2 * ((radius + thickness) * Math.Tan (angle / 2));
      var bendAllowance = angle * (kFactor * thickness + radius);
      return double.Round (Math.Abs (totalSetBack - bendAllowance), 3);
   }

   public static double GetBendAllowance (double angle, double kFactor, double thickness, double radius)
      => angle.ToRadians () * (kFactor * thickness + radius);

   #endregion

   #region Private Data ---------------------------------------------
   List<PLine> mStepLines, mEdgeLines, mNewLines;
   List<Point2> mStepStartPts, mStepEndPts;
   int mIndex;
   #endregion
}
#endregion