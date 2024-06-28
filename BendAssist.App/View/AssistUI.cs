using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Reflection.Metadata;

namespace BendAssist.App.View;

#region class AssistUI ----------------------------------------------------------------------------
public class AssistUI : UserControl {
   #region Constructor ----------------------------------------------

   /// <summary>Gets an array of inputs required</summary>
   public AssistUI (params string[] inputs) {
      UniformGrid ufg = new UniformGrid () { Rows = inputs.Length, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness (5, 10, 0, 0) };
      foreach (var input in inputs) {
         Label lbl = new () { Content = input + " : ", Width = 50, Margin = new Thickness (5) };
         TextBox tBox = new TextBox () { Width = 50, Height = 20, Margin = new Thickness (5) };
         tBox.PreviewKeyDown += OnPreviewKeyDown;
         tBox.KeyDown += OnKeyDown;
         ufg.Children.Add (lbl);
         ufg.Children.Add (tBox);
      }
      Content = ufg;
   }
   #endregion

   #region Event Handlers -------------------------------------------
   // Handles the KeyDown event for the TextBox to process specific key actions
   void OnKeyDown (object sender, KeyEventArgs e) {
      if (sender is not TextBox tb) return;
      var key = e.Key;
      if (key is Key.Enter) { }
   }
   // Restricts the input given in the text box
   void OnPreviewKeyDown (object sender, KeyEventArgs e) {
      var key = e.Key;
      e.Handled = !((key is >= Key.D0 and <= Key.D9) ||
                    (key is >= Key.NumPad0 and <= Key.NumPad9) ||
                    (key is Key.Back or Key.Delete or Key.Left or Key.Enter or Key.LeftCtrl or Key.RightCtrl or
                     Key.Right or Key.Tab or Key.OemPlus or Key.OemMinus or Key.Decimal));
   }
   #endregion
}
#endregion