//
// FileServiceEventsPad.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using AppKit;
using MonoDevelop.Components;
using MonoDevelop.Components.Declarative;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components.LogView;

namespace MonoDevelop.FileServiceEvents
{
	class FileServiceEventsPad : PadContent
	{
		LogViewController logViewController;
		LogViewProgressMonitor progressMonitor;
		NSView logView;
		ToolbarButtonItem startButton;
		ToolbarButtonItem stopButton;
		ToolbarButtonItem clearButton;
		bool enabled;

		public FileServiceEventsPad ()
		{
		}

		protected override void Initialize (IPadWindow window)
		{
			logViewController = new LogViewController ("LogMonitor");
			logView = logViewController.Control.GetNativeWidget<NSView> ();

			progressMonitor = (LogViewProgressMonitor)logViewController.GetProgressMonitor ();

			var toolbar = new PadToolbar ();

			startButton = new ToolbarButtonItem (toolbar.Properties, nameof (startButton));
			startButton.Icon = Stock.RunProgramIcon;
			startButton.Clicked += StartButtonClicked;
			startButton.Tooltip = GettextCatalog.GetString ("Start monitoring file events");
			toolbar.AddItem (startButton);

			stopButton = new ToolbarButtonItem (toolbar.Properties, nameof (stopButton));
			stopButton.Icon = Stock.Stop;
			stopButton.Clicked += StopButtonClicked;
			stopButton.Tooltip = GettextCatalog.GetString ("Stop monitoring file events");
			// Cannot disable the button before the underlying NSView is created.
			//stopButton.Enabled = false;
			toolbar.AddItem (stopButton);

			clearButton = new ToolbarButtonItem (toolbar.Properties, nameof (clearButton));
			clearButton.Icon = Stock.Clear;
			clearButton.Clicked += ButtonClearClicked;
			clearButton.Tooltip = GettextCatalog.GetString ("Clear");
			toolbar.AddItem (clearButton);

			window.SetToolbar (toolbar, DockPositionType.Right);

			stopButton.Enabled = false;
		}

		public override Control Control {
			get { return logView; }
		}

		void StartButtonClicked (object sender, EventArgs e)
		{
			enabled = true;
			startButton.Enabled = false;
			stopButton.Enabled = true;

			OnEnabledChanged ();
		}

		void StopButtonClicked (object sender, EventArgs e)
		{
			enabled = false;
			startButton.Enabled = true;
			stopButton.Enabled = false;

			OnEnabledChanged ();
		}

		void OnEnabledChanged ()
		{
			if (enabled) {
				FileService.FileCreated += FileCreated;
				FileService.FileRemoved += FileRemoved;
				FileService.FileMoved += FileRenamed;
				FileService.FileChanged += FileChanged;
			} else {
				FileService.FileCreated -= FileCreated;
				FileService.FileRemoved -= FileRemoved;
				FileService.FileMoved -= FileRenamed;
				FileService.FileChanged -= FileChanged;
			}
		}

		void FileChanged (object sender, FileEventArgs e)
		{
			foreach (FileEventInfo info in e) {
				WriteText ("FileChanged: " + info.FileName);
			}
		}

		void FileRenamed (object sender, FileCopyEventArgs e)
		{
			foreach (FileCopyEventInfo info in e) {
				WriteText (string.Format ("FileRenamed: {0} -> {1}", info.SourceFile, info.TargetFile));
			}
		}

		void FileRemoved (object sender, FileEventArgs e)
		{
			foreach (FileEventInfo info in e) {
				WriteText ("FileRemoved: " + info.FileName);
			}
		}

		void FileCreated (object sender, FileEventArgs e)
		{
			foreach (FileEventInfo info in e) {
				WriteText ("FileCreated: " + info.FileName);
			}
		}

		void ButtonClearClicked (object sender, EventArgs e)
		{
			logViewController.Clear ();
		}

		void WriteText (string message)
		{
			string fullMessage = string.Format ("{0}: {1}{2}", DateTime.Now.ToString ("u"), message, Environment.NewLine);
			Runtime.RunInMainThread (() => {
				logViewController.WriteText (progressMonitor, fullMessage);
			});
		}

		public override void Dispose ()
		{
			if (startButton != null) {
				startButton.Clicked -= StartButtonClicked;
			}
			if (stopButton != null) {
				stopButton.Clicked -= StopButtonClicked;
			}
			if (clearButton != null) {
				clearButton.Clicked -= ButtonClearClicked;
			}

			base.Dispose ();
		}
	}
}
