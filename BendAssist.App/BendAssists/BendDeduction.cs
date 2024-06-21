using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendDeduction -----------------------------------------------------------------------
public sealed class BendDeduction : BendAssist {
   #region Constructors ---------------------------------------------
   public BendDeduction (Part part, EBDAlgorithm algorithm) => (mFreshPart, mAlgorithm) = (part, algorithm);
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) { } 
      else { }
      return true;
   }
   #endregion

   #region Implementation -------------------------------------------
   #endregion

   #region Private Data ---------------------------------------------
   readonly EBDAlgorithm mAlgorithm;
   #endregion
}
#endregion