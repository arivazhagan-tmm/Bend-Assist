using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using VA = System.Windows.VerticalAlignment;
using HA = System.Windows.HorizontalAlignment;
using Microsoft.Win32;
using BendAssist.App.View;
using BendAssist.App.FileHandling;

namespace BendAssist.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   #region Constructors ---------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      (Height, Width) = (750, 900);
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      WindowState = WindowState.Maximized;
      WindowStyle = WindowStyle.SingleBorderWindow;
      Loaded += OnLoaded;
   }
   #endregion

   #region Properties -----------------------------------------------
   public static MainWindow? It;
   #endregion

   #region Methods --------------------------------------------------
   #endregion

   #region Implementation -------------------------------------------
   void OnLoaded (object sender, RoutedEventArgs e) {
      It = this;
      #region Styles ----------------------------
      var spStyle = new Style ();
      spStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      spStyle.Setters.Add (new Setter (VerticalContentAlignmentProperty, VA.Top));
      spStyle.Setters.Add (new Setter (BackgroundProperty, Brushes.Transparent));
      var menuPanel = new StackPanel () { Style = spStyle };
      var menuStyle = new Style ();
      menuStyle.Setters.Add (new Setter (WidthProperty, 50.0));
      menuStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      #endregion
      var menu = new Menu ();
      var fileMenu = new MenuItem () { Style = menuStyle, Header = "_File" };
      var saveMenu = new MenuItem () { Header = "_Export...", IsEnabled = false };
      saveMenu.Click += (s, e) => {
         var currentFileName = "";
         var dlg = new SaveFileDialog () { FileName = $"{Path.GetFileNameWithoutExtension (currentFileName)}_BendProfile", Filter = "GEO|*.geo" };
         if (dlg.ShowDialog () is true) { }
      };
      var openMenu = new MenuItem () { Header = "_Import...", IsEnabled = true };
      openMenu.Click += (s, e) => {
         var dlg = new OpenFileDialog () {
            DefaultExt = ".geo", Title = "Import Geo file", Filter = "Geo files (*.geo)|*.geo"
         };
         if (dlg.ShowDialog () is true) {
            var reader = new GeoReader (dlg.FileName);
            var part = reader.ParsePart ();
            mViewport?.Clear ();
            mViewport?.UpdateViewport (part);
         }
      };
      fileMenu.Items.Add (openMenu);
      fileMenu.Items.Add (saveMenu);
      menu.Items.Add (fileMenu);
      menuPanel.Children.Add (menu);
      var dp = new DockPanel ();
      dp.Children.Add (menuPanel);
      mViewport = new Viewport ();
      dp.Children.Add (mViewport);
      DockPanel.SetDock (menuPanel, Dock.Top);
      DockPanel.SetDock (mViewport, Dock.Bottom);
      mMainPanel.Content = dp;
      Background = Brushes.WhiteSmoke;
   }
   #endregion

   #region Private Data ---------------------------------------------
   Viewport? mViewport;
   #endregion
}