using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class MakeFlange --------------------------------------------------------------------------
public sealed class MakeFlange : BendAssist {
    #region Constructors ---------------------------------------------
    public MakeFlange (Part part) { }
    #endregion

    #region Properties -----------------------------------------------
    #endregion

    #region Methods --------------------------------------------------
    public override bool Assisted () { return mCanAssist; }
    #endregion
}
#endregion
