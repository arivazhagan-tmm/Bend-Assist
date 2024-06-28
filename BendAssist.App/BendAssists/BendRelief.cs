using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
    #region Constructors --------------------------------------------
    public BendRelief (Part part) => (mPart, mPLines) = (part, part.PLines);
    #endregion

    #region Methods -------------------------------------------------
    /// <summary>Method to asses whether bend relief necessary for slected base and bend line</summary>
    public override bool Assisted () {
        if (mPart != null) {
            if (mPart.BendLines.Count != 0) {
                var temp = mPart.Vertices.Where (x => IsRepeated (mPart.Vertices, x)).Where (x => x.IsWithinBound (x, mPart.Bound)).ToList ();
                if (temp.Count > 0) {
                    mCanAssist = true;
                    mCommonVertices = temp[..(temp.Count / 2)];
                    mHLines = mPLines.Where (x=> x.Orientation == EOrientation.Horizontal).ToList ();
                    mVLines = mPLines.Where (x=> x.Orientation == EOrientation.Vertical).ToList ();
                }
            }
        }
        return mCanAssist;
    }

    /// <summary>Find whether selected point is repeated in given set of vertices</summary>
    bool IsRepeated (List<Point2> vertices, Point2 point) => vertices.Where (x => x == point).ToList ().Count > 1;

    /// <summary>Find distance between a line and a bend line</summary>
    double GetDistanceToLine (PLine line, BendLine bLine) =>
            bLine.Orientation == EOrientation.Horizontal ? Math.Abs (line.StartPoint.Y - bLine.StartPoint.Y)
                                 : Math.Abs (line.StartPoint.X - bLine.StartPoint.X);

    /// <summary>Generates new vector2 with given angle and displacement value</summary>
    Vector2 GetVector (double value, double angle) {
        angle = angle.ToRadians ();
        double dx1 = value * Math.Round (Math.Cos (angle));
        double dy1 = value * Math.Round (Math.Sin (angle));
        return new Vector2 (dx1, dy1);
    }

    /// <summary>Find a point of intersection for given line and a line drawn at given angle from other point</summary>
    Point2 FindIntersectPoint (PLine line, Point2 p, double angle) {
        Point2 p1 = p.Translate (new Vector2 (1 * (Math.Sin (angle.ToRadians ())), 1 * (Math.Cos (angle.ToRadians ()))));
        double slope1 = (p1.Y - p.Y) / (p1.X - p.X), slope2 = (line.EndPoint.Y - line.StartPoint.Y) / (line.EndPoint.X - line.StartPoint.X);
        double intercept1 = p.Y - slope1 * (p.X), intercept2 = line.StartPoint.Y - slope2 * (line.StartPoint.X);
        double commonX = intercept2 - intercept1 / slope1 - slope2;
        return (slope1, slope2) switch {
            (0, 0) => new Point2 (p.X, line.StartPoint.Y),
            (double.NegativeInfinity or double.PositiveInfinity, double.NegativeInfinity or double.PositiveInfinity) => new Point2 (line.StartPoint.X, p.Y),
            _ => new Point2 (commonX, slope1 * commonX + intercept1)
        };
    }

    /// <summary>Find the nearest parallel line to the bendline from the given list of lines</summary>
    PLine GetNearestParallelLine (List<PLine> lines, BendLine bLine) {
        List<PLine> p = [.. lines.OrderBy (line => GetDistanceToLine (line, bLine))];
        if (p.Count > 0)
            return p.First ();
        throw new ArgumentNullException (nameof (p));
    }
    #endregion

    #region Implementation ------------------------------------------
    /// <summary>Method which creates a new processed part after craeting bend relief</summary>
    public ProcessedPart ApplyBendRelief (Part part) {
        List<PLine> lines = mPLines;
        foreach (var vertex in mCommonVertices) {
            foreach (var bl in part.BendLines) {
                if (BendUtils.HasVertex (bl, vertex)) {
                    bool isHorizontal = bl.Orientation == EOrientation.Horizontal;
                    Point2 p1 = vertex, p2, p3, p4, defaultPt = new (0, 0);
                    PLine nearAlignedCurve = new (defaultPt, defaultPt);
                    PLine[] reliefCurves = new PLine[4];
                    (float angle, float radius, float deduction) = bl.BLInfo;
                    double brHeight = BendUtils.GetBendAllowance ((double)angle, 0.38, part.Thickness, radius) / 2;
                    double brWidth = part.Thickness / 2;
                    PLine? temp = GetNearestParallelLine (bl.Orientation == EOrientation.Horizontal ? mHLines : mVLines, bl);
                    if (temp != null) nearAlignedCurve = temp;
                    double Angle = 0, translateAngle1, translateAngle2;
                    int angleCode = (int)bl.Angle;
                    Angle = angleCode switch {
                        180 => 0,
                        270 => 90,
                        _ => bl.Angle,
                    };
                    if (isHorizontal) {
                        translateAngle1 = vertex.Y > part.Centroid.Y ? Angle + 270 : Angle + 90;
                        translateAngle2 = vertex.X < part.Centroid.X ? Angle + 180 : Angle;
                    } else {
                        translateAngle1 = vertex.X < part.Centroid.X ? Angle - 90 : Angle + 90;
                        translateAngle2 = vertex.Y < part.Centroid.Y ? Angle + 180 : Angle;
                    }
                    p2 = vertex.Translate (GetVector (brHeight, translateAngle1));
                    p3 = p2.Translate (GetVector (brWidth, translateAngle2));
                    p4 = FindIntersectPoint (nearAlignedCurve, p3, 180 - translateAngle1);
                    reliefCurves[0] = new PLine (p1, p2);
                    reliefCurves[1] = new (p2, p3);
                    reliefCurves[2] = new PLine (p3, p4);
                    reliefCurves[3] = new (p4, vertex == nearAlignedCurve.StartPoint ? nearAlignedCurve.EndPoint : nearAlignedCurve.StartPoint);
                    for (int i = 0; i < 4; i++)
                        lines.Add (reliefCurves[i]);
                    lines.Remove (nearAlignedCurve);
                }
            }
        }
        return new ProcessedPart (lines, part.BendLines, (float)part.Thickness, EBendAssist.BendRelief);
    }
    #endregion

    #region Private --------------------------------------------------
    readonly List<PLine> mPLines;
    List<Point2> mCommonVertices = [];
    List<PLine> mHLines = [];
    List<PLine> mVLines = [];
    #endregion
}
#endregion