
using System;
using System.Collections.Generic;
using Gtk;

namespace IPod {

    public class DeviceCombo : ComboBox {

        private ListStore store;
        private DeviceManager manager;

        private List<Device> devices = new List<Device> ();

        public Device ActiveDevice {
            get {
                TreeIter iter = TreeIter.Zero;

                if (!GetActiveIter (out iter))
                    return null;
                
                return (Device) store.GetValue (iter, 1);
            }
        }

        public DeviceCombo () : base () {
            store = new ListStore (typeof (string), typeof (Device));
            this.Model = store;

            CellRendererText renderer = new CellRendererText ();
            this.PackStart (renderer, false);
            
            this.AddAttribute (renderer, "text", 0);

            Refresh ();

            manager = DeviceManager.Create ();
            foreach (Device device in manager.Devices) {
                AddDevice (device);
            }
            SetActive ();
            manager.DeviceAdded += OnDeviceAdded;
            manager.DeviceRemoved += OnDeviceRemoved;
        }

        private void OnDeviceAdded (object o, DeviceArgs args) {
            AddDevice (args.Device);
                
            if (ActiveDevice == null) {
                SetActive ();
            }
        }

        private void OnDeviceRemoved (object o, DeviceArgs args) {
            RemoveDevice (args.Device);
        }

        private void OnDeviceChanged (object o, EventArgs args) {
            Device device = o as Device;
            TreeIter iter = FindDevice (device.VolumeInfo.MountPoint);
            
            if (!iter.Equals (TreeIter.Zero)) {
                store.SetValue (iter, 0, device.Name);
                store.EmitRowChanged (store.GetPath (iter), iter);
            }
        }

        private TreeIter FindDevice (string mount) {
            TreeIter iter = TreeIter.Zero;

            if (!store.GetIterFirst (out iter))
                return TreeIter.Zero;
            
            do {
                Device device = (Device) store.GetValue (iter, 1);

                if (device != null) {
                    
                    if (device.VolumeInfo.MountPoint == mount)
                        return iter;
                }

            } while (store.IterNext (ref iter));

            return TreeIter.Zero;
        }

        private void ClearPlaceholder () {
            TreeIter iter = TreeIter.Zero;

            if (!store.GetIterFirst (out iter))
                return;
            
            do {
                Device device = (Device) store.GetValue (iter, 1);
                if (device == null) {
                    store.Remove (ref iter);
                    break;
                }
            } while (store.IterNext (ref iter));
        }

        private void Refresh () {
            string current = null;
            bool haveActive = false;
            
            if (ActiveDevice != null)
                current = ActiveDevice.VolumeInfo.MountPoint;
            
            store.Clear ();

            if (!haveActive) {
                SetActive ();
            }
        }

        private void SetActive () {
            if (store.IterNChildren () == 0) {
                store.AppendValues ("No iPod Found", null);
                this.Active = 0;
            } else {
                this.Active = 0;
            }
        }

        private TreeIter AddDevice (Device device) {
            ClearPlaceholder ();

            device.Changed += OnDeviceChanged;

            // gtk-sharp doesn't ensure that I will get the same managed
            // instance when pulling this out of the store, so hold a
            // ref to it here
            devices.Add (device);

            return store.AppendValues (device.Name, device);
        }

        private void RemoveDevice (Device device) {
            TreeIter iter = FindDevice (device.VolumeInfo.MountPoint);

            if (iter.Equals (TreeIter.Zero))
                return;

            device.Changed -= OnDeviceChanged;
            devices.Remove (device);
            
            if (!iter.Equals (TreeIter.Zero)) {
                store.Remove (ref iter);
            }

            SetActive ();
        }
    }
}
