// 
// lat - SelectContainerDialog.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
//

using Gtk;
using GLib;
using Glade;
using System;

namespace lat 
{
	public class SelectContainerDialog
	{
		[Glade.Widget] Gtk.Dialog selectContainerDialog;
		[Glade.Widget] ScrolledWindow browserScrolledWindow;
		[Glade.Widget] Gtk.Label msgLabel;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Button cancelButton;

		private Glade.XML ui;
		private LdapTreeView _ldapTreeview;

		private string _dn = "";

		public SelectContainerDialog (lat.Connection conn, Gtk.Window parent)
		{
			ui = new Glade.XML (null, "lat.glade", "selectContainerDialog", null);
			ui.Autoconnect (this);

			_ldapTreeview = new LdapTreeView (conn, parent);
			_ldapTreeview.dnSelected += new dnSelectedHandler (ldapDNSelected);

			browserScrolledWindow.AddWithViewport (_ldapTreeview);
			browserScrolledWindow.Show ();

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			selectContainerDialog.Resize (350, 400);
		}

		public void Run ()
		{
			selectContainerDialog.Run ();
			selectContainerDialog.Destroy ();
		}

		public string DN
		{
			get { return _dn; }
		}

		public string Title
		{
			set { selectContainerDialog.Title = value; }
		}

		public string Message
		{
			set 
			{
				msgLabel.Markup = String.Format ("<span size=\"larger\">{0}</span>", value); 
			}
		}

		private void ldapDNSelected (object o, dnSelectedEventArgs args)
		{
			if (args.IsHost)
			{
				return;
			}

			_dn = args.DN;

			selectContainerDialog.HideAll ();
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			_dn = _ldapTreeview.getSelectedDN ();

			selectContainerDialog.HideAll ();
		}
	
		private void OnCancelClicked (object o, EventArgs args)
		{
			selectContainerDialog.HideAll ();
		}
	}
}