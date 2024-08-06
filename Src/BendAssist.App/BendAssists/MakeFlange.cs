using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.BendAssists;

#region class MakeFlange --------------------------------------------------------------------------
public sealed class MakeFlange : BendAssist {
   #region Constructors ---------------------------------------------
   /// <summary>Gets the part, selected pline, bend angle, flange height, bend radius</summary>
   public MakeFlange (Part part, PLine pline, float angle, double height, float radius) =>
       (mPart, mPline, mBendAngle, mHeight, mRadius) = (part, pline, angle, height, radius);

   /// <summary>Gets the part</summary>
   public MakeFlange (Part part) => mPart = part;
   #endregion

   #region Properties -----------------------------------------------
   public override string[] Prompts => ["Select an edge to make flange [Ctrl= All edges]"];
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Adds a new flange with the specified height, angle and radius.</summary>
   /// Translates the selected pline to the given height
   /// Creates a bend line with the given angle.
   public override void Execute () {
      if (mPart == null) return;
      foreach (var bendline in mPart.BendLines) mBendLines.Add (bendline);
      bool anySelected = mPart.PLines.Any (x => x.IsSelected);
      foreach (var pline in mPart.PLines) {
         if (anySelected) {
            if (pline.IsSelected) GetFlange (pline);
            else mPLines.Add (new PLine (pline.StartPoint, pline.EndPoint, mIndex++));
         } else GetFlange (pline);
      }
      if (mPLines != null && mBendLines != null) mProcessedPart = new (mPLines, mBendLines, mRadius, EBendAssist.AddFlange);
   }

   void GetFlange (PLine pline) {
      if (mPart!.BendLines.Any (x => x.StartPoint.IsWithinBound (pline.Bound) || x.EndPoint.IsWithinBound (pline.Bound))) {
         mPLines.Add (pline);
         return;
      }
      var (startPt, endPt) = (pline.StartPoint, pline.EndPoint);
      // Radially moves the point perpendicular to the selected pline
      var angle = CommonUtils.ToRadians (pline.Angle - 90);
      var bendDeduction = BendUtils.GetBendDeduction (mBendAngle, 0.38, mPart.Thickness, mRadius);
      var height = mHeight;
      height -= bendDeduction / 2; // Gets the bend deducted height of the flange
      var (sin, cos) = Math.SinCos (angle);
      var (dx, dy) = (height * cos, height * sin); // Calculates the offsets in x and y
      // Translates the line with the offset values
      var translatedLine = (PLine)pline.Translated (dx, dy);
      var (tStart, tEnd) = (translatedLine.StartPoint, translatedLine.EndPoint);
      mPLines.Add (new (startPt, tStart, mIndex++));
      mPLines.Add (new (tStart, tEnd, mIndex++));
      mPLines.Add (new (tEnd, endPt, mIndex++));
      var count = mPart.BendLines.Count;
      // Bend line is created at the previous position of the selected pline
      // This bend line is added to the last of the list with index as count+1
      mBendLines?.Add (new BendLine (startPt, endPt, count > 0 ? count + 1 : 1, new BendLineInfo (mBendAngle, mRadius, (float)bendDeduction)));
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<PLine> mPLines = []; List<BendLine> mBendLines = [];
   int mIndex = 1;
   double mHeight = 10;    // Height of the flange
   readonly float mBendAngle = 90;    // Bend angle of flange
   readonly PLine? mPline;    // Selected pline
   readonly float mRadius = 2;    // Bend radius
   #endregion
}
#endregion
