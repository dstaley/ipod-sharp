
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace IPod {

    internal class Initializer {
        private static bool inited = false;

        private static IntPtr mainContext;

        [DllImport ("libglib-2.0.so.0")]
        private static extern IntPtr g_main_context_new ();

        [DllImport ("libglib-2.0.so.0")]
        private static extern void g_main_context_iteration (IntPtr ctx, bool mayblock);

        [DllImport ("libglib-2.0.so.0")]
        private static extern bool g_main_context_pending (IntPtr ctx);

        [DllImport ("ipoddevice")]
        private static extern void ipod_device_set_global_main_context (IntPtr ctx);
        
        [DllImport ("libdbus-glib-1.so.1")]
        private static extern void dbus_g_thread_init ();
        
        public static void Init () {
            lock (typeof (Initializer)) {
                if (inited)
                    return;
                
                dbus_g_thread_init ();
                Gtk.Application.Init ();
                GLib.GType.Register (Device.GType, typeof (Device));
                GLib.GType.Register (DeviceEventListener.GType, typeof (DeviceEventListener));
                
                mainContext = g_main_context_new ();
                ipod_device_set_global_main_context (mainContext);
                
                Thread mainThread = new Thread (new ThreadStart (MainLoopThread));
                mainThread.IsBackground = true;
                mainThread.Start ();

                inited = true;
            }
        }

        private static void MainLoopThread () {
            while (true) {
                try {
                    while (g_main_context_pending (mainContext))
                        g_main_context_iteration (mainContext, false);

                    Thread.Sleep (10);
                } catch (Exception e) {
                    Console.Error.WriteLine ("Error in event listener loop: " + e);
                }
            }
        }
    }
}
