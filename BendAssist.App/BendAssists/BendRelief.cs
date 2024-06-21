using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public BendRelief (Part part) => mFreshPart = part;
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