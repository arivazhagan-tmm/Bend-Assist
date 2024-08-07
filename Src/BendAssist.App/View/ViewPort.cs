﻿using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static System.Windows.Controls.ToolTipService;
using BendAssist.App.Model;
using BendAssist.App.Utils;

namespace BendAssist.App.View;

#region class Viewport ----------------------------------------------------------------------------
internal sealed class Viewport : Canvas {
   #region Constructors ---------------------------------------------
   public Viewport () => Loaded += OnLoaded;
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Clears the drawn lines from the viewport</summary>
   public void Clear () {
      mPart = mProcessedPart = null;
      mLines?.Clear ();
   }

   public void SetSelectionMode (EMode mode = EMode.None) {
      mCurrentMode = mode;
      Cursor = mode switch {
         EMode.Pick => Cursors.Hand,
         EMode.Clip => Cursors.Cross,
         _ => Cursors.Arrow
      };
   }

   /// <summary>Draws the plines of the part in the viewport</summary>
   public void UpdateViewport (Part part) {
      if (part is null) return;
      mLines ??= [];
      if (part is null || part.PLines is null || part.BendLines is null) return;
      if (part is ProcessedPart pPart) mProcessedPart = pPart;
      else mPart = part;
      LoadSnapSource ();
      ZoomExtents ();
   }

   /// <summary>Zooms the entity extents and fit them to the current viewport size</summary>
   public void ZoomExtents () {
      UpdateBound ();
      InvalidateVisual ();
   }
   #endregion

   #region Implementation -------------------------------------------
   // Populates the snap source with the vertices of the plines and bend lines
   void LoadSnapSource () {
      mSnapSource ??= [];
      mSnapSource.AddRange (mPart!.Vertices);
      if (mProcessedPart != null) mSnapSource.AddRange (mProcessedPart.Vertices);
   }

   // Initializes the rendering objects and attaches the mouse events
   void OnLoaded (object sender, RoutedEventArgs e) {
      #region Initializing members --------------
      Background = Brushes.Transparent;
      ClipToBounds = true;
      mMargin = 25.0;
      mSnapSource = [];
      mBGPen = new (Brushes.Gray, 0.5);
      mBLPen = new (Brushes.ForestGreen, 2.0) { DashStyle = DashStyles.Dash };
      mPLPen = new (Brushes.Black, 1.0);
      if (MainWindow.It != null)
         mVRect = new Rect (new Size (MainWindow.It.ActualWidth - 220, MainWindow.It.ActualHeight - 100));
      (mWidth, mHeight) = (mVRect.Width, mVRect.Height);
      mVBound = new Bound2 (new (0.0, 0.0), new (mWidth, mHeight));
      mCenter = new Point (mVBound.Mid.X, mVBound.Mid.Y);
      UpdatePXfm (mVBound);
      #endregion

      #region Attaching Events ------------------
      MouseMove += OnMouseMove;
      MouseWheel += OnMouseWheel;
      MouseEnter += (s, e) => Cursor = Cursors.Cross;
      MouseLeave += (s, e) => Cursor = Cursors.Arrow;
      MouseUp += (s, e) => {
         if (mIsClipping) {
            foreach (var line in mPart?.PLines!) {
               if (line is PLine pl && pl.IsInside (new Bound2 (mFirstPt, mMousePt))) {
                  pl.IsSelected = true;
               }
            }
         }
         mIsClipping = false;
         mFirstPt = new ();
         InvalidateVisual ();
      };
      MouseLeftButtonDown += (s, e) => mFirstPt = e.GetPosition (this).Transform (mIPXfm);
      SizeChanged += (s, e) => ZoomExtents ();
      #endregion

      mCords = new TextBlock () { Background = Brushes.Transparent };
      mToolTip = new ToolTip ();
      var menu = new ContextMenu ();
      var zoomExtent = new MenuItem () { Header = "Zoom Extents" };
      var clearViewPort = new MenuItem () { Header = "Clear" };
      zoomExtent.Click += (s, e) => ZoomExtents ();
      clearViewPort.Click += (s, e) => Clear ();
      menu.Items.Add (zoomExtent);
      ContextMenu = menu;
      SetToolTip (this, mToolTip);
      Children.Add (mCords);
   }

   // Updates the snap point and current mouse point on the viewport
   void OnMouseMove (object sender, MouseEventArgs e) {
      mIsClipping = e.LeftButton is MouseButtonState.Pressed;
      mMousePt = e.GetPosition (this).Transform (mIPXfm);
      mSnapPt = new Point2 ();
      if (mSnapSource != null && mMousePt.HasNeighbour (mSnapSource, mSnapDelta, out var pt)) {
         mSnapPt = mMousePt = pt;
         // Tool tip for snap points
         mToolTip!.Content = $"X : {mSnapPt.X:F2}  Y : {mSnapPt.Y:F2}";
         mToolTip.IsOpen = true;
         SetPlacement (this, PlacementMode.Mouse);
      } else mToolTip!.IsOpen = false;
      if (mCords != null) mCords.Text = $"X : {double.Round (mMousePt.X, 2)}  Y : {double.Round (mMousePt.Y, 2)}"; // To display the current mouse point
      InvalidateVisual ();
   }

   // Updates the bound of drawn entities using projection transform
   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      UpdatePXfm (mVBound.Transform (mIPXfm).Inflated (mMousePt, zoomFactor));
      InvalidateVisual ();
   }

   protected override void OnRender (DrawingContext dc) {
      dc.PushClip (new RectangleGeometry (mVRect)); // Keeps drawing inside viewport bounds
      if (mIsClipping && mFirstPt.IsSet) {
         var (start, end) = (Transform (mFirstPt), Transform (mMousePt));
         dc.DrawRectangle (Brushes.GhostWhite, mBGPen, new Rect (start, end));
      }
      if (mPart is null) return;
      var v = new Vector2 (mPart.Bound.MaxX + 50, 0.0);
      foreach (var l in mPart.PLines) {
         var pen = l.IsSelected ? new Pen (Brushes.WhiteSmoke, 2.0) : mPLPen;
         dc.DrawLine (pen, Transform (l.StartPoint), Transform (l.EndPoint));
      }
      foreach (var l in mPart.BendLines) dc.DrawLine (mBLPen, Transform (l.StartPoint), Transform (l.EndPoint));
      if (mProcessedPart != null) {
         foreach (var l in mProcessedPart.PLines) dc.DrawLine (mPLPen, Transform (l.StartPoint + v), Transform (l.EndPoint + v));
         foreach (var l in mProcessedPart.BendLines) dc.DrawLine (mBLPen, Transform (l.StartPoint + v), Transform (l.EndPoint + v));
      }
      if (mSnapPt.IsSet) {
         var snapSize = 5;
         var snapPt = Transform (mSnapPt);
         var vec = new Vector (snapSize, snapSize);
         dc.DrawRectangle (Brushes.White, mPLPen, new (snapPt - vec, snapPt + vec));
      }
      base.OnRender (dc);
      Point Transform (Point2 pt) => mPXfm.Transform (pt.Convert ());
   }

   // Updates the bound of drawn entities using projection transform
   void UpdateBound () {
      if (mPart is null) return;
      List<Point2> boundPts = [];
      var vec = new Vector2 (mPart.Bound.MaxX * 2, 0.0);
      if (mPart != null) boundPts.AddRange (mPart.Vertices);
      if (mProcessedPart != null) boundPts.AddRange (mProcessedPart.Vertices.Select (v => v + vec));
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
   EMode mCurrentMode;
   bool mIsClipping;
   double mWidth, mHeight, mMargin, mSnapDelta; // Viewport width, height, margin, snap tolreance
   Bound2 mVBound; // ViewportBound;
   Point mCenter; // Viewport center
   Point2 mMousePt, mSnapPt, mFirstPt; // Current mouse point, Current snap point
   Rect mVRect; // Viewport rectangle
   Matrix mPXfm, mIPXfm; // Projection transform, Inverse projection transform
   Pen? mBGPen, mBLPen, mPLPen; // Pen for Background, BendLine, PLine
   List<Point2>? mSnapSource; // vertices of the plines and bend lines to set the snap point on mouse move
   TextBlock? mCords;
   ToolTip? mToolTip;
   Part? mPart;
   ProcessedPart? mProcessedPart;
   List<Line>? mLines;
   #endregion
}
#endregion