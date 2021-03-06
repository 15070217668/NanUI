﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NetDimension.NanUI.Internal
{
	internal class NonclientNativeWindow : NativeWindow
	{
		int _iCaptionHeight = 30;
		private int _iFrameHeight = 8;
		private int _iFrameWidth = 8;

		private bool _bStoreSize = false;
		private bool _bResetSize = false;


		protected IntPtr FormHandle
		{
			private set;
			get;
		}

		protected HtmlUIForm ParentForm
		{
			private set;
			get;
		}
		public NonclientNativeWindow(HtmlUIForm form)
		{
			ParentForm = form;
			FormHandle = form.Handle;

			AssignHandle(FormHandle);
			RecalculateSize();
			InvalidateWindow();
			RecalculateSize();


		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case NativeMethods.WindowsMessage.WM_NCACTIVATE:
					if (m.WParam == IntPtr.Zero)
						m.Result = NativeMethods.MESSAGE_HANDLED;
					InvalidateWindow();
					break;
				case NativeMethods.WindowsMessage.WM_SETCURSOR:
				case NativeMethods.WindowsMessage.WM_ACTIVATEAPP:
					{
						base.WndProc(ref m);
						InvalidateWindow();
					}
					break;
				case NativeMethods.WindowsMessage.WM_NCUAHDRAWCAPTION:
				case NativeMethods.WindowsMessage.WM_NCUAHDRAWFRAME:
					{
						InvalidateWindow();
					}
					break;
				case NativeMethods.WindowsMessage.WM_NCPAINT:
					if (NativeMethods.IsWindowVisible(FormHandle))
					{
						DrawWindow(m.WParam);
						m.Result = NativeMethods.MESSAGE_HANDLED;
					}
					break;
				case NativeMethods.WindowsMessage.WM_NCCALCSIZE:
					if (m.WParam != IntPtr.Zero)
					{
						NativeMethods.NCCALCSIZE_PARAMS ncsize = (NativeMethods.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.NCCALCSIZE_PARAMS));
						NativeMethods.WINDOWPOS wp = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(ncsize.lppos, typeof(NativeMethods.WINDOWPOS));
						// store original frame sizes
						if (!_bStoreSize)
						{
							_bStoreSize = true;
							_iCaptionHeight = ncsize.rect2.Top - ncsize.rect0.Top;
							_iFrameHeight = ncsize.rect0.Bottom - ncsize.rect2.Bottom;
							_iFrameWidth = ncsize.rect2.Left - ncsize.rect0.Left;
						}
						if (!_bResetSize)
						{
							ncsize.rect0 = CalculateFrameSize(wp.x, wp.y, wp.cx, wp.cy);
							ncsize.rect1 = ncsize.rect0;
						}
						Marshal.StructureToPtr(ncsize, m.LParam, false);
						m.Result = (IntPtr)0x400;//WVR_VALIDRECTS;
					}
					else
					{
						NativeMethods.RECT rc = (NativeMethods.RECT)m.GetLParam(typeof(NativeMethods.RECT));
						rc = CalculateFrameSize(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top); ;
						Marshal.StructureToPtr(rc, m.LParam, true);
						m.Result = IntPtr.Zero;//MESSAGE_PROCESS;
					}

					base.WndProc(ref m);


					break;
				default:
					base.WndProc(ref m);
					break;
			}


		}

		private NativeMethods.RECT CalculateFrameSize(int x, int y, int cx, int cy)
		{
			NativeMethods.RECT windowRect = new NativeMethods.RECT(x, y, x + cx, y + cy);
			// subtract original frame size
			windowRect.Left -= _iFrameWidth;
			windowRect.Right += _iFrameWidth;
			windowRect.Top -= _iCaptionHeight;
			windowRect.Bottom += _iFrameHeight;
			// reset client area with new size
			windowRect.Left += 1;
			windowRect.Right -= 1;
			windowRect.Bottom -= 1;
			windowRect.Top += 1;

			NativeMethods.RECT clientRect = new NativeMethods.RECT(windowRect.Left, windowRect.Top, windowRect.Right, windowRect.Bottom);




			return windowRect;
		}

		private void InvalidateWindow()
		{
			NativeMethods.RedrawWindow(FormHandle, IntPtr.Zero, IntPtr.Zero, NativeMethods.RedrawWindowFlags.RDW_FRAME | NativeMethods.RedrawWindowFlags.RDW_UPDATENOW | NativeMethods.RedrawWindowFlags.RDW_INVALIDATE | NativeMethods.RedrawWindowFlags.RDW_ERASE);
		}

		private void RecalculateSize()
		{
			NativeMethods.SetWindowPos(FormHandle, IntPtr.Zero,
				0, 0, 0, 0,
				NativeMethods.SetWindowPosFlags.SWP_FRAMECHANGED | NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | NativeMethods.SetWindowPosFlags.SWP_NOMOVE | NativeMethods.SetWindowPosFlags.SWP_NOSIZE | NativeMethods.SetWindowPosFlags.SWP_NOZORDER);
		}

		private void DrawWindow(IntPtr hRgn)
		{
			Region clipRegion = null;
			if (hRgn != (IntPtr)1)
				clipRegion = Region.FromHrgn(hRgn);



			NativeMethods.RECT windowRect = new NativeMethods.RECT();
			NativeMethods.GetWindowRect(FormHandle, ref windowRect);
			NativeMethods.OffsetRect(ref windowRect, -windowRect.Left, -windowRect.Top);

			NativeMethods.RECT clientRect = new NativeMethods.RECT();
			NativeMethods.GetWindowRect(FormHandle, ref clientRect);
			NativeMethods.OffsetRect(ref clientRect, -clientRect.Left, -clientRect.Top);
			NativeMethods.OffsetRect(ref clientRect, -ParentForm.BorderSize, -ParentForm.BorderSize);



			IntPtr hDC = NativeMethods.GetWindowDC(FormHandle);


			try
			{
				using (var g = Graphics.FromHdc(hDC))
				{


					var height = windowRect.Bottom;
					var width = windowRect.Right;

					using (var pen = new Pen(ParentForm.BorderColor, ParentForm.BorderSize))
					{
						g.DrawLine(pen, 0, 0, 0, height);
					}

					using (var pen = new Pen(ParentForm.BorderColor, ParentForm.BorderSize))
					{
						g.DrawLine(pen, 0, 0, width, 0);
					}

					using (var pen = new Pen(ParentForm.BorderColor, ParentForm.BorderSize))
					{
						g.DrawLine(pen, width - ParentForm.BorderSize, 0, width - ParentForm.BorderSize, height);
					}
					using (var pen = new Pen(ParentForm.BorderColor, ParentForm.BorderSize))
					{
						g.DrawLine(pen, 0, height - ParentForm.BorderSize, width, height - ParentForm.BorderSize);
					}



				}
			}
			finally
			{
				NativeMethods.ExcludeClipRect(hDC, -ParentForm.BorderSize, -ParentForm.BorderSize, clientRect.Right + ParentForm.BorderSize, clientRect.Bottom + ParentForm.BorderSize);
				NativeMethods.ReleaseDC(FormHandle, hDC);
			}

		}


	}
}
