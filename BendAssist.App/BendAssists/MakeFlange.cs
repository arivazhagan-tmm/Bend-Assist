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

   #region Methods --------------------------------------------------
   public override bool Assisted () { return mCanAssist; }

   /// <summary>Adds a new flange with the specified height, angle and radius.</summary>
   /// Translates the selected pline to the given height
   /// and creates a bend line with the given angle.
   public override void Execute () {
      if (mPart == null) return;
      int index = 1;
      List<PLine> pLines = []; List<BendLine> bendLines = [];
      foreach (var bendline in mPart.BendLines) bendLines?.Add (bendline);
      foreach (var pline in mPart.PLines) {
         var (startPt, endPt) = (pline.StartPoint, pline.EndPoint);

         // Radially moves the point perpendicular to the selected pline
         var angle = CommonUtils.ToRadians (pline.Angle - 90);
         var bendDeduction = BendUtils.GetBendDeduction (mBendAngle, 0.38, 2, mRadius);
         mHeight -= bendDeduction / 2; // Gets the bend deducted height of the flange

         // Calculates the offsets in x and y
         var (dx, dy) = (mHeight * Math.Cos (angle), mHeight * Math.Sin (angle));

         // Translates the line with the offset values
         var translatedLine = (PLine)pline.Translated (dx, dy);

         pLines?.Add (new PLine (pline.StartPoint, translatedLine.StartPoint, index++));
         pLines?.Add (new PLine (translatedLine.StartPoint, translatedLine.EndPoint, index++));
         pLines?.Add (new PLine (translatedLine.EndPoint, pline.EndPoint, index++));

         var count = mPart.BendLines.Count;
         // Bend line is created at the previous position of the selected pline
         bendLines?.Add (new BendLine (startPt, endPt, count > 0 ? count - 1 : 1, new BendLineInfo (mBendAngle, mRadius, (float)bendDeduction)));
         if (pLines != null && bendLines != null) mProcessedPart = new (pLines, bendLines, mRadius, EBendAssist.AddFlange);
      }
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mHeight = 10;    // Height of the flange
   readonly float mBendAngle = 2;    // Bend angle of flange
   readonly PLine? mPline;    // Selected pline
   readonly float mRadius = 2;    // Bend radius
   #endregion
}
#endregion
