// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace GnomeKeyring {

	using System;
	using System.Collections;
	using System.Runtime.InteropServices;

#region Autogenerated code
	[StructLayout(LayoutKind.Sequential)]
	public struct Attribute {

		public string Name;
		public GnomeKeyring.AttributeType Type;
		public string Value;

		public static GnomeKeyring.Attribute Zero = new GnomeKeyring.Attribute ();

		public static GnomeKeyring.Attribute New(IntPtr raw) {
			if (raw == IntPtr.Zero) {
				return GnomeKeyring.Attribute.Zero;
			}
			GnomeKeyring.Attribute self = new GnomeKeyring.Attribute();
			self = (GnomeKeyring.Attribute) Marshal.PtrToStructure (raw, self.GetType ());
			return self;
		}

//		private static GLib.GType GType {
//			get { return GLib.GType.Pointer; }
//		}
#endregion
	}
}