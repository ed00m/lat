// 
// lat - AddEntryDialog.cs
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
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class AddEntryDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog addEntryDialog;
		[Glade.Widget] Gtk.Entry dnNameEntry;
		[Glade.Widget] Gtk.Entry attrNameEntry;
		[Glade.Widget] Gtk.Entry attrValueEntry;
		[Glade.Widget] TreeView attrListview; 
		[Glade.Widget] Gtk.Button addAttributeButton;
		[Glade.Widget] Gtk.Button removeAttributeButton;
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private ListStore attrListStore;

		private ArrayList _attributes;

		private Connection _conn;

		private string _dn;

		public AddEntryDialog (Connection conn)
		{
			_attributes = new ArrayList ();
			_conn = conn;

			ui = new Glade.XML (null, "lat.glade", "addEntryDialog", null);
			ui.Autoconnect (this);
			
			attrListStore = new ListStore (typeof (string), typeof (string));
			attrListview.Model = attrListStore;
			
			TreeViewColumn col;
			col = attrListview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			col = attrListview.AppendColumn ("Value", new CellRendererText (), "text", 1);
			col.SortColumnId = 1;

			attrListStore.SetSortColumnId (0, SortType.Ascending);
		
			addAttributeButton.Clicked += new EventHandler (OnAddAttributeClicked);
			removeAttributeButton.Clicked += new EventHandler (OnRemoveAttributeClicked);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			addEntryDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);

			addEntryDialog.Resize (300, 450);

			addEntryDialog.Run ();
			addEntryDialog.Destroy ();
		}

		private void OnAddAttributeClicked (object o, EventArgs args)
		{
			attrListStore.AppendValues (attrNameEntry.Text, attrValueEntry.Text);
			attrNameEntry.Text = "";
			attrValueEntry.Text = "";
		}

		private void OnRemoveAttributeClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (attrListview.Selection.GetSelected (out model, out iter)) 
			{
				attrListStore.Remove (ref iter);
			}

		}

		private bool attrForeachFunc (TreeModel model, TreePath path, TreeIter iter)
		{
			if (!attrListStore.IterIsValid (iter))
				return true;

			string _name = null;
			string _value = null;

			_name = (string) attrListStore.GetValue (iter, 0);
			_value = (string) attrListStore.GetValue (iter, 1);

			if (_name == null || _value == null)
				return false;

			LdapAttribute attr = new LdapAttribute (_name, _value);

			_attributes.Add (attr);

			return false;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			_dn = dnNameEntry.Text;
			
			attrListStore.Foreach (new TreeModelForeachFunc (attrForeachFunc));

			Util.AddEntry (_conn, addEntryDialog, _dn, _attributes);

			addEntryDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			addEntryDialog.HideAll ();
		}

		private void OnDlgDelete (object o, DeleteEventArgs args)
		{
			addEntryDialog.HideAll ();
		}
	}
}