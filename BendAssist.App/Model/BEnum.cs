namespace BendAssist.App.Model;

/// <summary>Orientation of the line</summary>
public enum EOrientation { None, Horizontal, Vertical, Inclined }

/// <summary>Operations assist the flawless bending process</summary>
public enum EBendAssist { BendDeduction, BendRelief, CornerClose, CornerRelief }

/// <summary>Bend deduction algorithms</summary>
public enum EBDAlgorithm { EquallyDistributed, PartiallyDistributed }