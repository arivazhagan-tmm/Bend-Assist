using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendAssist --------------------------------------------------------------------------
public abstract class BendAssist {
   #region Properties -----------------------------------------------
   public string? ProcessError => mProcessError;
   public Part? Part { get => mFreshPart; init => mFreshPart = value; }
   public ProcessedPart? ProcessedPart { get => mProcessedPart; init => mProcessedPart = value; }
   #endregion

   #region Methods --------------------------------------------------
   public abstract bool Assisted ();
   #endregion

   #region Private Data ---------------------------------------------
   protected bool mCanAssist;
   protected string? mProcessError;
   protected Part? mFreshPart;
   protected ProcessedPart? mProcessedPart;
   #endregion
}
#endregion