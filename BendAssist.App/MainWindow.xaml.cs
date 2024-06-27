using BendAssist.App.BendAssists;
using BendAssist.App.FileHandling;
using BendAssist.App.Model;
using BendAssist.App.View;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using HA = System.Windows.HorizontalAlignment;
using VA = System.Windows.VerticalAlignment;

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
        var btnStyle = new Style ();
        btnStyle.Setters.Add (new Setter (WidthProperty, 100.0));
        btnStyle.Setters.Add (new Setter (HeightProperty, 25.0));
        btnStyle.Setters.Add (new Setter (BackgroundProperty, Brushes.WhiteSmoke));
        btnStyle.Setters.Add (new Setter (MarginProperty, new Thickness (5.0)));
        btnStyle.Setters.Add (new Setter (HorizontalAlignmentProperty, HA.Left));
        btnStyle.Setters.Add (new Setter (VerticalAlignmentProperty, VA.Top));
        var borderStyle = new Style () { TargetType = typeof (Border) };
        borderStyle.Setters.Add (new Setter (Border.CornerRadiusProperty, new CornerRadius (5.0)));
        borderStyle.Setters.Add (new Setter (Border.BorderThicknessProperty, new Thickness (5.0)));
        btnStyle.Resources = new ResourceDictionary { [typeof (Border)] = borderStyle };
        var tbStyle = new Style ();
        tbStyle.Setters.Add (new Setter (HeightProperty, 20.0));
        tbStyle.Setters.Add (new Setter (BackgroundProperty, Brushes.WhiteSmoke));
        tbStyle.Setters.Add (new Setter (MarginProperty, new Thickness (5, 5, 0, 0)));
        tbStyle.Setters.Add (new Setter (HorizontalAlignmentProperty, HA.Left));
        tbStyle.Setters.Add (new Setter (VerticalAlignmentProperty, VA.Top));
        #endregion
        var menu = new Menu ();
        var fileMenu = new MenuItem () { Style = menuStyle, Header = "_File" };
        var saveMenu = new MenuItem () { Header = "_Export...", IsEnabled = false };
        var btnGrid = new UniformGrid () { Columns = 2 }; // Displays bend assist options
        var infoGrid = new UniformGrid () { Rows = 3, Columns = 2 }; // Displays info of part imported
        var optionPanel = new StackPanel () { Margin = new Thickness (0, 20, 0, 0) };
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
                var fileName = dlg.FileName;
                var part = reader.ParsePart ();
                mViewport?.Clear ();
                var mkf = new MakeFlange (part, new PLine (new Point2 (100, 0,3), new Point2 (100, 100,4), 2), 90, 10, 2);
                mViewport?.UpdateViewport (part);
                mkf.Execute ();
                mViewport?.Clear ();
                mViewport?.UpdateViewport (mkf.ProcessedPart!);
                // Info of the part
                string[] infobox = ["File Name : ", "Sheet Size : ", "BendLines : "];
                string[] infoboxvalue = [$"{Path.GetFileNameWithoutExtension (fileName)}{Path.GetExtension (fileName)}",
                                         $"{part.Bound.Width:F2} X {part.Bound.Height:F2}",
                                         $"{part.BendLines.Count}"];
                infoGrid.Children.Clear ();
                for (int i = 0; i < infobox.Length; i++) {
                    infoGrid.Children.Add (new Label () { Content = infobox[i], Margin = new Thickness (5, 5, 0, 0) });
                    infoGrid.Children.Add (new TextBlock () { Text = infoboxvalue[i], Style = tbStyle });
                }
                if (!optionPanel.Children.Contains (infoGrid))
                    optionPanel.Children.Add (infoGrid);
            }
        };
        foreach (var option in Enum.GetValues (typeof (EBendAssist))) {
            var btn = new Button () {
                Content = option,
                Tag = option,
                Style = btnStyle,
            };
            btnGrid.Children.Add (btn);
        }
        optionPanel.Children.Add (btnGrid);
        fileMenu.Items.Add (openMenu);
        fileMenu.Items.Add (saveMenu);
        menu.Items.Add (fileMenu);
        menuPanel.Children.Add (menu);
        var dp = new DockPanel ();
        mViewport = new Viewport ();
        dp.Children.Add (menuPanel);
        dp.Children.Add (optionPanel);
        dp.Children.Add (mViewport);
        DockPanel.SetDock (menuPanel, Dock.Top);
        DockPanel.SetDock (mViewport, Dock.Right);
        DockPanel.SetDock (optionPanel, Dock.Left);
        mMainPanel.Content = dp;
        Background = Brushes.WhiteSmoke;
    }
    #endregion

    #region Private Data ---------------------------------------------
    Viewport? mViewport;
    #endregion
}