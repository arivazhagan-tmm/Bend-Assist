using BendAssist.App.Model;
using System.IO;

namespace BendAssist.App.FileHandling;

#region class GeoWriter ---------------------------------------------------------------------------
public class GeoWriter {
   #region Constructor ---------------------------------------------
   /// <summary>Gets the processed part and imported file</summary>
   public GeoWriter (Part part, string fileName) => (mPart, mFileName) = (part, fileName);
   #endregion

   #region Methods -------------------------------------------------
   /// <summary>Writes a new geo file with new vertices</summary>
   /// copies the lines from the imported file which are not
   /// changed by the modification done on the part.
   public void WriteToGeo (string fileName) {    // Gets the file name where it has to be exported
      bool skipLines = true;  // Set to true if the line in the imported file has to be skipped
      var (bound, centroid, area, pts) = (mPart.Bound, mPart.Centroid, mPart.Area, new Dictionary<string, int> ()); // pts stores the points with their indices
      var lines = File.ReadAllLines (mFileName!).ToList (); // Reads the imported file
      using StreamWriter writer = new (fileName);
      foreach (string line in lines) {
         switch (line.Trim ()) {
            case "#~1":
               writer.Write ("#~1{0}1.03{0}1{0}{1}{0}", "\r\n", $"{DateTime.Now:dd.MM.yyyy}");
               OutDoubles (bound.MinX, bound.MinY, 0); OutDoubles (bound.MaxX, bound.MaxY, 0);
               writer.WriteLine ("{1:F3}{0}1{0}0.001{0}0{0}1{0}##~~", "\r\n", area);
               skipLines = true;
               break;
            case "#~3":
               writer.WriteLine ("#~3{0}{0}{0}", "\r\n");
               OutDoubles (0, 0, 1); OutDoubles (1, 0, 0, 0); OutDoubles (0, 1, 0, 0); OutDoubles (0, 0, 1, 0); OutDoubles (0, 0, 0, 1);
               OutDoubles (bound.MinX, bound.MinY, 0); OutDoubles (bound.MaxX, bound.MaxY, 0);
               OutDoubles (centroid.X, centroid.Y, 0); OutDoubles (area);
               writer.WriteLine ("1{0}0{0}0{0}0{0}0{0}##~~", "\r\n");
               skipLines = true;
               break;
            case "#~31": // Writes the new vertices
               writer.WriteLine ("#~31");
               var vertices = mPart.Vertices.Distinct ().ToList ();
               for (int i = 0; i < vertices.Count; i++) {
                  var pt = vertices[i];
                  pts.Add (pt.ToString (), i + 1);
                  writer.WriteLine ("P\r\n{0}", i + 1);
                  OutDoubles (pt.X, pt.Y, 0);
                  writer.WriteLine ("|~");
               }
               writer.WriteLine ("##~~");
               skipLines = true;
               break;
            case "#~33":
               // #~33 section - outputs pline info
               writer.WriteLine ("#~33");
               var idx = lines.IndexOf (line) + 1;    // Gets the index of the line from which to be copied
               var dataLines = lines.Skip (idx).Take (4).ToArray ();  // Copies the info about the contour from the imported file
               foreach (var dataline in dataLines) writer.WriteLine (dataline);
               OutDoubles (bound.MinX, bound.MinY, 0); OutDoubles (bound.MaxX, bound.MaxY, 0);
               OutDoubles (centroid.X, centroid.Y, 0); OutDoubles (area);
               writer.WriteLine ("0{0}##~~{0}#~331", "\r\n");
               foreach (var curve in mPart.PLines)
                  writer.WriteLine ("LIN{0}1 0{0}{1} {2}{0}|~", "\r\n", pts[curve.StartPoint.ToString ()], pts[curve.EndPoint.ToString ()]);
               writer.WriteLine ("##~~\n#~KONT_END");
               // #~37 section - outputs bend-line info
               foreach (var bl in mPart.BendLines) {
                  var (angle, radius, deduction) = (bl.BLInfo.Angle, bl.BLInfo.Radius, -1 * bl.BLInfo.Deduction);
                  writer.WriteLine ("#~37{0}0 0 0", "\r\n");
                  OutDoubles (angle, 0); OutDoubles (radius, radius); OutDoubles (deduction);
                  writer.WriteLine ("{0}{0}##~~{0}#~371{0}LIN{0}4 {1}{0}{2} {3}{0}|~{0}##~~{0}#~BIEG_END", "\r\n", angle < 0 ? 1 : 0,
                                    pts[bl.StartPoint.ToString ()], pts[bl.EndPoint.ToString ()]);
               }
               skipLines = true;
               break;
            case "#~11" or "#~30" or "#~END" or "#~EOF": writer.WriteLine (line); skipLines = false; break;
            default: if (!skipLines) writer.WriteLine (line); break; // Copies the lines from imported file which are not affected
         }
      }
      // Converts the given double to string in F9 format and writes to the file
      void OutDoubles (params double[] vals) {
         for (int i = 0; i < vals.Length; i++) {
            var s = vals[i].ToString ("F9");
            if (i > 0) s = " " + s;
            writer.Write (s);
         }
         writer.Write ("\r\n");
      }
   }
   #endregion

   #region Private Data --------------------------------------------
   readonly Part mPart;       // The processed part
   readonly string? mFileName;    // Imported file
   #endregion
}
#endregion

