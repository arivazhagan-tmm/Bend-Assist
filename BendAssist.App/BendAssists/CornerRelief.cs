using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class CornerRelief ------------------------------------------------------------------------
public sealed class CornerRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public CornerRelief (Part part) => mFreshPart = part;
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