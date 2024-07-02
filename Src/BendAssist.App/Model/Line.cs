using BendAssist.App.Utils;

namespace BendAssist.App.Model;

#region class Line --------------------------------------------------------------------------------
public abstract class Line : ICloneable {
   #region Properties -----------------------------------------------
   /// <summary>Returns true if the created line is tagged as bend line</summary>
   public bool IsBendLine { get; init; }
   /// <summary>Creates the current index of the line while object creation</summary>
   public int Index { get => mIndex; init => mIndex = value; }
   /// <summary>Distance between start and end point</summary>
   public double Length { get => mLength; init => mLength = value; }
   /// <summary>Angle between start and end point</summary>
   public double Angle { get => mAngle; init => mAngle = value; }
   /// <summary>Aligned bound created around the line</summary>
   public Bound2 Bound => mBound;
   /// <summary>Start point of the line</summary>
   public Point2 StartPoint { get => mStartPoint; init => mStartPoint = value; }
   /// <summary>End point of the line</summary>
   public Point2 EndPoint { get => mEndPoint; init { mEndPoint = value; UpdateProperties (); } }
   /// <summary>Orientation of the line either Horizontal or Vertical or Inclined</summary>
   public EOrientation Orientation { get => mOrientation; init => mOrientation = value; }
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Clones the given line and returns it as a new line</summary>
   public object Clone () => GetLine (mStartPoint, mEndPoint, mIndex);

   /// <summary>Shifts the line by the given dx and dy</summary>
   public virtual Line Translated (double dx, double dy) {
      var v = new Vector2 (dx, dy);
      var (startPt, endPt) = (mStartPoint + v, mEndPoint + v);
      return GetLine (startPt, endPt, mIndex);
   }

   /// <summary>Trims the line with respect to the given offsets</summary>
   public virtual Line Trimmed (double startDx = 0.0, double startDy = 0.0, double endDx = 0.0, double endDy = 0.0) {
      var (startPt, endPt) = (new Point2 (mStartPoint.X + startDx, mStartPoint.Y + startDy, mStartPoint.Index),
                              new Point2 (mEndPoint.X + endDx, mEndPoint.Y + endDy, mEndPoint.Index));
      return GetLine (startPt, endPt, mIndex);
   }

   public override string? ToString () => $"{mStartPoint}, {mEndPoint}, [{Index}]";
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Returns a new line with the given parameters</summary>
   Line GetLine (Point2 startPt, Point2 endPt, int index) {
      Line line = null!;
      if (this is PLine) line = new PLine (startPt, endPt, index);
      else if (this is BendLine bl) line = new BendLine (startPt, endPt, index, bl.BLInfo);
      return line;
   }

   /// <summary>Sets the properties of the line</summary>
   protected void UpdateProperties () {
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
      UpdateProperties ();
   }
   #endregion
}
#endregion

#region class BendLine ----------------------------------------------------------------------------
public sealed class BendLine : Line {
   #region Constructors ---------------------------------------------
   public BendLine (Point2 startPt, Point2 endPt, int index, BendLineInfo info) {
      (mStartPoint, mEndPoint, mIndex, BLInfo, IsBendLine) = (startPt, endPt, index, info, true);
      UpdateProperties ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly BendLineInfo BLInfo;
   #endregion
}
#endregion

#region struct BendLineInfo -----------------------------------------------------------------------
public readonly record struct BendLineInfo (float Angle, float Radius, float Deduction);
#endregion