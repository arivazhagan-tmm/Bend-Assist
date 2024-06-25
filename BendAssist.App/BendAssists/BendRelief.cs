using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendRelief --------------------------------------------------------------------------
public sealed class BendRelief : BendAssist {
   #region Constructors ---------------------------------------------
   public BendRelief (Part part) => mPart = part;
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {




      return mCanAssist;
   }
   #endregion

   #region Implementation -------------------------------------------
   #endregion
}
#endregion