using System;
using System.Runtime.InteropServices;

namespace Linage.GUI.Helpers
{
    public static class NativeMethods
    {
        public const int WM_USER = 0x400;
        public const int EM_GETSCROLLPOS = WM_USER + 221;
        public const int EM_SETSCROLLPOS = WM_USER + 222;
        public const int WM_VSCROLL = 0x115;
        public const int SB_THUMBPOSITION = 4;
        public const int WM_SETREDRAW = 0x000B;
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, ref Point pt);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        public static void Scroll(IntPtr handle, Point pt)
        {
            SendMessage(handle, EM_SETSCROLLPOS, 0, ref pt);
        }

        public static Point GetScrollPos(IntPtr handle)
        {
            Point pt = new Point();
            SendMessage(handle, EM_GETSCROLLPOS, 0, ref pt);
            return pt;
        }

        public static void SuspendDrawing(System.Windows.Forms.Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, (IntPtr)0, (IntPtr)0);
        }

        public static void ResumeDrawing(System.Windows.Forms.Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, (IntPtr)1, (IntPtr)0);
            parent.Refresh();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        public const int SB_VERT = 1;
        public const uint SIF_RANGE = 0x1;
        public const uint SIF_PAGE = 0x2;
        public const uint SIF_POS = 0x4;
        public const uint SIF_ALL = SIF_RANGE | SIF_PAGE | SIF_POS;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
    }
}
