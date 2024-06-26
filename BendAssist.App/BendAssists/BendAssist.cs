using BendAssist.App.Model;

namespace BendAssist.App.BendAssists;

#region class BendAssist --------------------------------------------------------------------------
public abstract class BendAssist {
   #region Properties -----------------------------------------------
   /// <summary>Instructions to be shown to the user</summary>
   public virtual string[] Prompts => mPrompts ??= [];
   public string? AssistError => mAssistError;
   public Part? Part { get => mPart; init => mPart = value; }
   public ProcessedPart? ProcessedPart { get => mProcessedPart; init => mProcessedPart = value; }
   #endregion

   #region Methods --------------------------------------------------
   public abstract bool Assisted ();

   public virtual void Execute () { }

   public virtual void ReceiveInput (object obj) {
      if (obj is Point2 pt) mSelectedPoint = pt;
      else if (obj is Line l) mSelectedLine = l;
      else if (obj is BendLine bl) mSelectedBendLine = bl;
   }
   #endregion

   #region Private Data ---------------------------------------------
   protected bool mCanAssist;
   protected string? mAssistError;
   protected string[]? mPrompts;
   protected Point2 mSelectedPoint; // Point selected by user on UI
   protected Line? mSelectedLine;
   protected BendLine? mSelectedBendLine;
   protected Part? mPart; // Current part being assisted
   protected ProcessedPart? mProcessedPart; // Bend assisted part
   #endregion
}
#endregion