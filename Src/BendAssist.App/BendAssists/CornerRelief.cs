using BendAssist.App.Model;
using BendAssist.App.Utils;
using System;

namespace BendAssist.App.BendAssists;

#region class CornerRelief ------------------------------------------------------------------------
public sealed class CornerRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerRelief (Part part) => (Part, mBendAllowance) = (part, 4.335);
   #endregion

   #region Methods --------------------------------------------------
   public override void Execute () {
      if (mPart is null) return;
      List<PLine> initialPlines = mPart.PLines; // An initial stage of the plines of the part is present in it.
      Point2 p1 = new (), p2 = new (), p3 = new ();
      var halfBA = mBendAllowance * 0.5;
      // To return the new processed part when the part has one or more corner relief options.
      bool assisted = false;
      foreach (var info in mPart.AssistInfo) {
         if (info.ReqAssist != EBendAssist.CornerRelief) continue;
         assisted = true;
         var vertex = mPart.Vertices.Find (v => v.Index == info.VIndex);
         var quadrant = vertex.Quadrant (mPart.Centroid);
         var (sDx, sDy, eDx, eDy) = (0.0, 0.0, 0.0, 0.0);
         // To find the corner relief vertices by translate vertex using following vectors.
         var (v1, v2, v3) = (new Vector2 (), new Vector2 (), new Vector2 ());
         switch (quadrant) {
            case IQuadrant.I or IQuadrant.III:
               (v1, v2, v3) = (new (halfBA, 0), new (-halfBA, -halfBA), new (0, halfBA));
               sDy = eDx = quadrant is IQuadrant.I ? halfBA : -halfBA;
               break;
            case IQuadrant.II or IQuadrant.IV:
               (v1, v2, v3) = (new (0, halfBA), new (halfBA, -halfBA), new (-halfBA, 0));
               (sDx, eDy) = quadrant is IQuadrant.II ? (-halfBA, halfBA) : (halfBA, -halfBA);
               break;
            default: break;
         }
         if (quadrant is IQuadrant.III or IQuadrant.IV) (v1, v2, v3) = (v1 * -1.0, v2 * -1.0, v3 * -1.0);
         (p1, p2, p3) = (vertex + v1, vertex + v2, vertex + v3);
         int index1 = info.PLIndices.First (), index2 = info.PLIndices.Last ();
         var plines = mPart.PLines.Where (p => p.Index == index1 || p.Index == index2);
         var pl1 = (PLine)plines.First ().Trimmed (endDx: eDx, endDy: eDy);
         var pl2 = (PLine)plines.Last ().Trimmed (startDx: sDx, startDy: sDy);
         var pl3 = new PLine (p1, p2, index1);
         var pl4 = new PLine (p2, p3, index2);
         initialPlines = initialPlines.Where (p => p.Index != index1 && p.Index != index2).ToList ();
         initialPlines.InsertRange (index1 - 1, [pl1, pl2, pl3, pl4]);
      }
      if (assisted)
         mProcessedPart = new ProcessedPart (initialPlines, mPart.BendLines, 2f, EBendAssist.CornerRelief);
   }
   #endregion

   #region Private Data ---------------------------------------------
   readonly double mBendAllowance; // Bend Allowance value.(Predefined material 1.0038 with a bend radius value is 2).
   #endregion
}
#endregion

public enum IQuadrant { None, I, II, III, IV }