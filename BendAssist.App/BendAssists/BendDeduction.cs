using BendAssist.App.Model;
using BendAssist.App.Utils;
using static BendAssist.App.Model.EOrientation;
using static BendAssist.App.Model.EPCoord;

namespace BendAssist.App.BendAssists;

#region class BendDeduction -----------------------------------------------------------------------
public sealed class BendDeduction : BendAssist {
   #region Constructors ---------------------------------------------
   public BendDeduction (Part part, EBDAlgorithm algorithm) => (mPart, mAlgorithm) = (part, algorithm);
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      if (mPart is null) return false;
      Execute ();
      return true;
   }

   /// <summary>Applies bend deduction for the bend lines and the edges</summary>
   public override void Execute () {
      var totalBD = 0.0;
      int verBLCount = 0, horBLCount = 0;
      List<BendLine> newBendLines = []; List<PLine> newPLines = [];
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) { // Handles only horizontal bend lines
         var tmp = mPart?.BendLines.Select (bl => (BendLine)bl.Clone ()).Reverse ().ToList ();
         newBendLines.AddRange (GetTranslatedBLines (tmp!, out totalBD, out horBLCount, out verBLCount));
         var centroidY = mPart!.Centroid.Y;
         foreach (var pLine in mPart.PLines) {
            var newPLine = pLine.StartPoint.Y < centroidY && pLine.EndPoint.Y < centroidY ? pLine.Translated (0, totalBD)
                                                                                          : pLine;
            if (pLine.Orientation is Vertical) {
               if (newPLine.StartPoint.Y < newPLine.EndPoint.Y) newPLine = newPLine.Trimmed (0, totalBD, 0, 0);
               else newPLine = newPLine.Trimmed (0, 0, 0, totalBD);
            }
            newPLines.Add ((PLine)newPLine);
         }
      } else { // Equally distributed algorithm - Handles horizontal and vertical bend lines
         var bendLines = mPart?.BendLines;
         var blCount = bendLines!.Count;
         // The area between the two innermost bend lines is considered the base.
         var bottomBLines = bendLines.Take (blCount / 2).Reverse ().ToList (); // Bend lines on the bottom and left side of the base
         var topBLines = bendLines.TakeLast (blCount - bottomBLines.Count).Reverse ().ToList (); // Bend lines on the top and right side of the base
         var tempPLines = new List<PLine> ();
         // Applies bend deduction from the top and right side of the part
         ApplyEqDistributedBD (ref newBendLines, topBLines, ref tempPLines, ref newPLines, ELoc.Top);
         if (bottomBLines.Count == 0) {  // Incase of a single bend line part
            foreach (var pLine in tempPLines) newPLines.Add (pLine);
            if (mPart != null) newPLines.Add (mPart.PLines.Except (tempPLines).First ());
         }
         // Applies bend deduction from the bottom and left side of the part
         ApplyEqDistributedBD (ref newBendLines, bottomBLines, ref tempPLines, ref newPLines, ELoc.Bottom);
      }
      mProcessedPart = new ProcessedPart (newPLines, newBendLines, 0, EBendAssist.BendDeduction);
   }
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Applies bend deduction for the bend lines and the edges equally on both sides of the bend lines</summary>
   // newBLines - Collection of bend deducted bend lines, bLines - Bend lines which are to be bend deducted.
   // tempPLines - Temporary collection of perpendicular pLines which have to be trimmed from the other side.
   // newPLines - Collection of bend deducted pLines, blLoc - Bend Line location.
   void ApplyEqDistributedBD (ref List<BendLine> newBLines, List<BendLine> bLines, ref List<PLine> tempPLines, ref List<PLine> newPLines, ELoc blLoc) {
      if (bLines.Count == 0 || mPart is null) return;
      // Bend Deduction on bend lines
      newBLines.AddRange (GetTranslatedBLines (bLines, out double totalBD, out int horBLCount, out int verBLCount, isNegOff: blLoc is ELoc.Top));
      // Bend Deduction on edges
      foreach (var pLine in GetAlignedPLines (mPart, blLoc, verBLCount > 0, horBLCount > 0)) {
         var pLOrient = pLine.Orientation;
         var (dx, dy) = blLoc is ELoc.Top ? pLOrient is Horizontal ? (0.0, -totalBD) : (-totalBD, 0)
                                          : pLOrient is Horizontal ? (0.0, totalBD) : (totalBD, 0);
         newPLines.Add ((PLine)pLine.Translated (dx, dy)); // Parallel pLines
         switch (blLoc) {
            case ELoc.Top:
               foreach (var idx in BendUtils.GetCPIndices (pLine, mPart.PLines)) { // Trims the connected perpendicular edges
                  var conPLine = mPart.PLines.Where (cPLine => cPLine.Index == idx).First ();
                  tempPLines.Add ((PLine)TrimLine (pLOrient is Horizontal ? Y : X, conPLine, pLine, totalBD, true));
               }
               break;
            case ELoc.Bottom:
               foreach (var idx in BendUtils.GetCPIndices (pLine, mPart.PLines)) {
                  PLine conPLine;
                  if (tempPLines.Any (tPLine => tPLine.Index == idx)) // checks whether the edge is trimmed from the other side or not
                     conPLine = tempPLines.Where (tPLine => tPLine.Index == idx).First ();
                  else {
                     conPLine = mPart.PLines.Where (newPLine => newPLine.Index == idx).First (); // in case of new edges to be trimmed
                     foreach (var tPLine in tempPLines) newPLines.Add (tPLine);
                  }
                  newPLines.Add ((PLine)TrimLine (pLOrient is Horizontal ? Y : X, conPLine, pLine, totalBD));
               }
               break;
         }
      }
   }

   /// <summary>Returns the list of bend lines after translating them</summary>
   // Each bendLine is moved towards the base by a sum of half of its bend deduction and 
   // the total of the bend deduction of all the inner bendLines
   List<BendLine> GetTranslatedBLines (List<BendLine> bLines, out double totalBD, out int hBLCount, out int vBLCount, bool isNegOff = false) {
      var newBendLines = new List<BendLine> ();
      var offFactor = isNegOff ? -1 : 1;
      totalBD = 0.0;
      hBLCount = vBLCount = 0;
      foreach (var bl in bLines) {
         var (bd, orient) = (bl.BLInfo.Deduction, bl.Orientation);
         var offset = offFactor * (totalBD + 0.5 * bd);
         (double dx, double dy) = (0.0, 0.0);
         if (orient is Horizontal) (hBLCount, dy) = (hBLCount + 1, offset);
         else if (orient is Vertical) (dx, vBLCount) = (offset, vBLCount + 1);
         totalBD += bd;
         newBendLines.Add ((BendLine)bl.Translated (dx, dy));
      }
      return newBendLines;
   }

   /// <summary>Returns the edges parallel to the bend lines</summary>
   List<PLine> GetAlignedPLines (Part part, ELoc loc, bool hasVBLine, bool hasHBLine) {
      var b = part.Bound;
      double bMaxX = b.MaxX, bMaxY = b.MaxY, bMinX = b.MinX, bMinY = b.MinY;
      List<PLine> alignedPLines = [];
      if (hasVBLine) { // Handles vertical bend lines and returns nearest vertical pLine 
         alignedPLines.Add (part.PLines.Where (c => loc is ELoc.Top ? CommonUtils.IsEqual (c.StartPoint.X, bMaxX) &&
                                                                      CommonUtils.IsEqual (c.EndPoint.X, bMaxX)
                                                                    : CommonUtils.IsEqual (c.StartPoint.X, bMinX) &&
                                                                      CommonUtils.IsEqual (c.EndPoint.X, bMinX)).First ());
      }

      if (hasHBLine) { // Handles horizontal bend lines and returns nearest horizontal pLine 
         alignedPLines.Insert (0, part.PLines.Where (c => loc is ELoc.Top ? CommonUtils.IsEqual (c.StartPoint.Y, bMaxY) &&
                                                                            CommonUtils.IsEqual (c.EndPoint.Y, bMaxY)
                                                                          : CommonUtils.IsEqual (c.StartPoint.Y, bMinY) &&
                                                                            CommonUtils.IsEqual (c.EndPoint.Y, bMinY)).First ());
      }
      return alignedPLines;
   }

   /// <summary>Trims the given line according to the parameters given and returns the trimmed line</summary>
   Line TrimLine (EPCoord coOrdinate, Line trimLine, Line refLine, double offset, bool lessThan = false) {
      return coOrdinate switch {
         X => lessThan == true ? trimLine.StartPoint.X < refLine.StartPoint.X ? trimLine.Trimmed (0, 0, -offset, 0)
                                                                              : trimLine.Trimmed (-offset, 0, 0, 0)
                               : trimLine.StartPoint.X > refLine.StartPoint.X ? trimLine.Trimmed (0, 0, offset, 0)
                                                                              : trimLine.Trimmed (offset, 0, 0, 0),
         _ => lessThan == true ? trimLine.StartPoint.Y < refLine.StartPoint.Y ? trimLine.Trimmed (0, 0, 0, -offset)
                                                                              : trimLine.Trimmed (0, -offset, 0, 0)
                               : trimLine.StartPoint.Y > refLine.StartPoint.Y ? trimLine.Trimmed (0, 0, 0, offset)
                                                                              : trimLine.Trimmed (0, offset, 0, 0)
      };
   }
   #endregion

   #region Private Data ---------------------------------------------
   readonly EBDAlgorithm mAlgorithm; // Algorithm by which bend deduction is applied
   #endregion
}
#endregion