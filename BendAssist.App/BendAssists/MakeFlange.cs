using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class MakeFlange --------------------------------------------------------------------------
public sealed class MakeFlange : BendAssist {
    #region Constructors --------------------------------------------
    public MakeFlange (Part part, PLine pline, double angle, double height, double radius) =>
        (mPart, mPline, mAngle, mHeight, mRadius) = (part, pline, angle, height, radius);
    #endregion

    #region Methods -------------------------------------------------
    public override bool Assisted () { return mCanAssist; }

    /// <summary>Adds a new flange with the specified height, angle and radius.</summary>
    /// Translates the selected pline to the given height
    /// and creates a bendline with the given angle.
    public override void Execute () {
        var startPt = mPline.StartPoint; var endPt = mPline.EndPoint;
        mAngle += 90;
        mHeight -= mBendDeduction / 2;
        var dx = mHeight * Math.Cos (mAngle);
        var dy = mHeight * Math.Sin (mAngle);
        var translatedLine = (PLine)mPline.Translated (dx, dy);
        mPLines?.Add (translatedLine);
        foreach (var pline in mPart!.PLines.Where (x => x.Index != mPline.Index)) {
            var p1 = pline.StartPoint; var p2 = pline.EndPoint;
            // Checks for the common vertex for selected pline and plines in the part
            // Pline's startpoint is checked with selected pline's points and also for pline's endpoint
            // and creates a new pline with the translated points
            if (startPt.AreEqual (p1) || endPt.AreEqual (p1)) mPLines?.Add (new PLine (translatedLine.StartPoint, p2, pline.Index));
            else if (startPt.AreEqual (p2) || endPt.AreEqual (p2)) mPLines?.Add (new PLine (p1, translatedLine.EndPoint, pline.Index));
            else mPLines?.Add (pline);
        }
        mBendLines?.Add (new BendLine (startPt, endPt, 1, new BendLineInfo ()));    // Bendline is created at the previous position of the selected pline
        mProcessedPart = new (mPLines!, mBendLines!, 2, EBendAssist.AddFlange);
    }
    #endregion

    #region Private Data --------------------------------------------
    List<PLine>? mPLines;
    List<BendLine>? mBendLines;
    double mAngle, mBendDeduction, mHeight;
    readonly PLine mPline;    // Selected pline
    readonly double mRadius;    // Bend radius
    #endregion
}
#endregion
