using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.View;

#region class Viewport ----------------------------------------------------------------------------
internal sealed class Viewport : Canvas {
   #region Constructors ---------------------------------------------
   public Viewport () => Loaded += OnLoaded;
   #endregion

   #region Properties -----------------------------------------------
   #endregion

   #region Methods --------------------------------------------------
   public void Clear () => mLines?.Clear ();

   public void UpdateViewport (Part part) {
      mPart = part;
      if (part is null || part.PLines is null || part.BendLines is null) return;
      mLines ??= [];
      if (part is ProcessedPart pr) {

      } else {
         mLines.AddRange (part.PLines.Where (pl => !mLines.HasDuplicate (pl)));
         mLines.AddRange (part.BendLines.Where (pl => !mLines.HasDuplicate (pl)));
      }
      UpdateBound ();
      ZoomExtents ();
   }

   /// <summary>Zooms the entity extents and fit them to the current viewport size</summary>
   public void ZoomExtents () {
      UpdateBound ();
      InvalidateVisual ();
   }

   protected override void OnRender (DrawingContext dc) {
      dc.DrawRectangle (Brushes.LightGray, mBGPen, mVRect);
      if (mLines is null || mLines.Count == 0) return;
      foreach (var l in mLines) {
         var pen = l is PLine ? mPLPen : mBLPen;
         dc.DrawLine (pen, Transform (l.StartPoint), Transform (l.EndPoint));
      }
      base.OnRender (dc);
      Point Transform (Point2 pt) => mPXfm.Transform (pt.Convert ());
   }
   #endregion

   #region Implementation -------------------------------------------
   // Populates the snap source with the vertices of the plines and bend lines
   void LoadSnapSource () {
      if (mPart is null || mProcessedPart is null) return;
      mSnapSource ??= [];
      mSnapSource.AddRange (mPart.Vertices);
      mSnapSource.AddRange (mProcessedPart.Vertices);
   }

   // Initializes the rendering objects and attaches the mouse events
   void OnLoaded (object sender, RoutedEventArgs e) {
      #region Initializing members --------------
      Background = Brushes.Transparent;
      mMargin = 25.0;
      mSnapSource = [];
      mBGPen = new (Brushes.Gray, 0.5);
      mBLPen = new (Brushes.ForestGreen, 2.0) { DashStyle = DashStyles.Dash };
      mPLPen = new (Brushes.Black, 1.0);
      if (MainWindow.It != null)
         mVRect = new Rect (new Size (MainWindow.It.ActualWidth - 320, MainWindow.It.ActualHeight - 100));
      (mWidth, mHeight) = (mVRect.Width, mVRect.Height);
      mVBound = new Bound2 (new (0.0, 0.0), new (mWidth, mHeight));
      mCenter = new Point (mVBound.Mid.X, mVBound.Mid.Y);
      UpdatePXfm (mVBound);
      #endregion

      #region Attaching Events ------------------
      MouseMove += OnMouseMove;
      MouseWheel += OnMouseWheel;
      SizeChanged += (s, e) => ZoomExtents ();
      #endregion

      var menu = new ContextMenu ();
      var zoomExtnd = new MenuItem () { Header = "Zoom Extents" };
      zoomExtnd.Click += (s, e) => ZoomExtents ();
      menu.Items.Add (zoomExtnd);
      ContextMenu = menu;
   }

   // Udpates the snap point and current mouse point on the viewport
   void OnMouseMove (object sender, MouseEventArgs e) {
      mMousePt = e.GetPosition (this).Transform (mIPXfm);
      if (mSnapSource != null && mMousePt.HasNeighbour (mSnapSource, mSnapDelta, out var pt))
         mSnapPt = mMousePt = pt;
      InvalidateVisual ();
   }

   // Updates the bound of drawn entities using projection transform
   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      UpdatePXfm (mVBound.Transform (mIPXfm).Inflated (mMousePt, zoomFactor));
      InvalidateVisual ();
   }

   // Updates the bound of drawn entities using projection transform
   void UpdateBound () {
      List<Point2> boundPts = mPart != null ? mPart.Vertices : [];
      if (mProcessedPart != null) boundPts.AddRange (mProcessedPart.Vertices);
      if (boundPts.Count > 2) UpdatePXfm (new Bound2 (boundPts));
   }

   // Updates the projection transform matrix to zoom fits the entities in the viewport
   void UpdatePXfm (Bound2 b) {
      double scaleX = (mWidth - mMargin) / b.Width,
             scaleY = (mHeight - mMargin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var xfm = Matrix.Identity;
      xfm.Scale (scale, -scale);
      // Projected mid point of the bound
      Point pMidPt = xfm.Transform (new Point (b.Mid.X, b.Mid.Y));
      var (dx, dy) = (mCenter.X - pMidPt.X, mCenter.Y - pMidPt.Y);
      xfm.Translate (dx, dy);
      mPXfm = xfm;
      mIPXfm = mPXfm;
      mIPXfm.Invert ();
      mSnapDelta = b.MaxX * 0.01;
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mWidth, mHeight, mMargin, mSnapDelta; // Viewport width, height, margin, snap tolreance
   Bound2 mVBound; // ViewportBound;
   Point mCenter; // Viewport center
   Point2 mMousePt, mSnapPt; // Current mouse point, Current snap point
   Rect mVRect; // Viewport rectangle
   Matrix mPXfm, mIPXfm; // Projection transform, Inverse projection transform
   Pen? mBGPen, mBLPen, mPLPen; // Pen for Background, BendLine, PLine
   List<Point2>? mSnapSource; // vertices of the plines and bend lines to set the snap point on mouse move
   Part? mPart;
   ProcessedPart? mProcessedPart;
   List<Line>? mLines;
   #endregion
}
#endregion