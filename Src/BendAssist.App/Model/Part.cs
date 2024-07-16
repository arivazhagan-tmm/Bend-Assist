using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region class Part --------------------------------------------------------------------------------
public class Part {
   #region Constructors ---------------------------------------------
   public Part (List<PLine> plines, List<BendLine> bendLines, float thickness = 2f) {
      (PLines, BendLines, Thickness) = (plines, bendLines, thickness);
      BendLines = [.. bendLines.OrderBy (bl => bl.StartPoint.Y).ThenBy (bl => bl.StartPoint.X)];
      (Vertices, Hull, AssistInfo) = ([], [], []);
      PLines = [.. plines.OrderBy (l => l.Index)];
      PLines.ForEach (l => Vertices.Add (l.StartPoint));
      Hull = Vertices.ConvexHull ();
      Area = Hull.Area ();
      Centroid = Hull.Centroid ();
      Bound = new Bound2 (Hull);
      var blVertices = new List<Point2> ();
      BendLines.ForEach (l => blVertices.AddRange ([l.StartPoint, l.EndPoint]));
      Vertices.AddRange (blVertices);
      var lines = new List<Line> ();
      lines.AddRange (PLines);
      lines.AddRange (BendLines);
      var vertices = blVertices.Where (v => !Hull.Contains (v)).Distinct ();
      foreach (var v in vertices) {
         if (v.IsCommonVertex (lines, out var connectedLines) && connectedLines.Count > 2) {
            List<int> plIndices = [], blIndices = [];
            foreach (var cl in connectedLines)
               if (cl is BendLine) blIndices.Add (cl.Index); else plIndices.Add (cl.Index);
            var count = blIndices.Count;
            if (count is 1) AssistInfo.Add (new (v.Index, [.. plIndices], [.. blIndices], EBendAssist.BendRelief));
            else if (count is 2) {
               AssistInfo.Add (new (v.Index, [.. plIndices], [.. blIndices], EBendAssist.CornerClose));
               AssistInfo.Add (new (v.Index, [.. plIndices], [.. blIndices], EBendAssist.CornerRelief));
            }
         }
      }
   }
   #endregion

   #region Properties -----------------------------------------------
   public string? FilePath;
   public readonly double Area;
   public readonly double Thickness;
   public readonly Point2 Centroid;
   public readonly Bound2 Bound;
   public readonly List<Point2> Vertices;
   public readonly List<Point2> Hull;
   public readonly List<PLine> PLines;
   public readonly List<BendLine> BendLines;
   public readonly List<AssistInfo> AssistInfo;
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

#region struct AssistInfo -------------------------------------------------------------------------
/// <summary>A struct to store the information of the bend assist for the imported part.</summary>
/// Stores the indices of vertices, plines and bendlines which requires the bend assist
public readonly record struct AssistInfo (int Vertex, int[] PLIndices, int[] BLIndieces, EBendAssist ReqAssist);
#endregion