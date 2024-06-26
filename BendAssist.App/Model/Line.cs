using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region class Line --------------------------------------------------------------------------------
public abstract class Line : ICloneable {
   #region Properties -----------------------------------------------
   public bool IsBendLine { get; init; }
   public int Index { get => mIndex; init => mIndex = value; }
   public double Length { get => mLength; init => mLength = value; }
   public Bound2 Bound => mBound;
   public Point2 StartPoint { get => mStartPoint; init => mStartPoint = value; }
   public Point2 EndPoint { get => mEndPoint; init { mEndPoint = value; UpdateProperties (); } }
   public EOrientation Orientation { get => mOrientation; init => mOrientation = value; }
   #endregion

   #region Methods --------------------------------------------------
   public object Clone () => GetLine (mStartPoint, mEndPoint, mIndex);
   public override string? ToString () => $"{mStartPoint}, {mEndPoint}, [{Index}]";
   public virtual Line Translated (double dx, double dy) {
      var v = new Vector2 (dx, dy);
      var (startPt, endPt) = (mStartPoint + v, mEndPoint + v);
      return GetLine (startPt, endPt, mIndex);
   }
   public virtual Line Trimmed (double startDx, double startDy, double endDx, double endDy) {
      var (startPt, endPt) = (new Point2 (mStartPoint.X + startDx, mStartPoint.Y + startDy, mStartPoint.Index),
                              new Point2 (mEndPoint.X + endDx, mEndPoint.Y + endDy, mEndPoint.Index));
      return GetLine (startPt, endPt, mIndex);
   }
   #endregion

   #region Implementation -------------------------------------------
   Line GetLine (Point2 startPt, Point2 endPt, int index) {
      Line line = null!;
      if (this is PLine) line = new PLine (startPt, endPt, index);
      else if (this is BendLine bl) line = new BendLine (startPt, endPt, index, bl.BLInfo);
      return line;
   }

   void UpdateProperties () {
      if (mStartPoint.IsSet && mEndPoint.IsSet) {
         mAngle = mStartPoint.AngleTo (mEndPoint);
         mOrientation = mAngle switch {
            0.0 or 180.0 => EOrientation.Horizontal,
            90.0 or 270.0 => EOrientation.Vertical,
            _ => EOrientation.Inclined
         };
         mLength = mStartPoint.DistanceTo (mEndPoint);
      }
   }
   #endregion

   #region Private Data ---------------------------------------------
   protected int mIndex;
   protected double mAngle, mLength;
   protected EOrientation mOrientation;
   protected Bound2 mBound;
   protected Point2 mStartPoint, mEndPoint;
   #endregion
}
#endregion

#region class PLine -------------------------------------------------------------------------------
public sealed class PLine : Line {
   #region Constructors ---------------------------------------------
   public PLine (Point2 startPt, Point2 endPt, int index = -1) {
      (mStartPoint, mEndPoint, mIndex) = (startPt, endPt, index);
      mBound = BendUtils.CreateBoundAround (this);
   }
   #endregion
}
#endregion

#region class BendLine ----------------------------------------------------------------------------
public sealed class BendLine : Line {
   #region Constructors ---------------------------------------------
   public BendLine (Point2 startPt, Point2 endPt, int index, BendLineInfo info) =>
      (mStartPoint, mEndPoint, mIndex, BLInfo, IsBendLine) = (startPt, endPt, index, info, true);
   #endregion

   #region Properties -----------------------------------------------
   public readonly BendLineInfo BLInfo;
   #endregion
}
#endregion

#region struct BendLineInfo -----------------------------------------------------------------------
public readonly record struct BendLineInfo (float Angle, float Radius, float Deduction);
#endregion