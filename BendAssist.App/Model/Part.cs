using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region class Part --------------------------------------------------------------------------------
public class Part {
   #region Constructors ---------------------------------------------
   public Part (List<PLine> plines, List<BendLine> bendLines, float thickeness = 0.0f) {
      (PLines, BendLines) = (plines, bendLines);
      Vertices = [];
      PLines.ForEach (l => Vertices.Add (l.StartPoint));
      BendLines.ForEach (l => Vertices.Add (l.StartPoint));
      Area = Vertices.Area ();
      Centroid = Vertices.Centroid ();
      Bound = new Bound2 (Vertices);
   }
   #endregion

   #region Methods --------------------------------------------------
   public Part ReBuild () {
      List<PLine> plines = []; List<BendLine> blines = [];
      var index = 0;
      foreach (var l in PLines.OrderBy (l => l.Index))
         plines.Add (new (l.StartPoint.Duplicate (index++), l.EndPoint.Duplicate (index++), l.Index));
      index = 0;
      foreach (var l in BendLines.OrderBy (l => l.Index))
         blines.Add (new (l.StartPoint.Duplicate (index++), l.EndPoint.Duplicate (index++), l.Index, l.BLInfo));
      return new Part (plines, blines);
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly double Area;
   public readonly Point2 Centroid;
   public readonly Bound2 Bound;
   public readonly List<Point2> Vertices;
   public readonly List<PLine> PLines;
   public readonly List<BendLine> BendLines;
   #endregion
}
#endregion

#region class ProcessedPart -----------------------------------------------------------------------
public sealed class ProcessedPart (List<PLine> plines, List<BendLine> bendLines, float materialThickness, EBendAssist asst) : Part (plines, bendLines, materialThickness) {
   #region Properties -----------------------------------------------
   public readonly EBendAssist AppliedAssist = asst;
   #endregion
}
#endregion