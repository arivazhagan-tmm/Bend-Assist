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
      if (mPart is null || !CheckBendLine ()) return;
      List<PLine> orgPlines = mPart.PLines;
      Point2 p1 = new (), p2 = new (), p3 = new ();
      foreach (var info in mValidCR) {
         var vertex = mPart.Vertices.Find (v => v.Index == info.Key);
         var (vX, vY) = (vertex.X, vertex.Y);
         int fv = info.Value.First (), lv = info.Value.Last ();
         var plines = mPart.PLines.Where (p => p.Index == fv || p.Index == lv);
         var quadrant = vertex.Quadrant (mPart.Centroid);
         var halfBA = mBendAllowance * 0.5;
         var (pl1, pl2) = (plines.First (), plines.Last ());
         switch (quadrant) {
            case IQuadrant.I:
               (p1, p2, p3) = (new (vX + halfBA, vY), new (vX - halfBA, vY - halfBA), new (vX, vY + halfBA));
               (pl1, pl2) = ((PLine)pl1.Trimmed (endDx: halfBA), (PLine)pl2.Trimmed (startDy: halfBA));
               break;
            case IQuadrant.II:
               (p1, p2, p3) = (new (vX, vY + halfBA), new (vX + halfBA, vY - halfBA), new (vX - halfBA, vY));
               (pl1, pl2) = ((PLine)pl1.Trimmed (endDy: halfBA), (PLine)pl2.Trimmed (startDx: -halfBA));
               break;
            case IQuadrant.III:
               (p1, p2, p3) = (new (vX - halfBA, vY), new (vX + halfBA, vY + halfBA), new (vX, vY - halfBA));
               (pl1, pl2) = ((PLine)pl1.Trimmed (endDx: -halfBA), (PLine)pl2.Trimmed (startDy: -halfBA));
               break;
            case IQuadrant.IV:
               (p1, p2, p3) = (new (vX, vY - halfBA), new (vX - halfBA, vY + halfBA), new (vX + halfBA, vY));
               (pl1, pl2) = ((PLine)pl1.Trimmed (endDy: -halfBA), (PLine)pl2.Trimmed (startDx: halfBA));
               break;
            default: break;
         }
         orgPlines = UpdatePlines (orgPlines, p1, p2, p3, pl1.Index, pl2.Index, pl1, pl2);
      }
      mProcessedPart = new ProcessedPart (orgPlines, mPart.BendLines, 2f, EBendAssist.CornerRelief);
   }
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Get a new list of plines for corner relief.</summary>
   List<PLine> UpdatePlines (List<PLine> plines, Point2 p1, Point2 p2, Point2 p3, int index1,
      int index2, PLine pline1, PLine pline2) {
      PLine newpline1 = new (p1, p2, index1), newpline2 = new (p2, p3, index2);
      plines = plines.Where (x => x.Index != index1 && x.Index != index2).ToList ();
      plines.InsertRange (index1 - 1, [pline1, newpline1, newpline2, pline2]);
      return plines;
   }

   /// <summary>Check all the bend lines orientation in the part.</summary>
   bool CheckBendLine () {
      foreach (var info in mPart!.AssistInfo) {
         if (info.ReqAssist != EBendAssist.CornerRelief) continue;
         var bl1 = mPart.BendLines.First (x => x.Index == info.BLIndieces[0]);
         var bl2 = mPart.BendLines.First (x => x.Index == info.BLIndieces[1]);
         if ((bl1!.Orientation is EOrientation.Horizontal or EOrientation.Vertical)
            && (bl2!.Orientation is EOrientation.Horizontal or EOrientation.Vertical))
            mValidCR.Add (info.Vertex, info.PLIndices);
      }
      return mValidCR.Count > 0;
   }
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mBendAllowance; // Bend Allowance value.(Predefined material 1.0038 with a bend radius value is 2).
   readonly Dictionary<int, int[]> mValidCR = [];// It contains vertex index and their respective list of plines.
   #endregion
}
#endregion

public enum IQuadrant { None, I, II, III, IV }