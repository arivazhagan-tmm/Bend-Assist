﻿using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region class Part --------------------------------------------------------------------------------
public class Part {
   #region Constructors ---------------------------------------------
   public Part (List<PLine> plines, List<BendLine> bendLines, float thickeness = 0.0f) {
      (PLines, BendLines) = (plines, bendLines);
      BendLines = bendLines.OrderBy (bl => bl.StartPoint.Y).ThenBy (bl => bl.StartPoint.X).ToList ();
      Vertices = [];
      PLines.ForEach (l => Vertices.Add (l.StartPoint));
      BendLines.ForEach (l => Vertices.AddRange ([l.StartPoint, l.EndPoint]));
      Area = Vertices.Area ();
      Centroid = Vertices.Centroid ();
      Bound = new Bound2 (Vertices);
      Thickness = thickeness;
   }
   #endregion

   #region Methods --------------------------------------------------
   public Part ReBuild () {
      List<PLine> plines = []; List<BendLine> blines = [];
      var index = 1;
      foreach (var l in PLines.OrderBy (l => l.Index))
         plines.Add (new (l.StartPoint.Duplicate (index), l.EndPoint.Duplicate (index++), l.Index));
      foreach (var l in BendLines.OrderBy (l => l.Index))
         blines.Add (new (l.StartPoint.Duplicate (index++), l.EndPoint.Duplicate (index++), l.Index, l.BLInfo));
      return new Part (plines, blines);
   }
   #endregion

   #region Properties -----------------------------------------------
   public string? FilePath;
   public readonly double Area;
   public readonly Point2 Centroid;
   public readonly Bound2 Bound;
   public readonly List<Point2> Vertices;
   public readonly List<PLine> PLines;
   public readonly List<BendLine> BendLines;
   public readonly double Thickness;
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