namespace BendAssist.App.Model;

/// <summary>Orientation of the line</summary>
public enum EOrientation { None = -1, Horizontal = 0, Vertical = 1, Inclined = 2 }

/// <summary>Operations assist the flawless bending process</summary>
public enum EBendAssist { AddFlange, BendDeduction, BendRelief, CornerClose, CornerRelief }

/// <summary>Bend deduction algorithms</summary>
public enum EBDAlgorithm { EquallyDistributed, PartiallyDistributed }

/// <summary>Location of the points and lines</summary>
public enum ELoc { Bottom, Left, Right, Top }

/// <summary>Cartesian Coordinates</summary>
public enum EPCoord { X, Y }