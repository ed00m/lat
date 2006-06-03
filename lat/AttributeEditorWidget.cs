// 
// lat - AttributeEditorWidget.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 
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

using System;
using System.Collections;
using System.Collections.Specialized;
using Gtk;
using GLib;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class AttributeEditorWidget : Gtk.VBox
	{
		ScrolledWindow sw;
		Button applyButton;
		TreeView tv;
		ListStore store;

		LdapServer currentServer;
		string currentDN;
		
		ArrayList allAttrs;
		NameValueCollection currentAttributes;

		public AttributeEditorWidget() : base ()
		{
			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;

			store = new ListStore (typeof (string), typeof(string));
			store.SetSortColumnId (0, SortType.Ascending);
			
			tv = new TreeView ();
			tv.Model = store;
			
			TreeViewColumn col;
			col = tv.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);
			
			tv.AppendColumn ("Value", cell, "text", 1);						
			
			tv.KeyPressEvent += new KeyPressEventHandler (OnKeyPress);
			tv.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
			
			sw.AddWithViewport (tv);			
					
			HButtonBox hb = new HButtonBox ();			
			hb.Layout = ButtonBoxStyle.End;
			
			applyButton = new Button ();
			applyButton.Label = "Apply";
			applyButton.Image = new Gtk.Image (Stock.Apply, IconSize.Button);
			applyButton.Clicked += new EventHandler (OnApplyClicked);
			applyButton.Sensitive = false;
			
			hb.Add (applyButton);
			
			this.PackStart (sw, true, true, 0);
			this.PackStart (hb, false, false, 5);
		
			this.ShowAll ();
		}

		void OnApplyClicked (object o, EventArgs args)
		{
			ArrayList modList = new ArrayList ();
			NameValueCollection newAttributes = new NameValueCollection ();
			
			foreach (object[] row in this.store) {
			
				string newValue = row[1].ToString();
				
				if (newValue == "" || newValue == null)
					continue;
					
				newAttributes.Add (row[0].ToString(), newValue);
			}

			foreach (string key in newAttributes.AllKeys) {
			
				string[] newValues = newAttributes.GetValues(key);
				string[] oldValues = currentAttributes.GetValues (key);
				LdapAttribute la = new LdapAttribute (key, newValues);
				
				if (oldValues == null) {					
					LdapModification lm = new LdapModification (LdapModification.ADD, la);
					modList.Add (lm);
				} else {
										
					foreach (string nv in newValues) {
					
						bool foundMatch = false;
						foreach (string ov in oldValues)
							if (ov == nv)
								foundMatch = true;
								
						if (!foundMatch) {
							LdapModification lm = new LdapModification (LdapModification.REPLACE, la);
							modList.Add (lm);
						}
					}
				}				
			}

			foreach (string key in currentAttributes.AllKeys) {
				string[] newValues = newAttributes.GetValues (key);
								
				if (newValues == null) {
					string[] oldValues = currentAttributes.GetValues (key);
					LdapAttribute la = new LdapAttribute (key, oldValues);
					LdapModification lm = new LdapModification (LdapModification.DELETE, la);
					modList.Add (lm);
				} else {
					LdapAttribute la = new LdapAttribute (key, newValues);
					LdapModification lm = new LdapModification (LdapModification.REPLACE, la);
					modList.Add (lm);
				}
			}

			Util.ModifyEntry (currentServer, null, currentDN, modList, Global.VerboseMessages);
		}
	
		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!store.GetIterFromString (out iter, args.Path))
				return;

			string oldText = (string) store.GetValue (iter, 1);
			if (oldText == args.NewText)
				return;
				
			store.SetValue (iter, 1, args.NewText);
			applyButton.Sensitive = true;
		}
	
		public void Show (LdapServer server, LdapEntry entry, bool showAll)
		{
			currentServer = server;
			currentDN = entry.DN;
			currentAttributes = new NameValueCollection ();

			// FIXME: crashes after an apply if I don't re-create the store;	
			store = new ListStore (typeof (string), typeof(string));
			store.SetSortColumnId (0, SortType.Ascending);
			tv.Model = store;
			
//			store.Clear ();
		
			allAttrs = new ArrayList ();
			LdapAttribute a = entry.getAttribute ("objectClass");

			for (int i = 0; i < a.StringValueArray.Length; i++) {
			
				string o = (string) a.StringValueArray[i];	
				store.AppendValues ("objectClass", o);
				currentAttributes.Add ("objectClass", o);
				
				string[] attrs = server.GetAllAttributes (o);				
				foreach (string at in attrs)
					if (!allAttrs.Contains (at))
						allAttrs.Add (at);
			}
			
			LdapAttributeSet attributeSet = entry.getAttributeSet ();

			foreach (LdapAttribute attr in attributeSet) {

				if (allAttrs.Contains (attr.Name))
					allAttrs.Remove (attr.Name);

				if (attr.Name.ToLower() == "objectclass")
					continue;

				try {
				
					foreach (string s in attr.StringValueArray) {
						store.AppendValues (attr.Name, s);
						currentAttributes.Add (attr.Name, s);
					}
					
				} catch (ArgumentOutOfRangeException e) {					
					// FIXME: this only happens with gmcs
					store.AppendValues (attr.Name, "");
					Logger.Log.Debug ("Show attribute arugment out of range: {0}", attr.Name);
				}
			}

			if (!showAll)
				return;

			foreach (string n in allAttrs)
				store.AppendValues (n, "");
		}
		
		void InsertAttribute ()
		{
			string attrName;
			TreeModel model;
			TreeIter iter;

			if (!tv.Selection.GetSelected (out model, out iter))
				return;
				
			attrName = (string) store.GetValue (iter, 0);
						
			if (attrName == null)
				return;
								
			SchemaParser sp = currentServer.GetAttributeTypeSchema (attrName);
			
			if (!sp.Single) {
				TreeIter newRow = store.InsertAfter (iter);
				store.SetValue (newRow, 0, attrName);
				applyButton.Sensitive = true;
			} else {
			
				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Unable to insert value",
					"Multiple values not supported by this attribute");

				dialog.Run ();
				dialog.Destroy ();
			}						
		}
		
		void DeleteAttribute ()
		{
			TreeModel model;
			TreeIter iter;

			if (!tv.Selection.GetSelected (out model, out iter))
				return;
				
			store.Remove (ref iter);
			applyButton.Sensitive = true;
		}
		
		void OnKeyPress (object o, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			
			case Gdk.Key.Insert:
			case Gdk.Key.KP_Insert:
				InsertAttribute ();
				break;
				
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
				DeleteAttribute ();
				break;
				
			default:
				break;
			}				
		}
		
		[ConnectBefore]
		void OnRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		void OnInsertActivate (object o, EventArgs args)
		{
			InsertAttribute ();
		}
		
		void OnDeleteActivate (object o, EventArgs args)
		{
			DeleteAttribute ();
		}

		void OnAddObjectClassActivate (object o, EventArgs args)
		{
			AddObjectClassDialog dlg = new AddObjectClassDialog (currentServer);
				
			foreach (string s in dlg.ObjectClasses) {
				string[] req = currentServer.GetRequiredAttrs (s);
				store.AppendValues ("objectClass", s);
				
				foreach (string r in req) {
					if (allAttrs.Contains (r))
						allAttrs.Remove (r);						

					string m = currentAttributes[r];
					if (m == null) {
						store.AppendValues (r, "");
						currentAttributes.Add (r, "");
					}
				}
			}
		}

		void DoPopUp()
		{
			Menu popup = new Menu();

			ImageMenuItem newObjectClassItem = new ImageMenuItem ("Add object class(es)");
			newObjectClassItem.Image = new Gtk.Image (Stock.Add, IconSize.Menu);
			newObjectClassItem.Activated += new EventHandler (OnAddObjectClassActivate);
			newObjectClassItem.Show ();
			popup.Append (newObjectClassItem);

			ImageMenuItem newItem = new ImageMenuItem ("Insert attribute");
			newItem.Image = new Gtk.Image (Stock.New, IconSize.Menu);
			newItem.Activated += new EventHandler (OnInsertActivate);			
			newItem.Show ();
			popup.Append (newItem);

			ImageMenuItem deleteItem = new ImageMenuItem ("Delete attribute");
			deleteItem.Image = new Gtk.Image (Stock.Delete, IconSize.Menu);
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();
			popup.Append (deleteItem);

			popup.Popup(null, null, null, 3, Gtk.Global.CurrentEventTime);
		}
	}
	
}
