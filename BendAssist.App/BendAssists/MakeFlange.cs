using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class MakeFlange --------------------------------------------------------------------------
public sealed class MakeFlange : BendAssist {
    #region Constructors --------------------------------------------
    public MakeFlange (Part part, PLine pline, float angle, double height, float radius) =>
        (mPart, mPline, mBendAngle, mHeight, mRadius) = (part, pline, angle, height, radius);
    public MakeFlange (Part part) { }
    #endregion

    #region Methods -------------------------------------------------
    public override bool Assisted () { return mCanAssist; }

    /// <summary>Adds a new flange with the specified height, angle and radius.</summary>
    /// Translates the selected pline to the given height
    /// and creates a bendline with the given angle.
    public override void Execute () {
        List<PLine> pLines = []; List<BendLine> bendLines = [];
        var (startPt, endPt) = (mPline.StartPoint, mPline.EndPoint);
        var angle = mPline.Angle;
        float bendDeduction = 3.665F;
        mHeight -= bendDeduction / 2;
        var (dx, dy) = (mHeight * Math.Cos (angle), mHeight * Math.Sin (angle));

        var (centroidX, centroidY) = (mPart?.Centroid.X, mPart?.Centroid.Y);
        if (startPt.Y < centroidY && endPt.Y < centroidY) dy = -dy;
        if (startPt.X < centroidX && endPt.X < centroidX) dx = -dx;

        var translatedLine = (PLine)mPline.Translated (dx, dy);
        pLines?.Add (translatedLine);
        var lines = mPart!.PLines.Where (x => x.Index != mPline.Index).ToList ();
        foreach (var pline in lines) {
            var (p1, p2) = (pline.StartPoint, pline.EndPoint);
            // Checks for the common vertex for selected pline and plines in the part
            // Pline's startpoint is checked with selected pline's points and also for pline's endpoint
            // and creates a new pline with the translated points
            if (mPline.HasVertex (p1)) pLines?.Add (new PLine (translatedLine.EndPoint, p2, pline.Index));
            else if (mPline.HasVertex (p2)) pLines?.Add (new PLine (p1, translatedLine.StartPoint, pline.Index));
            else pLines?.Add (pline);
        }
        bendLines?.Add (new BendLine (startPt, endPt, 1, new BendLineInfo (mBendAngle, 2, bendDeduction)));    // Bendline is created at the previous position of the selected pline
        if (pLines != null && bendLines != null) mProcessedPart = new (pLines, bendLines, mRadius, EBendAssist.AddFlange);
    }
    #endregion

    #region Private Data --------------------------------------------
    float mBendAngle;
    double mHeight;
    readonly PLine? mPline;    // Selected pline
    readonly float mRadius;    // Bend radius
    #endregion
}
#endregion
