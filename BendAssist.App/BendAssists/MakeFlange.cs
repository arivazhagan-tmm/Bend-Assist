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
   public MakeFlange (Part part) { }
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () { return mCanAssist; }

   /// <summary>Adds a new flange with the specified height, angle and radius.</summary>
   /// Translates the selected pline to the given height
   /// and creates a bendline with the given angle.
   public override void Execute () {
      List<PLine> pLines = []; List<BendLine> bendLines = [];
      var (startPt, endPt) = (mPline!.StartPoint, mPline.EndPoint);

      // Adds 90 degree to radially move the point
      var angle = CommonUtils.ToRadians (mPline.Angle) + CommonUtils.ToRadians (90);
      var bendDeduction = BendUtils.GetBendDeduction (mBendAngle, 0.38, 2, mRadius);

      // Gets the bend deducted height of the flange
      mHeight -= bendDeduction / 2;
      var (centroidX, centroidY) = (mPart?.Centroid.X, mPart?.Centroid.Y);

      // Calculates the offsets in x and y
      var (dx, dy) = (mHeight * Math.Cos (angle), mHeight * Math.Sin (angle));
      (dx, dy) = (startPt.X < centroidX && endPt.X < centroidX ? -dx : Math.Abs (dx),
                  startPt.Y < centroidY && endPt.Y < centroidY ? -dy : Math.Abs (dy));

      // Translates the line with the offset values
      var translatedLine = (PLine)mPline.Translated (dx, dy);

      // Get the lines from the part except the selected line
      var lines = mPart!.PLines.Where (x => x.Index != mPline.Index).ToList ();

      if (mPline.Orientation == EOrientation.Inclined) {
         // Inserts the plines at the specified index
         pLines = BendUtils.InsertAt (new PLine (mPline.StartPoint, translatedLine.StartPoint), mPline.Index, lines!);
         pLines?.Add (new PLine (translatedLine.StartPoint, translatedLine.EndPoint, mPline.Index + 1));
         pLines = BendUtils.InsertAt (new PLine (translatedLine.EndPoint, mPline.EndPoint), mPline.Index + 2, pLines!);
      } else {
         foreach (var pline in lines!) {
            var (p1, p2) = (pline.StartPoint, pline.EndPoint);
            pLines?.Add (translatedLine);
            // Checks for the common vertex for selected pline and plines in the part
            // Pline's points is checked with selected pline's points
            // and creates a new pline with the translated points
            pLines?.Add (mPline.HasVertex (p1) ? new PLine (translatedLine.EndPoint, p2, pline.Index) :
                         mPline.HasVertex (p2) ? new PLine (p1, translatedLine.StartPoint, pline.Index) : pline);
         }
      }
      // Bendline is created at the previous position of the selected pline
      bendLines?.Add (new BendLine (startPt, endPt, 1, new BendLineInfo (mBendAngle, mRadius, (float)bendDeduction)));
      if (pLines != null && bendLines != null) mProcessedPart = new (pLines, bendLines, mRadius, EBendAssist.AddFlange);
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mHeight;    // Height of the flange
   readonly float mBendAngle;    // Bend angle of flange
   readonly PLine? mPline;    // Selected pline
   readonly float mRadius;    // Bend radius
   #endregion

   #region Commented ------------------------------------------------
   //public static List<PLine> InsertAt (this PLine l, int index, List<PLine> lines) {
   //   if (lines.Count == 0) { lines.Add (l); return lines; }
   //   var len = lines.Count + 1;
   //   var tmp = new PLine[len];
   //   var (ptIndex, inserted) = (1, false);
   //   for (int i = 0; i < len; i++) {
   //      if (i == index - 1) {
   //         tmp[i] = new (l.StartPoint.Duplicate (ptIndex), l.EndPoint.Duplicate (++ptIndex), index);
   //         inserted = true;
   //      } else {
   //         var line = lines[i];
   //         var (startPt, endPt) = (line.StartPoint, line.EndPoint);
   //         tmp[i] = !inserted ? new (startPt, endPt, i + 1)
   //                            : new (startPt.Duplicate (ptIndex), endPt.Duplicate (++ptIndex), i + 1);
   //         ptIndex = line.EndPoint.Index;
   //      }
   //   }
   //   return [.. tmp];
   //}
   #endregion
}
#endregion
