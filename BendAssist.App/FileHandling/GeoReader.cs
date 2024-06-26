using BendAssist.App.Model;
using System.IO;

namespace BendAssist.App.FileHandling;

#region class GeoReader ----------------------------------------------------------------------------
public class GeoReader (string fileName) {
   #region Method --------------------------------------------------
   /// <summary>Reads the given geo file</summary>
   /// and returns a part with list of plines and bendlines.
   public Part ParsePart () {
      var (vertices, pLines, bLines) = (new List<Point2> (), new List<PLine> (), new List<BendLine> ());
      using StreamReader reader = new (fileName);
      string materialType = string.Empty;
      float thickness = 0.0f, bDeduction = 0.0f, bAngle = 0.0f, bRadius = 0.0f;
      int index = 1;
      while (ReadLine (out string? str)) {
         switch (str) {
            case "#~11": // Gets material details
               for (int j = 0; j < 5; j++) SkipLine ();
               ReadLine (out str);
               materialType = str;
               ReadLine (out str);
               thickness = ParseInfo (str);
               break;
            case "#~31": // Gets vertices
               while (ReadLine (out str) && str is "P") {
                  SkipLine ();
                  ReadLine (out str);
                  var coords = str.Split (' ');
                  var (x, y) = (double.Parse (coords[0]), double.Parse (coords[1]));
                  vertices.Add (new Point2 (x, y, index++));
                  SkipLine ();
               }
               index = 1;
               break;
            case "#~331": // Creates new plines
               while (ReadLine (out str) && str is "LIN") {
                  SkipLine ();
                  var (v1, v2) = ParsePoints ();
                  pLines.Add (new PLine (v1, v2, index++));
                  SkipLine ();
               }
               index = 1;
               break;
            case "#~37": // Gets bendline info (Radius,Angle,Deduction)
               SkipLine ();
               ReadLine (out str);
               bAngle = ParseInfo (str.Split (' ')[0]);
               ReadLine (out str);
               bRadius = ParseInfo (str.Split (' ')[0]);
               ReadLine (out str);
               bDeduction = -1 * ParseInfo (str);
               break;

            case "#~371": // Creates new bendlines
               for (int j = 0; j < 2; j++) SkipLine ();
               var (p1, p2) = ParsePoints ();
               bLines.Add (new BendLine (p1, p2, index++, new BendLineInfo (bAngle, bRadius, bDeduction)));
               break;
         }
      }
      return new Part (pLines, bLines, thickness);

      bool ReadLine (out string str) { // Reads the str and return the trimmed str if it is not null
         str = reader.ReadLine ()!;
         if (str is null) return false;
         else {
            str = str.Trim ();
            return true;
         }
      }

      (Point2, Point2) ParsePoints () { // Parses points from string
         ReadLine (out string str);
         var coords = str.Split (" ");
         return (vertices[int.Parse (coords[0]) - 1], vertices[int.Parse (coords[1]) - 1]);
      }

      float ParseInfo (string value) => (float)Math.Round (double.Parse (value)); // Rounds off the parsed value

      void SkipLine () => reader.ReadLine (); // Skips a str
   }
   #endregion

   #region Properties ----------------------------------------------
   public string FileName => fileName; // Gets the name of the file
   #endregion
}
#endregion