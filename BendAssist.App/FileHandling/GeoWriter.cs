using BendAssist.App.Model;
using System.IO;

namespace BendAssist.App.FileHandling;

#region class GeoWriter ---------------------------------------------------------------------------
public class GeoWriter {
    #region Constructor ---------------------------------------------
    /// <summary>Gets the processed part</summary>
    public GeoWriter (Part part) {
        mPart = part.ReBuild ();
        mBoundMin = $"{mPart.Bound.MinX:F9} {mPart.Bound.MinY:F9} 0.000000000";
        mBoundMax = $"{mPart.Bound.MaxX:F9} {mPart.Bound.MaxY:F9} 0.000000000";
        mCentroid = $"{mPart.Centroid.X:F9} {mPart.Centroid.Y:F9} 0.000000000";
        mArea = $"{mPart.Area:F3}";
    }
    #endregion

    #region Methods -------------------------------------------------
    /// <summary>Writes a new geo file with new vertices</summary>
    /// copies the lines from the imported file which are not
    /// changed by the modification done on the part.
    public void Save (string fileName) {    // Gets the file name where it has to be exported
        var lines = File.ReadAllLines (mPart.FilePath!).ToList (); // Reads the imported file
        using StreamWriter writer = new (fileName);
        foreach (string line in lines) {
            switch (line.Trim ()) {
                case "#~1":
                    writer.WriteLine ("#~1{0}1.03{0}1{0}{1}{0}{2}{0}{3}{0}{4}{0}1{0}0.001{0}0{0}1{0}##~~", "\r\n",
                        $"{DateTime.Now:dd.MM.yyyy}", mBoundMin, mBoundMax, mArea);
                    mIsLineNeeded = false;
                    break;
                case "#~3":
                    writer.WriteLine ("#~3{0}{0}{0}", "\r\n" + $"{0:F9} {0:F9} {1:F9}\r\n{1:F9} {0:F9} {0:F9} {0:F9}\r\n" +
                        $"{0:F9} {1:F9} {0:F9} {0:F9}\r\n{0:F9} {0:F9} {1:F9} {0:F9}\r\n{0:F9} {0:F9} {0:F9} {1:F9}" +
                        "{1}{0}{2}{0}{3}{0}{4}{0}1{0}0{0}0{0}0{0}0{0}##~~", "\r\n", mBoundMin, mBoundMax, mCentroid, mArea);
                    mIsLineNeeded = false;
                    break;
                case "#~31": // Writes the new vertices
                    writer.WriteLine ("#~31");
                    foreach (var vertex in mPart.Vertices)
                        writer.WriteLine ("P{0}{1}{0}{2} {3}{0}{4}", "\r\n", vertex.Index, $"{vertex.X:F9}", $"{vertex.Y:F9} 0.000000000", "|~");
                    writer.WriteLine ("##~~");
                    mIsLineNeeded = false;
                    break;
                case "#~33":
                    writer.WriteLine ("#~33");
                    var idx = lines.IndexOf (line) + 1;    // Gets the index of the line from which to be copied
                    var dataLines = lines.Skip (idx).Take (4).ToArray ();  // Copies the info about the contour from the imported file
                    foreach (var dataline in dataLines) writer.WriteLine (dataline);
                    writer.WriteLine ("{1}{0}{2}{0}{3}{0}{4}{0}0{0}##~~", "\r\n", mBoundMin, mBoundMax, mCentroid, mArea);
                    mIsLineNeeded = false;
                    break;
                case "#~11" or "#~30" or "#~37" or "#~331" or "#~371" or "#~END" or "#~EOF":
                    writer.WriteLine (line);
                    if (line.Trim () == "#~331") {    // Writes the vertices of contour for mapping them
                        foreach (var curve in mPart.PLines)
                            writer.WriteLine ("LIN{0}1 0{0}{1} {2}{0}|~", "\r\n", curve.StartPoint.Index, curve.EndPoint.Index);
                        writer.WriteLine ("##~~\n#~KONT_END");
                        mIsLineNeeded = false;
                    } else if (line.Trim () == "#~371") {    // Writes the vertices of bendlines for mapping them
                        int totalCount = mPart.BendLines.Count;
                        if (mBLCount < totalCount) {
                            var startPtIdx = mPart.BendLines[mBLCount].StartPoint.Index;               // #~371 section is written after each bendline's
                            var endPtIdx = mPart.BendLines[mBLCount].EndPoint.Index;                   // #~37 section which holds info about that bendline
                            writer.WriteLine ("LIN{0}4 0{0}{1} {2}{0}|~", "\r\n", startPtIdx, endPtIdx);
                        }
                        mBLCount++;
                        mIsLineNeeded = false;
                        writer.WriteLine ("##~~\n#~BIEG_END");
                    } else mIsLineNeeded = true;
                    break;
                default: if (mIsLineNeeded) writer.WriteLine (line); break; // Copies the lines from imported file which are not affected
            }
        }
    }
    #endregion

    #region Private Data --------------------------------------------
    int mBLCount;    // Holds the count of the bendlines written
    bool mIsLineNeeded;  // Set to false if the line in the imported file has to be skipped
    readonly Part mPart;       // The processed part
    readonly string mBoundMin, mBoundMax, mCentroid, mArea;
    #endregion
}
#endregion

