using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendDeduction -----------------------------------------------------------------------
public sealed class BendDeduction : BendAssist {
   #region Constructors ---------------------------------------------
   public BendDeduction (Part part, EBDAlgorithm algorithm) => (mPart, mAlgorithm) = (part, algorithm);
   #endregion

   #region Methods --------------------------------------------------
   public override bool Assisted () {
      if (mPart is null) return false;
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) {
         var newBLines = new List<BendLine> ();
         foreach (var bl in mPart.BendLines) {
         }
      } else { }
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