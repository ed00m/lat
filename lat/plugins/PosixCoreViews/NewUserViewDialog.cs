// 
// lat - NewUserViewDialog.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
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

using Gtk;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Mono.Security.Protocol.Ntlm;
using Novell.Directory.Ldap;

namespace lat
{
	public class NewUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newUserDialog;

		// General 
		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry displayNameEntry;
		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.HBox comboHbox;
		[Glade.Widget] Gtk.CheckButton enableSambaButton;

		private static string[] userAttrs = { "givenName", "sn", "uid", "uidNumber", "gidNumber",
					      "userPassword", "initials", "loginShell", "cn",
					      "homeDirectory", "displayName" };

		private Hashtable _allGroups;
		private Hashtable _allGroupGids;
		private Hashtable _memberOfGroups;

		private string _smbSID = "";
		private string _smbLM = "";
		private string _smbNT = "";

		private ComboBox primaryGroupComboBox;

		public NewUserViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();		

			getGroups ();

			createCombo ();

			uidSpinButton.Value = server.GetNextUID ();
			enableSambaButton.Toggled += new EventHandler (OnSambaChanged);

			newUserDialog.Icon = Global.latIcon;
			newUserDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newUserDialog.Run ();				
			}

			newUserDialog.Destroy ();
		}

		private void OnSambaChanged (object o, EventArgs args)
		{
			if (enableSambaButton.Active) {
				_smbSID = server.GetLocalSID ();

				if (_smbSID == null) {
					Util.DisplaySambaSIDWarning (newUserDialog);
					enableSambaButton.Active = false;
					return;
				}
			}
		}
		
		private void getGroups ()
		{
			LdapEntry[] grps = server.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				_allGroups.Add (nameAttr.StringValue, e);
				_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		private void createCombo ()
		{
			primaryGroupComboBox = ComboBox.NewText ();

			foreach (string key in _allGroups.Keys)
				primaryGroupComboBox.AppendText (key);

			primaryGroupComboBox.Active = 0;
			primaryGroupComboBox.Show ();

			comboHbox.Add (primaryGroupComboBox);
		}

		private void Init ()
		{
			_memberOfGroups = new Hashtable ();
			_allGroups = new Hashtable ();
			_allGroupGids = new Hashtable ();

			ui = new Glade.XML (null, "lat.glade", "newUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = newUserDialog;

			passwordEntry.Sensitive = false;

			displayNameEntry.FocusInEvent += new FocusInEventHandler (OnDisplayNameFocusIn);
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);

			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
		}

		public void OnPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (!passwordEntry.Text.Equals ("") && pd.UnixPassword.Equals (""))
				return;

			passwordEntry.Text = pd.UnixPassword;
			_smbLM = pd.LMPassword;
			_smbNT = pd.NTPassword;
		}
		
		private void OnDisplayNameFocusIn (object o, EventArgs args)
		{
			string suid = Util.SuggestUserName (
					firstNameEntry.Text, 
					lastNameEntry.Text);

			usernameEntry.Text = suid;

			if (displayNameEntry.Text != "")
				return;

			if (initialsEntry.Text.Equals("")) {
				displayNameEntry.Text = String.Format ("{0} {1}", 
					firstNameEntry.Text, 
					lastNameEntry.Text);
			} else {

				String format = "";
				if (initialsEntry.Text.EndsWith("."))
					format = "{0} {1} {2}";
				else
					format = "{0} {1}. {2}";

				displayNameEntry.Text = String.Format (format, 
					firstNameEntry.Text, 
					initialsEntry.Text, 
					lastNameEntry.Text);
			}

			if (homeDirEntry.Text.Equals("") && !usernameEntry.Text.Equals(""))
				homeDirEntry.Text = String.Format("/home/{0}", usernameEntry.Text);
		}
			
		private void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try {
				server.Modify (groupEntry.DN, mods);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to modify group ") + groupEntry.DN;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Modify error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		private void updateGroupMembership ()
		{
			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [1];

			foreach (string key in _memberOfGroups.Keys) {

				LdapAttribute attr = new LdapAttribute ("memberUid", usernameEntry.Text);
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				groupEntry = (LdapEntry) _allGroups[key];

				mods[0] = lm;
			}

			modifyGroup (groupEntry, mods);
		}

		private string getGidNumber (string name)
		{
			if (name == null)
				return null;

			LdapEntry le = (LdapEntry) _allGroups [name];		
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		private Hashtable getUpdatedUserInfo ()
		{
			Hashtable retVal = new Hashtable ();

			TreeIter iter;
				
			if (primaryGroupComboBox.GetActiveIter (out iter)) {
				string pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);
				retVal.Add ("gidNumber", getGidNumber(pg));
			}

			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("userPassword", passwordEntry.Text);
			retVal.Add ("loginShell", shellEntry.Text);
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("displayName", displayNameEntry.Text);
			retVal.Add ("initials", initialsEntry.Text);

			return retVal;
		}
	
		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cui = getUpdatedUserInfo ();

			string[] objClass = { "top", "posixaccount", "shadowaccount","inetorgperson", "person" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			if (!Util.CheckUserName (server, usernameEntry.Text)) {
				string format = Mono.Unix.Catalog.GetString (
					"A user with the username '{0}' already exists!");

				string msg = String.Format (format, usernameEntry.Text);

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				errorOccured = true;

				return;
			}

			if (!Util.CheckUID (server, Convert.ToInt32 (uidSpinButton.Value))) {
				string msg = Mono.Unix.Catalog.GetString (
					"The UID you have selected is already in use!");

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				errorOccured = true;

				return;
			}

			if (passwordEntry.Text == "" || passwordEntry.Text == null) {
				string msg = Mono.Unix.Catalog.GetString (
					"You must set a password for the new user");

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				errorOccured = true;

				return;
			}

			string fullName = (string)cui["displayName"];

			cui["cn"] = fullName;
			cui["gecos"] = fullName;

			ArrayList attrList = getAttributes (objClass, userAttrs, cui);
			attrList.Add (new LdapAttribute ("cn", fullName));
			attrList.Add (new LdapAttribute ("gecos", fullName));

			if (enableSambaButton.Active) {

				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;

				ArrayList smbMods = Util.CreateSambaMods (
							user_rid, 
							_smbSID,
							_smbLM,
							_smbNT);

				foreach (LdapModification l in smbMods) {
					if (l.Attribute.Name.Equals ("objectclass")) {
 	
						LdapAttribute a = (LdapAttribute) attrList[0];
						a.addValue ("sambaSAMAccount");

						attrList[0] = a;
					}
					else
						attrList.Add (l.Attribute);
				}
			}

			string userDN = null;

			if (this.defaultNewContainer == null) {
				SelectContainerDialog scd = 
					new SelectContainerDialog (server, newUserDialog);

				scd.Title = "Save User";
				scd.Message = String.Format ("Where in the directory would\nyou like save the user\n{0}?", fullName);

				scd.Run ();

				if (scd.DN == "")
					return;
					
				userDN = String.Format ("cn={0},{1}", fullName, scd.DN);
				
			} else {
			
				userDN = String.Format ("cn={0},{1}", fullName, this.defaultNewContainer);
			}
			
			updateGroupMembership ();

			if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
				errorOccured = true;
				return;
			}

			newUserDialog.HideAll ();
		}
	}
}