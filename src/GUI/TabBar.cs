﻿// NClass - Free class diagram editor
// Copyright (C) 2006-2009 Balazs Tihanyi
// 
// This program is free software; you can redistribute it and/or modify it under 
// the terms of the GNU General Public License as published by the Free Software 
// Foundation; either version 3 of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful, but WITHOUT 
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS 
// FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with 
// this program; if not, write to the Free Software Foundation, Inc., 
// 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Windows.Forms;
using NClass.DiagramEditor;
using NClass.Translations;

namespace NClass.GUI
{
	public partial class TabBar : Control
	{
		private class Tab
		{
			const int MinWidth = 60;
			const int TextMargin = 20;
		    public const int IconMargin = 20;

			IDocument document;
			TabBar parent;
			string text;
			float textWidth;

			public Tab(IDocument document, TabBar parent)
			{
				this.document = document;
				this.parent = parent;
				document.Renamed += document_Renamed;
				text = document.Name;
				textWidth = MeasureWidth(text);
			}

			private void document_Renamed(object sender, EventArgs e)
			{
				Text = document.Name;
			}

			public IDocument Document
			{
				get { return document; }
			}

			public string Text
			{
				get
				{
					return text;
				}
				private set
				{
					if (text != value)
					{
						text = value;
						textWidth = MeasureWidth(text);
						parent.Invalidate();
					}
				}
			}

			public float TextWidth
			{
				get { return textWidth; }
			}

			public int Width
			{
				get
				{
					return Math.Max(MinWidth, (int) TextWidth + TextMargin + IconMargin);
				}
			}

			public bool IsActive
			{
				get { return (Document == parent.docManager.ActiveDocument); }
			}

			public void Detached()
			{
				document.Renamed -= document_Renamed;
			}

			private float MeasureWidth(string text)
			{
				Graphics g = parent.CreateGraphics();

				SizeF textSize = g.MeasureString(text, parent.activeTabFont,
					parent.MaxTabWidth, parent.stringFormat);
				g.Dispose();

				return textSize.Width;
			}

			public override string ToString()
			{
				return text;
			}

		    public Rectangle Bounds { get; private set; }

		    public bool IsClosingSignActive { get; private set; }

		    public void Draw(Graphics g, Rectangle tabRectangle, Brush activeTabBrush, Brush inactiveTabBrush,
		        Pen borderPen, Brush textBrush, StringFormat stringFormat, Font activeTabFont, Font inactiveTabFont)
		    {
		        var top = (this.IsActive ? TopMargin : TopMargin + 2);

		        var margin = (tabRectangle.Height - ClosingSignSize) / 2;
		        var closingSignLeft = tabRectangle.Left + tabRectangle.Width - 4 - ClosingSignSize;
                var tabBrush = (this.IsActive ? activeTabBrush : inactiveTabBrush);
		        var imageRectangle = new Rectangle(tabRectangle.Left + 2, top + 2, 16, 16);
		        var tabTextRectangle = new Rectangle(tabRectangle.Left + Tab.IconMargin, top, closingSignLeft - tabRectangle.Left - IconMargin, tabRectangle.Height);
		        var icon = BitmapHelper.GetBitmapForDocument(Document);

		        // To display bottom line for inactive tabs
		        if (!IsActive)
		        {
		            tabRectangle.Height--;
		            tabTextRectangle.Height--;
		            imageRectangle.Height--;
		        }

		        this.Bounds = tabRectangle;

		        g.FillRectangle(tabBrush, tabRectangle); // Draw background
		        g.DrawRectangle(borderPen, tabRectangle); // Draw border

		        var font = (IsActive) ? activeTabFont : inactiveTabFont;
		        g.DrawImage(icon, imageRectangle);
		        g.DrawString(Text, font, textBrush, tabTextRectangle, stringFormat);

		        Color lineColor = IsClosingSignActive ? SystemColors.ControlText : SystemColors.ControlDark;
		        Pen linePen = new Pen(lineColor, 2);

		        g.DrawLine(linePen, closingSignLeft, tabRectangle.Top + margin, closingSignLeft + ClosingSignSize, tabRectangle.Top + margin + ClosingSignSize);
		        g.DrawLine(linePen, closingSignLeft, tabRectangle.Top + margin + ClosingSignSize, closingSignLeft + ClosingSignSize, tabRectangle.Top + margin);
		        linePen.Dispose();
            }

		    private bool IsOverClosingSign(Point location)
		    {
		        var margin = (Bounds.Height - ClosingSignSize) / 2;
		        int closingSignLeft = Bounds.Left + Bounds.Width - 4 - ClosingSignSize;

		        return (
		            location.X >= closingSignLeft && location.X <= closingSignLeft + ClosingSignSize &&
		            location.Y >= margin && location.Y <= margin + ClosingSignSize
		        );
		    }

		    public void OnMouseMove(MouseEventArgs args)
		    {
		        IsClosingSignActive = IsOverClosingSign(args.Location);
                this.parent.Invalidate(Bounds);
		    }
		}


		DocumentManager docManager = null;
		List<Tab> tabs = new List<Tab>();
		Tab activeTab = null;
		Tab grabbedTab = null;
		int originalPosition = 0;
		bool activeCloseButton = false;

		StringFormat stringFormat;
		Font activeTabFont;
		Color borderColor = SystemColors.ControlDark;
		Color activeTabColor = Color.White;
		Color inactiveTabColor = SystemColors.ControlLight;
		int maxTabWidth = 200;

		const int LeftMargin = 3;
		const int TopMargin = 3;
		const int ClosingSignSize = 8;

		public TabBar()
		{
			InitializeComponent();
			UpdateTexts();
		    DoubleBuffered = true;
			this.BackColor = SystemColors.Control;
			this.SetStyle(ControlStyles.Selectable, false);
			this.SetStyle(ControlStyles.ResizeRedraw, true);

			stringFormat = new StringFormat(StringFormat.GenericTypographic);
			stringFormat.Alignment = StringAlignment.Near;
			stringFormat.LineAlignment = StringAlignment.Center;
			stringFormat.Trimming = StringTrimming.EllipsisCharacter;
			stringFormat.FormatFlags |= StringFormatFlags.NoWrap;

			activeTabFont = new Font(Font, FontStyle.Bold);
		}

		private void UpdateTexts()
		{
			mnuClose.Text = Strings.MenuCloseTab;
			mnuCloseAll.Text = Strings.MenuCloseAllTabs;
			mnuCloseAllButThis.Text = Strings.MenuCloseAllTabsButThis;
		}

		[Browsable(false)]
		public DocumentManager DocumentManager
		{
			get
			{
				return docManager;
			}
			set
			{
				if (docManager != value)
				{
					if (docManager != null)
					{
						docManager.ActiveDocumentChanged -= docManager_ActiveDocumentChanged;
						docManager.DocumentAdded -= docManager_DocumentAdded;
						docManager.DocumentRemoved -= docManager_DocumentRemoved;
						docManager.DocumentMoved -= docManager_DocumentMoved;
						ClearTabs();
					}
					docManager = value;
					if (docManager != null)
					{
						docManager.ActiveDocumentChanged += docManager_ActiveDocumentChanged;
						docManager.DocumentAdded += docManager_DocumentAdded;
						docManager.DocumentRemoved += docManager_DocumentRemoved;
						docManager.DocumentMoved += docManager_DocumentMoved;
						CreateTabs();
					}
				}
			}
		}

		[DefaultValue(typeof(Color), "Control")]
		public override Color BackColor
		{
			get
			{
				if (Parent != null && !DesignMode &&
					(docManager == null || !docManager.HasDocument))
				{
					return Parent.BackColor;
				}
				else
				{
					return base.BackColor;
				}
			}
			set
			{
				base.BackColor = value;
			}
		}

		[DefaultValue(typeof(Color), "ControlDark")]
		public Color BorderColor
		{
			get { return borderColor; }
			set { borderColor = value; }
		}

		[DefaultValue(typeof(Color), "White")]
		public Color ActiveTabColor
		{
			get { return activeTabColor; }
			set { activeTabColor = value; }
		}

		[DefaultValue(typeof(Color), "ControlLight")]
		public Color InactiveTabColor
		{
			get { return inactiveTabColor; }
			set { inactiveTabColor = value; }
		}

		[DefaultValue(200)]
		public int MaxTabWidth
		{
			get
			{
				return maxTabWidth;
			}
			set
			{
				maxTabWidth = value;
				if (maxTabWidth < 50)
					maxTabWidth = 50;
			}
		}

		protected override Size DefaultSize
		{
			get
			{
				return new Size(100, 25);
			}
		}

		private void CreateTabs()
		{
			foreach (IDocument doc in docManager.Documents)
			{
				Tab tab = new Tab(doc, this);
				tabs.Add(tab);
				if (doc == docManager.ActiveDocument)
					activeTab = tab;
			}
		}

		private void ClearTabs()
		{
			foreach (Tab tab in tabs)
				tab.Detached();
			tabs.Clear();
			activeTab = null;
		}

		private void docManager_ActiveDocumentChanged(object sender, DocumentEventArgs e)
		{
			foreach (Tab tab in tabs)
			{
				if (tab.Document == docManager.ActiveDocument)
				{
					activeTab = tab;
					break;
				}
			}
			this.Invalidate();
		}

		private void docManager_DocumentAdded(object sender, DocumentEventArgs e)
		{
			Tab tab = new Tab(e.Document, this);
			tabs.Add(tab);
		}

		private void docManager_DocumentRemoved(object sender, DocumentEventArgs e)
		{
			for (int index = 0; index < tabs.Count; index++)
			{
				if (tabs[index].Document == e.Document)
				{
					tabs.RemoveAt(index);
					this.Invalidate();
					return;
				}
			}
		}

		private void docManager_DocumentMoved(object sender, DocumentMovedEventArgs e)
		{
			Tab tab = tabs[e.OldPostion];

			if (e.NewPosition > e.OldPostion)
			{
				for (int i = e.OldPostion; i < e.NewPosition; i++)
					tabs[i] = tabs[i + 1];
			}
			else // e.NewPosition < e.OldPostion
			{
				for (int i = e.OldPostion; i > e.NewPosition; i--)
					tabs[i] = tabs[i - 1];
			}
			tabs[e.NewPosition] = tab;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			Tab selectedTab = PickTab(e.Location);
			if (selectedTab != null)
			{
				if (e.Button == MouseButtons.Middle || selectedTab.IsClosingSignActive)
				{
					docManager.Close(selectedTab.Document);
				}
				else
				{
					docManager.ActiveDocument = selectedTab.Document;
					grabbedTab = selectedTab;
					originalPosition = e.X;
				}
			}
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			base.OnMouseDoubleClick(e);

			Tab selectedTab = PickTab(e.Location);
			if (selectedTab != null && e.Button == MouseButtons.Left)
			{
				docManager.Close(selectedTab.Document);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button == MouseButtons.Left && grabbedTab != null)
			{
				MoveTab(grabbedTab, e.Location);
			}

		    var tab = PickTab(e.Location);
		    tab?.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			grabbedTab = null;
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			if (activeCloseButton)
			{
				activeCloseButton = false;
				Invalidate();
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			activeTabFont.Dispose();
			activeTabFont = new Font(Font, FontStyle.Bold);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (tabs.Count > 0)
			{
				DrawTabs(e.Graphics);
			}
		}

		private Tab PickTab(Point point)
		{
			int x = LeftMargin;

			foreach (Tab tab in tabs)
			{
				if (point.X >= x && point.X < x + tab.Width)
					return tab;
				x += (int) tab.Width;
			}
			return null;
		}

		private void MoveTab(Tab grabbedTab, Point destination)
		{
			Tab newNeighbourTab = PickTab(destination);

			if (newNeighbourTab == grabbedTab)
			{
				originalPosition = destination.X;
			}
			else if (newNeighbourTab != null)
			{
				int oldIndex = tabs.IndexOf(grabbedTab);
				int newIndex = tabs.IndexOf(newNeighbourTab);

				if (newIndex > oldIndex) // Moving right
				{
					if (destination.X >= originalPosition)
						docManager.MoveDocument(grabbedTab.Document, newIndex - oldIndex);
				}
				else if (newIndex < oldIndex) // Moving left
				{
					if (destination.X <= originalPosition)
						docManager.MoveDocument(grabbedTab.Document, newIndex - oldIndex);
				}
			}
		}

        private void DrawTabs(Graphics g)
        {
            var borderPen = new Pen(BorderColor);
            var activeTabBrush = new SolidBrush(ActiveTabColor);
            var inactiveTabBrush = new LinearGradientBrush(
                new Rectangle(0, 5, Width, Height - 1), ActiveTabColor,
                SystemColors.ControlLight, LinearGradientMode.Vertical);
            var left = LeftMargin;

            var textBrush = ForeColor.IsKnownColor ? SystemBrushes.FromSystemColor(ForeColor) : new SolidBrush(ForeColor);

            g.DrawLine(borderPen, 0, Height - 1, left, Height - 1);
            foreach (Tab tab in tabs)
            {
                var top = (tab.IsActive ? TopMargin : TopMargin + 2);
                var tabRectangle = new Rectangle(left, top, tab.Width, Height - top);
                tab.Draw(g, tabRectangle, activeTabBrush, inactiveTabBrush, borderPen, textBrush, stringFormat, activeTabFont, Font);
                left += tab.Width;
            }
            g.DrawLine(borderPen, left, Height - 1, Width - 1, Height - 1);

            borderPen.Dispose();
            if (!ForeColor.IsKnownColor)
                textBrush.Dispose();
            activeTabBrush.Dispose();
            inactiveTabBrush.Dispose();
        }

		private void contextMenu_Opening(object sender, CancelEventArgs e)
		{
			if (docManager == null || !docManager.HasDocument)
			{
				e.Cancel = true;
			}
		}

		private void mnuClose_Click(object sender, EventArgs e)
		{
			if (docManager != null && activeTab != null)
			{
				docManager.Close(activeTab.Document);
			}
		}

		private void mnuCloseAll_Click(object sender, EventArgs e)
		{
			if (docManager != null)
				docManager.CloseAll();
		}

		private void mnuCloseAllButThis_Click(object sender, EventArgs e)
		{
			if (docManager != null && activeTab != null)
			{
				docManager.CloseAllOthers(activeTab.Document);
			}
		}
	}
}
