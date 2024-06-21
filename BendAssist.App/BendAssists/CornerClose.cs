using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class CornerClose -------------------------------------------------------------------------
public sealed class CornerClose : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerClose (Part part) => mFreshPart = part;
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      return true;
   }
   #endregion

   #region Implementation -------------------------------------------
   #endregion
}
#endregion